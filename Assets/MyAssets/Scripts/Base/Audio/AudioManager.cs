using UnityEngine;
using UnityEngine.Audio;

namespace Duel
{
    /// <summary>
    /// 音频服务（全局单例）：SFX 用固定声道轮转 + PlayOneShot，叠播不吞音；
    /// BGM 独立音源。可手动挂在全局物体上（如 GlobalManagement），不挂也行——
    /// 第一次调用时自动创建（DontDestroyOnLoad）。AudioMixer 分组可选：没拖就走主输出。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>(); // 触发 Awake 初始化
                }
                return _instance;
            }
        }

        [Header("SFX 声道")]
        [Tooltip("轮转声道数：短音效叠播不丢音，不够再加")]
        [SerializeField] private int voiceCount = 8;

        [Header("AudioMixer 分组（可选）")]
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        private AudioSource[] sfxVoices;
        private int nextVoice;
        private AudioSource bgmSource;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        private void Init()
        {
            // 每个声道一个子物体，PlayOneShot 叠播，不做借出/归还
            sfxVoices = new AudioSource[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                var go = new GameObject($"SFX{i}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = sfxGroup;
                sfxVoices[i] = source;
            }

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.outputAudioMixerGroup = bgmGroup;
        }

        public void PlaySFX(AudioClip clip, float pitch = 1f, float volumeScale = 1f)
        {
            if (clip == null || sfxVoices == null || sfxVoices.Length == 0) return;

            var source = sfxVoices[nextVoice];
            nextVoice = (nextVoice + 1) % sfxVoices.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, volumeScale);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void StopBGM() => bgmSource.Stop();

        public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.6f, float maxPitch = 1.8f, float volumeScale = 1f)
        {
            PlaySFX(clip, Random.Range(minPitch, maxPitch), volumeScale);
        }
    }
}
