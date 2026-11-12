using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteFW
{
    /// <summary>框架启动入口，负责创建、接线和释放服务。</summary>
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
        /// <summary>对象池管理器。</summary>
        private PoolManager m_PoolManager;
        /// <summary>标签服务。</summary>
        private IGameplayTagManager m_GameplayTags;
        /// <summary>输入管理器。</summary>
        private InputManager m_InputManager;
        /// <summary>UI 面板管理器。</summary>
        private UIManager m_UIManager;
        /// <summary>UI 根节点。</summary>
        private GameObject m_UIRoot;
        /// <summary>是否已完成初始化。</summary>
        private bool m_Initialized;
        /// <summary>是否正在初始化。</summary>
        private bool m_Initializing;

        /// <summary>当前活动的启动器。</summary>
        public static FrameworkBootstrap Instance => s_Instance;

        /// <summary>框架服务是否已初始化完成。</summary>
        public bool IsReady => m_Initialized;

        /// <summary>音频服务。</summary>
        public AudioManager Audio => m_AudioManager;

        /// <summary>黑板容器。</summary>
        public BlackboardManager Blackboards => m_BlackboardManager;

        /// <summary>对象池管理器。</summary>
        public PoolManager Pools => m_PoolManager;

        /// <summary>标签服务。</summary>
        public IGameplayTagManager Tags => m_GameplayTags;

        /// <summary>输入管理器。</summary>
        public InputManager Input => m_InputManager;

        /// <summary>场景导航服务。</summary>
        public ISceneNavigator Scenes => m_SceneNavigator;

        /// <summary>UI 面板管理器。</summary>
        public UIManager UI => m_UIManager;

        /// <summary>资源提供器。</summary>
        public IAssetProvider Assets => m_Assets;

        /// <summary>建立单例并启动框架初始化。</summary>
        private async void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            await InitializeAsync();
        }

        /// <summary>启动器销毁时释放全部服务。</summary>
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
            if (m_Initializing || m_Initialized)
                throw new InvalidOperationException("启动器已初始化，不能替换资源提供器。");

            m_Assets = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>按顺序创建并接线各框架服务。</summary>
        private async UniTask InitializeAsync()
        {
            if (m_Initialized)
                return;

            m_Initializing = true;
            try
            {
                m_Assets ??= new ResourcesAssetProvider();
                m_AudioManager = CreateService<AudioManager>("AudioManager", false);
                m_BlackboardManager = CreateService<BlackboardManager>("BlackboardManager");
                m_InputManager = CreateService<InputManager>("InputManager");
                m_AudioManager.SetAssetProvider(m_Assets);
                m_AudioManager.gameObject.SetActive(true);

                var poolRoot = new GameObject("PoolRoot");
                poolRoot.transform.SetParent(transform, false);
                m_PoolManager = new PoolManager(poolRoot.transform);

                m_GameplayTags = await LoadTagsAsync();

                CreateUIRuntime();
                await m_UIManager.LoadRegistryAsync();
                m_SceneNavigator = new SceneNavigator();
                m_Initialized = true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"框架初始化失败：{exception}");
                Shutdown();
            }
            finally
            {
                m_Initializing = false;
            }
        }

        /// <summary>加载标签目录；缺失时创建空标签服务。</summary>
        private async UniTask<IGameplayTagManager> LoadTagsAsync()
        {
            const string key = "Tags/GameplayTagCatalog";
            IAssetHandle<GameplayTagCatalog> handle = null;
            try
            {
                handle = await m_Assets.LoadAsync<GameplayTagCatalog>(key);
                return GameplayTagManager.Create(handle.Asset.CreateSource());
            }
            catch (AssetNotFoundException)
            {
                return GameplayTagManager.Create();
            }
            finally
            {
                if (handle != null)
                    handle.Dispose();
            }
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
        private T CreateService<T>(string objectName, bool active = true) where T : Component
        {
            var serviceObject = new GameObject(objectName);
            serviceObject.transform.SetParent(transform, false);
            serviceObject.SetActive(active);
            return serviceObject.AddComponent<T>();
        }

        /// <summary>释放全部框架服务与资源。</summary>
        private void Shutdown()
        {
            if (m_UIManager != null)
            {
                m_UIManager.Dispose();
                m_UIManager = null;
            }

            m_PoolManager?.Dispose();
            m_PoolManager = null;
            m_GameplayTags = null;
            m_InputManager = null;

            if (m_UIRoot != null)
            {
                Destroy(m_UIRoot);
                m_UIRoot = null;
            }

            if (m_Assets is IDisposable disposableAssets)
                disposableAssets.Dispose();

            if (m_SceneNavigator is IDisposable disposable)
                disposable.Dispose();

            EventBus.Clear();
            m_SceneNavigator = null;
            m_Assets = null;
            m_Initialized = false;
            m_Initializing = false;
        }
    }
}
