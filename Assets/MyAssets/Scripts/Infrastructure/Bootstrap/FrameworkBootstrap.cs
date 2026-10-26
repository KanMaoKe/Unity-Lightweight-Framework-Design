using System;
using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 框架启动入口：统一创建服务并管理其生命周期。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class FrameworkBootstrap : MonoBehaviour
    {
        private static FrameworkBootstrap s_Instance;

        private IAssetProvider m_Assets;
        private ISceneNavigator m_SceneNavigator;
        private AudioManager m_AudioManager;
        private BlackboardManager m_BlackboardManager;
        private bool m_Initialized;

        /// <summary>当前活动的启动器。</summary>
        public static FrameworkBootstrap Instance => s_Instance;

        /// <summary>音频服务。</summary>
        public AudioManager Audio => m_AudioManager;

        /// <summary>黑板容器。</summary>
        public BlackboardManager Blackboards => m_BlackboardManager;

        /// <summary>场景导航服务。</summary>
        public ISceneNavigator Scenes => m_SceneNavigator;

        /// <summary>资源提供器。</summary>
        public IAssetProvider Assets => m_Assets;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        void OnDestroy()
        {
            if (s_Instance != this)
                return;

            Shutdown();
            s_Instance = null;
        }

        /// <summary>替换资源提供器；须在启动器初始化前设置。</summary>
        public void SetAssetProvider(IAssetProvider provider)
        {
            m_Assets = provider ?? throw new ArgumentNullException(nameof(provider));
            if (m_AudioManager != null)
                m_AudioManager.SetAssetProvider(provider);
        }

        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_Assets ??= new ResourcesAssetProvider();
            m_AudioManager = CreateService<AudioManager>("AudioManager");
            m_BlackboardManager = CreateService<BlackboardManager>("BlackboardManager");
            m_AudioManager.SetAssetProvider(m_Assets);
            m_SceneNavigator = new SceneNavigator();
            m_Initialized = true;
        }

        private T CreateService<T>(string objectName) where T : Component
        {
            var serviceObject = new GameObject(objectName);
            serviceObject.transform.SetParent(transform, false);
            return serviceObject.AddComponent<T>();
        }

        private void Shutdown()
        {
            if (!m_Initialized)
                return;

            if (m_SceneNavigator is IDisposable disposable)
                disposable.Dispose();

            m_SceneNavigator = null;
            m_Assets = null;
            m_Initialized = false;
        }
    }
}
