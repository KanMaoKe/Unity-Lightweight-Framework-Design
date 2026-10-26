using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace LiteFW
{
    /// <summary>
    /// 音频服务（全局单例）：2D / 3D / 跟随播放共用 AudioSource 对象池，
    /// 播放结束后自动回收；返回 AudioHandle 可主动停止；BGM 使用独立音源。
    /// 资源通过 IAssetProvider 按 key 加载（默认 Resources，可替换）。
    /// 备注：音频参数也可用 ScriptableObject 预设（AudioPreset，参考 StreamRoar）集中配置；
    /// 本项目规划走 Luban 配表，参数由表数据提供，故此处保持直接传参。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
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

        [Header("播放器对象池")]
        [Tooltip("预热数量：初始化时预创建的空闲播放器数量")]
        [SerializeField] private int poolPrewarmCount = 8;

        [Tooltip("池最大容量：超出后归还的播放器会被销毁")]
        [SerializeField] private int poolMaxSize = 64;

        [Header("AudioMixer 分组（可选）")]
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        private IAssetProvider m_Assets;
        private ObjectPool<AudioSource> m_SourcePool;
        private readonly List<Playback> m_ActivePlaybacks = new List<Playback>();
        private int m_NextPlaybackId = 1;
        private AudioSource m_BgmSource;

        /// <summary>
        /// 一次播放的运行时记录。
        /// </summary>
        private readonly struct Playback
        {
            public Playback(int id, AudioSource source, Transform followTarget)
            {
                Id = id;
                Source = source;
                FollowsTarget = followTarget != null;
                FollowTarget = followTarget;
            }

            public int Id { get; }
            public AudioSource Source { get; }
            public bool FollowsTarget { get; }
            public Transform FollowTarget { get; }
        }

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

        void Update()
        {
            Tick();
        }

        void OnDestroy()
        {
            if (_instance != this)
                return;

            m_SourcePool?.Dispose();
            _instance = null;
        }

        /// <summary>
        /// 替换资源提供器（如接入 YooAsset 时注入对应实现）。
        /// </summary>
        public void SetAssetProvider(IAssetProvider provider)
        {
            m_Assets = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// 播放 2D 声音（UI 音效、提示音）：不受世界位置影响。
        /// </summary>
        public AudioHandle Play2D(string assetKey, float volume = 1f, float pitch = 1f)
        {
            return Play(assetKey, Vector3.zero, false, null, volume, pitch);
        }

        /// <summary>
        /// 播放 3D 声音：在指定世界位置发声，带距离衰减。
        /// </summary>
        public AudioHandle PlayAt(string assetKey, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            return Play(assetKey, position, true, null, volume, pitch);
        }

        /// <summary>
        /// 播放跟随声音：持续跟随目标位置；跟随目标销毁后自动停止。
        /// </summary>
        public AudioHandle PlayFollow(string assetKey, Transform target, float volume = 1f, float pitch = 1f)
        {
            if (!target)
                throw new ArgumentNullException(nameof(target));

            return Play(assetKey, target.position, true, target, volume, pitch);
        }

        /// <summary>
        /// 以随机音高播放 2D 声音。
        /// </summary>
        public AudioHandle Play2DRandomPitch(string assetKey, float minPitch = 0.6f, float maxPitch = 1.8f, float volume = 1f)
        {
            return Play2D(assetKey, volume, UnityEngine.Random.Range(minPitch, maxPitch));
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

        /// <summary>
        /// 播放背景音乐。
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            m_BgmSource.clip = clip;
            m_BgmSource.Play();
        }

        /// <summary>
        /// 停止背景音乐。
        /// </summary>
        public void StopBGM() => m_BgmSource.Stop();

        private void Init()
        {
            m_Assets ??= new ResourcesAssetProvider();

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

        private AudioHandle Play(string assetKey, Vector3 position, bool is3D, Transform followTarget, float volume, float pitch)
        {
            if (string.IsNullOrEmpty(assetKey))
                throw new ArgumentException("音频资源 key 不能为空。", nameof(assetKey));

            AudioClip clip = m_Assets.Load<AudioClip>(assetKey);
            if (clip == null)
                return default;

            AudioSource source = m_SourcePool.Get();
            source.transform.position = position;
            source.clip = clip;
            source.outputAudioMixerGroup = sfxGroup;
            source.loop = false;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = pitch;
            source.spatialBlend = is3D ? 1f : 0f;

            int playbackId = m_NextPlaybackId++;
            m_ActivePlaybacks.Add(new Playback(playbackId, source, followTarget));
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
        }

        private void ReleaseAt(int index)
        {
            Playback playback = m_ActivePlaybacks[index];
            m_ActivePlaybacks.RemoveAt(index);
            m_SourcePool.Release(playback.Source);
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("AudioSource");
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            go.SetActive(false);
            return source;
        }

        private static void OnGetSource(AudioSource source)
        {
            source.gameObject.SetActive(true);
        }

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

        private static void OnDestroySource(AudioSource source)
        {
            if (source != null)
                Destroy(source.gameObject);
        }
    }
}
