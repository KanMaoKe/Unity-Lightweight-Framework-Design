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
        /// <summary>场景中活动的启动器实例。</summary>
        private static FrameworkBootstrap s_Instance;

        /// <summary>资源提供器。</summary>
        private IAssetProvider m_Assets;
        /// <summary>场景导航服务。</summary>
        private ISceneNavigator m_SceneNavigator;
        /// <summary>音频服务。</summary>
        private AudioManager m_AudioManager;
        /// <summary>黑板容器。</summary>
        private BlackboardManager m_BlackboardManager;
        /// <summary>UI 面板管理器。</summary>
        private UIManager m_UIManager;
        /// <summary>UI 根节点。</summary>
        private GameObject m_UIRoot;
        /// <summary>是否已完成初始化。</summary>
        private bool m_Initialized;

        /// <summary>当前活动的启动器。</summary>
        public static FrameworkBootstrap Instance => s_Instance;

        /// <summary>音频服务。</summary>
        public AudioManager Audio => m_AudioManager;

        /// <summary>黑板容器。</summary>
        public BlackboardManager Blackboards => m_BlackboardManager;

        /// <summary>场景导航服务。</summary>
        public ISceneNavigator Scenes => m_SceneNavigator;

        /// <summary>UI 面板管理器。</summary>
        public UIManager UI => m_UIManager;

        /// <summary>资源提供器。</summary>
        public IAssetProvider Assets => m_Assets;

        /// <summary>建立单例并完成框架初始化。</summary>
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

        /// <summary>活动启动器销毁时释放服务。</summary>
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
            if (m_Initialized)
                throw new InvalidOperationException("启动器已初始化，不能替换资源提供器。");

            m_Assets = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>按顺序创建并接线各框架服务。</summary>
        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_Assets ??= new ResourcesAssetProvider();
            m_AudioManager = CreateService<AudioManager>("AudioManager");
            m_BlackboardManager = CreateService<BlackboardManager>("BlackboardManager");
            m_AudioManager.SetAssetProvider(m_Assets);
            CreateUIRuntime();
            UIPanelRegistry registry = Resources.Load<UIPanelRegistry>("UI/UIPanelRegistry");
            if (registry != null)
                m_UIManager.RegisterPanels(registry.Panels);
            m_SceneNavigator = new SceneNavigator();
            m_Initialized = true;
        }

        /// <summary>创建 UI 根节点并注入资源提供器。</summary>
        private void CreateUIRuntime()
        {
            m_UIRoot = new GameObject("UIRoot", typeof(RectTransform));
            m_UIRoot.transform.SetParent(transform, false);

            m_UIManager = UIManager.Instance;
            m_UIManager.SetAssetProvider(m_Assets);
            m_UIManager.SetRoot(m_UIRoot.transform);
        }

        /// <summary>创建启动器子对象形式的服务。</summary>
        private T CreateService<T>(string objectName) where T : Component
        {
            var serviceObject = new GameObject(objectName);
            serviceObject.transform.SetParent(transform, false);
            return serviceObject.AddComponent<T>();
        }

        /// <summary>按依赖顺序释放 UI、资源和场景服务。</summary>
        private void Shutdown()
        {
            if (!m_Initialized)
                return;

            if (m_UIManager != null)
            {
                m_UIManager.Dispose();
                m_UIManager = null;
            }

            if (m_UIRoot != null)
            {
                Destroy(m_UIRoot);
                m_UIRoot = null;
            }

            if (m_Assets is IDisposable disposableAssets)
                disposableAssets.Dispose();

            if (m_SceneNavigator is IDisposable disposable)
                disposable.Dispose();

            m_SceneNavigator = null;
            m_Assets = null;
            m_Initialized = false;
        }
    }
}
