using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace LiteFW
{
    /// <summary>
    /// 音频服务（全局单例）：2D / 3D / 跟随播放共用 AudioSource 对象池，
    /// 播放结束后自动回收；返回 AudioHandle 可主动停止；BGM 使用独立音源。
    /// 单实例播放按调用方指定的分组 Key 隔离：同组新播放替换旧播放，不同组可以并存。
    /// 资源通过 IAssetProvider 按 key 加载（默认 Resources，可替换）。
    /// 备注：音频参数也可用 ScriptableObject 预设（AudioPreset，参考 StreamRoar）集中配置；
    /// 本项目规划走 Luban 配表，参数由表数据提供，故此处保持直接传参。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>单例实例。</summary>
        private static AudioManager _instance;

        /// <summary>
        /// 全局单例；未挂载时首次访问自动创建。
        /// </summary>
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
                return _instance;
            }
        }

        /// <summary>初始化时预创建的播放器数量。</summary>
        [Header("播放器对象池")]
        [Tooltip("预热数量：初始化时预创建的空闲播放器数量")]
        [SerializeField] private int poolPrewarmCount = 8;

        /// <summary>池容量上限，超出后归还即销毁。</summary>
        [Tooltip("池最大容量：超出后归还的播放器会被销毁")]
        [SerializeField] private int poolMaxSize = 64;

        [Header("AudioMixer 分组（可选）")]

        /// <summary>BGM 输出的 Mixer 分组。</summary>
        [SerializeField] private AudioMixerGroup bgmGroup;

        /// <summary>音效输出的 Mixer 分组。</summary>
        [SerializeField] private AudioMixerGroup sfxGroup;

        /// <summary>当前音频资源提供器。</summary>
        private IAssetProvider m_Assets;
        /// <summary>音效播放器对象池。</summary>
        private ObjectPool<AudioSource> m_SourcePool;
        /// <summary>进行中的播放记录。</summary>
        private readonly List<Playback> m_ActivePlaybacks = new List<Playback>();
        /// <summary>下一个可分配的播放标识。</summary>
        private int m_NextPlaybackId = 1;
        /// <summary>BGM 专用播放器。</summary>
        private AudioSource m_BgmSource;
        /// <summary>当前 BGM 的资源句柄。</summary>
        private IAssetHandle<AudioClip> m_BgmHandle;
        /// <summary>各分组的单实例播放记录。</summary>
        private readonly Dictionary<string, SinglePlayback> m_SinglePlaybacks = new Dictionary<string, SinglePlayback>();

        /// <summary>一个分组的单实例播放状态。</summary>
        private sealed class SinglePlayback
        {
            /// <summary>当前请求序号。</summary>
            public int RequestId;

            /// <summary>当前播放使用的音源。</summary>
            public AudioSource Source;

            /// <summary>当前播放持有的资源。</summary>
            public IAssetHandle<AudioClip> AssetHandle;

            /// <summary>归还当前音源并释放资源。</summary>
            public void Release()
            {
                if (Source != null)
                {
                    Source.Stop();
                    Source.clip = null;
                }

                AssetHandle?.Dispose();
                Source = null;
                AssetHandle = null;
            }
        }

        /// <summary>
        /// 一次播放的运行时记录。
        /// </summary>
        private readonly struct Playback
        {
            /// <summary>创建播放记录。</summary>
            public Playback(int id, AudioSource source, Transform followTarget, IAssetHandle<AudioClip> assetHandle)
            {
                Id = id;
                Source = source;
                FollowsTarget = followTarget != null;
                FollowTarget = followTarget;
                AssetHandle = assetHandle;
            }

            /// <summary>播放标识。</summary>
            public int Id { get; }

            /// <summary>播放用的音频源。</summary>
            public AudioSource Source { get; }

            /// <summary>是否需要跟随目标。</summary>
            public bool FollowsTarget { get; }

            /// <summary>跟随目标。</summary>
            public Transform FollowTarget { get; }

            /// <summary>音频资源句柄。</summary>
            public IAssetHandle<AudioClip> AssetHandle { get; }
        }

        /// <summary>确认单例归属并初始化音频系统。</summary>
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            Init();
        }

        /// <summary>每帧回收播放并更新跟随位置。</summary>
        void Update()
        {
            Tick();
        }

        /// <summary>销毁时停止全部播放并释放资源。</summary>
        void OnDestroy()
        {
            if (_instance != this)
                return;

            for (int i = m_ActivePlaybacks.Count - 1; i >= 0; i--)
                ReleaseAt(i);

            StopBGM();
            foreach (SinglePlayback playback in m_SinglePlaybacks.Values)
            {
                AudioSource source = playback.Source;
                playback.Release();
                if (source != null)
                    m_SourcePool.Release(source);
            }
            m_SinglePlaybacks.Clear();
            m_SourcePool?.Dispose();
            _instance = null;
        }

        /// <summary>
        /// 替换资源提供器（如接入 YooAsset 时注入对应实现）。
        /// </summary>
        internal void SetAssetProvider(IAssetProvider provider)
        {
            m_Assets = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// 异步播放 2D 声音（UI 音效、提示音）：不受世界位置影响。
        /// </summary>
        public UniTask<AudioHandle> Play2DAsync(
            string assetKey,
            float volume = 1f,
            float pitch = 1f,
            CancellationToken cancellationToken = default)
        {
            return PlayAsync(assetKey, Vector3.zero, false, null, volume, pitch, cancellationToken);
        }

        /// <summary>
        /// 异步播放 3D 声音：在指定世界位置发声，带距离衰减。
        /// </summary>
        public UniTask<AudioHandle> PlayAtAsync(
            string assetKey,
            Vector3 position,
            float volume = 1f,
            float pitch = 1f,
            CancellationToken cancellationToken = default)
        {
            return PlayAsync(assetKey, position, true, null, volume, pitch, cancellationToken);
        }

        /// <summary>
        /// 异步播放跟随声音：持续跟随目标位置；跟随目标销毁后自动停止。
        /// </summary>
        public UniTask<AudioHandle> PlayFollowAsync(
            string assetKey,
            Transform target,
            float volume = 1f,
            float pitch = 1f,
            CancellationToken cancellationToken = default)
        {
            if (!target)
                throw new ArgumentNullException(nameof(target));

            return PlayAsync(assetKey, target.position, true, target, volume, pitch, cancellationToken);
        }

        /// <summary>
        /// 以随机音高异步播放 2D 声音。
        /// </summary>
        public UniTask<AudioHandle> Play2DRandomPitchAsync(
            string assetKey,
            float minPitch = 0.6f,
            float maxPitch = 1.8f,
            float volume = 1f,
            CancellationToken cancellationToken = default)
        {
            return Play2DAsync(
                assetKey,
                volume,
                UnityEngine.Random.Range(minPitch, maxPitch),
                cancellationToken);
        }

        /// <summary>
        /// 停止指定播放；播放已经结束或被回收时返回 false。
        /// </summary>
        public bool Stop(AudioHandle handle)
        {
            if (!handle.IsValid)
                return false;

            for (int i = m_ActivePlaybacks.Count - 1; i >= 0; i--)
            {
                if (m_ActivePlaybacks[i].Id != handle.Id)
                    continue;

                ReleaseAt(i);
                return true;
            }

            return false;
        }

        /// <summary>按分组播放单实例声音；同组新播放替换旧播放。</summary>
        public async UniTask<AudioHandle> PlaySingleAsync(
            string groupKey,
            string assetKey,
            float volume = 1f,
            float pitch = 1f,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupKey))
                throw new ArgumentException("单实例分组 Key 不能为空。", nameof(groupKey));
            if (string.IsNullOrEmpty(assetKey))
                throw new ArgumentException("音频资源 key 不能为空。", nameof(assetKey));

            if (!m_SinglePlaybacks.TryGetValue(groupKey, out SinglePlayback playback))
            {
                playback = new SinglePlayback();
                m_SinglePlaybacks.Add(groupKey, playback);
            }

            int requestId = ++playback.RequestId;
            ReleaseSingle(playback);

            try
            {
                IAssetHandle<AudioClip> assetHandle =
                    await m_Assets.LoadAsync<AudioClip>(assetKey, cancellationToken);
                if (!m_SinglePlaybacks.TryGetValue(groupKey, out SinglePlayback current) ||
                    current.RequestId != requestId)
                {
                    assetHandle.Dispose();
                    return default;
                }

                if (!assetHandle.IsValid)
                {
                    assetHandle.Dispose();
                    m_SinglePlaybacks.Remove(groupKey);
                    return default;
                }

                AudioSource source = m_SourcePool.Get();
                source.clip = assetHandle.Asset;
                source.outputAudioMixerGroup = sfxGroup;
                source.loop = false;
                source.volume = Mathf.Clamp01(volume);
                source.pitch = pitch;
                source.spatialBlend = 0f;
                source.transform.position = Vector3.zero;

                current.Source = source;
                current.AssetHandle = assetHandle;
                source.Play();
                return new AudioHandle(m_NextPlaybackId++);
            }
            catch
            {
                if (m_SinglePlaybacks.TryGetValue(groupKey, out SinglePlayback current) &&
                    current.RequestId == requestId)
                {
                    m_SinglePlaybacks.Remove(groupKey);
                }
                throw;
            }
        }

        /// <summary>停止指定分组的单实例播放。</summary>
        public bool StopSingle(string groupKey)
        {
            if (string.IsNullOrWhiteSpace(groupKey) ||
                !m_SinglePlaybacks.TryGetValue(groupKey, out SinglePlayback playback))
            {
                return false;
            }

            AudioSource source = playback.Source;
            playback.Release();
            m_SinglePlaybacks.Remove(groupKey);
            if (source != null)
                m_SourcePool.Release(source);
            return true;
        }

        /// <summary>释放分组当前播放并归还音源。</summary>
        private void ReleaseSingle(SinglePlayback playback)
        {
            AudioSource source = playback.Source;
            playback.Release();
            if (source != null)
                m_SourcePool.Release(source);
        }

        /// <summary>
        /// 播放背景音乐。
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            ReleaseBgmHandle();
            m_BgmSource.clip = clip;
            m_BgmSource.Play();
        }

        /// <summary>
        /// 异步按资源 key 播放背景音乐。
        /// </summary>
        public async UniTask<AudioHandle> PlayBGMAsync(
            string assetKey,
            float volume = 1f,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(assetKey))
                throw new ArgumentException("音频资源 key 不能为空。", nameof(assetKey));

            IAssetHandle<AudioClip> handle = await m_Assets.LoadAsync<AudioClip>(assetKey, cancellationToken);
            ReleaseBgmHandle();
            m_BgmHandle = handle;
            m_BgmSource.clip = handle.Asset;
            m_BgmSource.volume = Mathf.Clamp01(volume);
            m_BgmSource.Play();
            return new AudioHandle(m_NextPlaybackId++);
        }

        /// <summary>
        /// 停止背景音乐。
        /// </summary>
        public void StopBGM()
        {
            m_BgmSource.Stop();
            m_BgmSource.clip = null;
            ReleaseBgmHandle();
        }

        /// <summary>创建播放器池与 BGM 播放器。</summary>
        private void Init()
        {
            if (m_Assets == null)
                throw new InvalidOperationException("音频资源提供器尚未注入。");

            m_SourcePool = new ObjectPool<AudioSource>(
                CreateSource,
                OnGetSource,
                OnReleaseSource,
                OnDestroySource,
                collectionCheck: true,
                defaultCapacity: poolPrewarmCount,
                maxSize: poolMaxSize);

            PrewarmPool(poolPrewarmCount);

            m_BgmSource = gameObject.AddComponent<AudioSource>();
            m_BgmSource.loop = true;
            m_BgmSource.playOnAwake = false;
            m_BgmSource.outputAudioMixerGroup = bgmGroup;
        }

        /// <summary>
        /// 预先创建指定数量的播放器并归还池中，避免首次播放时创建卡顿。
        /// </summary>
        private void PrewarmPool(int count)
        {
            var prewarmed = new List<AudioSource>(count);
            for (int i = 0; i < count; i++)
                prewarmed.Add(m_SourcePool.Get());

            for (int i = 0; i < prewarmed.Count; i++)
                m_SourcePool.Release(prewarmed[i]);
        }

        /// <summary>加载音频并从池中取播放器开始播放。</summary>
        private async UniTask<AudioHandle> PlayAsync(
            string assetKey,
            Vector3 position,
            bool is3D,
            Transform followTarget,
            float volume,
            float pitch,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(assetKey))
                throw new ArgumentException("音频资源 key 不能为空。", nameof(assetKey));

            IAssetHandle<AudioClip> assetHandle =
                await m_Assets.LoadAsync<AudioClip>(assetKey, cancellationToken);
            if (!assetHandle.IsValid)
            {
                assetHandle.Dispose();
                return default;
            }

            AudioSource source = m_SourcePool.Get();
            source.transform.position = position;
            source.clip = assetHandle.Asset;
            source.outputAudioMixerGroup = sfxGroup;
            source.loop = false;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = pitch;
            source.spatialBlend = is3D ? 1f : 0f;

            int playbackId = m_NextPlaybackId++;
            m_ActivePlaybacks.Add(new Playback(playbackId, source, followTarget, assetHandle));
            source.Play();
            return new AudioHandle(playbackId);
        }

        /// <summary>
        /// 每帧回收播放结束的声音，并更新跟随位置。
        /// </summary>
        private void Tick()
        {
            for (int i = m_ActivePlaybacks.Count - 1; i >= 0; i--)
            {
                Playback playback = m_ActivePlaybacks[i];

                if (playback.FollowsTarget && !playback.FollowTarget)
                {
                    ReleaseAt(i);
                    continue;
                }

                if (playback.FollowsTarget)
                    playback.Source.transform.position = playback.FollowTarget.position;

                if (!playback.Source.isPlaying)
                    ReleaseAt(i);
            }

            if (m_SinglePlaybacks.Count == 0)
                return;

            var finishedGroups = new List<string>();
            foreach (KeyValuePair<string, SinglePlayback> pair in m_SinglePlaybacks)
            {
                if (pair.Value.Source != null && !pair.Value.Source.isPlaying)
                    finishedGroups.Add(pair.Key);
            }

            for (int i = 0; i < finishedGroups.Count; i++)
                StopSingle(finishedGroups[i]);
        }

        /// <summary>归还指定播放器并释放其资源句柄。</summary>
        private void ReleaseAt(int index)
        {
            Playback playback = m_ActivePlaybacks[index];
            m_ActivePlaybacks.RemoveAt(index);
            m_SourcePool.Release(playback.Source);
            playback.AssetHandle.Dispose();
        }

        /// <summary>释放当前 BGM 资源句柄。</summary>
        private void ReleaseBgmHandle()
        {
            if (m_BgmHandle == null)
                return;

            m_BgmHandle.Dispose();
            m_BgmHandle = null;
        }

        /// <summary>在管理器下创建待用的播放器。</summary>
        private AudioSource CreateSource()
        {
            var go = new GameObject("AudioSource");
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            go.SetActive(false);
            return source;
        }

        /// <summary>取出播放器时激活其对象。</summary>
        private static void OnGetSource(AudioSource source)
        {
            source.gameObject.SetActive(true);
        }

        /// <summary>归还播放器时重置状态并隐藏。</summary>
        private void OnReleaseSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.loop = false;
            source.volume = 1f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.transform.SetParent(transform, false);
            source.gameObject.SetActive(false);
        }

        /// <summary>销毁不再需要的播放器。</summary>
        private static void OnDestroySource(AudioSource source)
        {
            if (source != null)
                Destroy(source.gameObject);
        }
    }
}
