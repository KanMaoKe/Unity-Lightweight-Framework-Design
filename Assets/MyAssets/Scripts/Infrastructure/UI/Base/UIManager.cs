using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteFW
{
    /// <summary>管理面板配置、加载、缓存与生命周期。</summary>
    public class UIManager : IDisposable
    {
        #region 框架内部实现 / 状态与缓存
        /// <summary>全局单例实例。</summary>
        private static UIManager _instance;

        /// <summary>全局单例。</summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UIManager();
                return _instance;
            }
        }

        /// <summary>面板配置表。</summary>
        private readonly Dictionary<string, PanelConfig> m_Configs = new Dictionary<string, PanelConfig>();
        /// <summary>已打开面板表。</summary>
        private readonly Dictionary<string, BasePanel> m_OpenPanels = new Dictionary<string, BasePanel>();
        /// <summary>已缓存的面板实例。</summary>
        private readonly Dictionary<string, BasePanel> m_CachedPanels = new Dictionary<string, BasePanel>();
        /// <summary>预制体与资源句柄缓存。</summary>
        private readonly Dictionary<string, PrefabEntry> m_PrefabCache = new Dictionary<string, PrefabEntry>();
        /// <summary>进行中的加载任务。</summary>
        private readonly Dictionary<string, UniTask<BasePanel>> m_LoadingRequests = new Dictionary<string, UniTask<BasePanel>>();
        /// <summary>层到根节点的映射。</summary>
        private readonly Dictionary<PanelLayer, UILayerRoot> m_Layers = new Dictionary<PanelLayer, UILayerRoot>();

        /// <summary>当前资源提供器。</summary>
        private IAssetProvider m_Assets;
        /// <summary>UI 根节点。</summary>
        private Transform m_Root;
        /// <summary>驱动打开面板 Update 的组件。</summary>
        private UIUpdateDriver m_UpdateDriver;
        /// <summary>是否已释放。</summary>
        private bool m_IsDisposed;

        /// <summary>预制体缓存项。</summary>
        private sealed class PrefabEntry
        {
            /// <summary>创建预制体缓存项。</summary>
            internal PrefabEntry(GameObject prefab, IAssetHandle<GameObject> handle)
            {
                Prefab = prefab;
                Handle = handle;
            }

            /// <summary>缓存的预制体。</summary>
            internal GameObject Prefab { get; }

            /// <summary>预制体资源句柄。</summary>
            internal IAssetHandle<GameObject> Handle { get; }
        }
        #endregion

        #region 框架内部实现 / 运行时接入
        /// <summary>设置 UI 根节点并创建三层 Canvas。</summary>
        internal void SetRoot(Transform root)
        {
            m_Root = root ?? throw new ArgumentNullException(nameof(root));
            EnsureLayers();
        }

        /// <summary>替换资源提供器并释放旧预制体缓存。</summary>
        internal void SetAssetProvider(IAssetProvider provider)
        {
            ReleasePrefabs();
            m_Assets = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        #endregion

        #region 业务入口 / 面板注册与查询
        /// <summary>注册面板配置。</summary>
        public void RegisterPanel(PanelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            PanelLayerRules.Validate(config.Layer, config.SortOrder);

            if (m_Configs.TryGetValue(config.PanelId, out PanelConfig existing) &&
                existing.ResourcePath != config.ResourcePath)
            {
                throw new InvalidOperationException($"面板已注册到其他资源路径：{config.PanelId}");
            }

            m_Configs[config.PanelId] = config;
        }

        /// <summary>注册批量面板配置。</summary>
        public void RegisterPanels(IEnumerable<PanelConfig> configs)
        {
            if (configs == null)
                return;

            foreach (PanelConfig config in configs)
                RegisterPanel(config);
        }
        #endregion

        #region 框架内部实现 / 注册表加载
        /// <summary>从资源提供器加载并注册面板配置。</summary>
        internal async UniTask LoadRegistryAsync(CancellationToken cancellationToken = default)
        {
            if (m_Assets == null)
                throw new InvalidOperationException("UI 资源提供器尚未注入。");

            IAssetHandle<UIPanelRegistry> handle = null;
            try
            {
                handle = await m_Assets.LoadAsync<UIPanelRegistry>("UI/UIPanelRegistry", cancellationToken);
                if (handle.IsValid)
                    RegisterPanels(handle.Asset.Panels);
            }
            catch (AssetNotFoundException)
            {
                return;
            }
            finally
            {
                if (handle != null)
                    handle.Dispose();
            }
        }
        #endregion

        #region 业务入口 / 面板查询
        /// <summary>读取面板配置。</summary>
        public bool TryGetConfig(string name, out PanelConfig config)
        {
            return m_Configs.TryGetValue(name, out config);
        }

        /// <summary>面板是否打开。</summary>
        public bool IsOpen(string name)
        {
            return m_OpenPanels.TryGetValue(name, out BasePanel panel) && panel != null;
        }

        /// <summary>面板是否已有缓存实例。</summary>
        public bool HasCachedPanel(string name)
        {
            return m_CachedPanels.TryGetValue(name, out BasePanel panel) && panel != null;
        }

        /// <summary>获取已注册面板配置。</summary>
        public IReadOnlyCollection<PanelConfig> Configs => m_Configs.Values;
        #endregion

        #region 业务入口 / 面板操作与释放
        /// <summary>异步打开面板。</summary>
        public async UniTask<BasePanel> OpenPanelAsync(string name, CancellationToken cancellationToken = default)
        {
            if (m_IsDisposed)
                return null;

            if (!m_Configs.TryGetValue(name, out PanelConfig config))
            {
                Debug.LogError("界面未注册：" + name);
                return null;
            }

            if (m_OpenPanels.TryGetValue(name, out BasePanel openPanel) && openPanel != null)
                return openPanel;

            if (m_CachedPanels.TryGetValue(name, out BasePanel cachedPanel) && cachedPanel != null)
            {
                try
                {
                    OpenCachedPanel(cachedPanel, config);
                    return cachedPanel;
                }
                catch (Exception exception)
                {
                    DestroyFailedPanel(config.PanelId, cachedPanel, cachedPanel.gameObject);
                    Debug.LogError($"打开面板失败：{config.PanelId}；{exception.Message}");
                    return null;
                }
            }

            if (m_LoadingRequests.TryGetValue(name, out UniTask<BasePanel> pending))
                return await pending;

            UniTask<BasePanel> request = LoadAndOpenAsync(config, cancellationToken);
            m_LoadingRequests[name] = request;
            try
            {
                return await request;
            }
            finally
            {
                m_LoadingRequests.Remove(name);
            }
        }

        /// <summary>关闭面板但保留实例。</summary>
        public bool ClosePanel(string name)
        {
            if (!m_OpenPanels.TryGetValue(name, out BasePanel panel) || panel == null)
                return false;

            m_OpenPanels.Remove(name);
            panel.Close();
            m_CachedPanels[name] = panel;
            RefreshCoverage();
            return true;
        }

        /// <summary>销毁指定面板实例并保留预制体缓存。</summary>
        public bool ReleasePanel(string name)
        {
            if (!m_CachedPanels.TryGetValue(name, out BasePanel panel) || panel == null)
                return false;

            m_OpenPanels.Remove(name);
            m_CachedPanels.Remove(name);
            DestroyPanel(panel);
            RefreshCoverage();
            return true;
        }

        /// <summary>释放全部面板、配置与资源缓存。</summary>
        public void Dispose()
        {
            if (m_IsDisposed)
                return;

            m_IsDisposed = true;
            var panels = new HashSet<BasePanel>(m_OpenPanels.Values);
            panels.UnionWith(m_CachedPanels.Values);
            foreach (BasePanel panel in panels)
                DestroyPanel(panel);

            m_OpenPanels.Clear();
            m_CachedPanels.Clear();
            ReleasePrefabs();
            m_Configs.Clear();
            m_LoadingRequests.Clear();
            m_Layers.Clear();
            m_Root = null;
            m_UpdateDriver = null;
            _instance = null;
        }
        #endregion

        #region 框架内部实现 / 加载、层级与清理
        /// <summary>加载预制体、创建实例并打开面板。</summary>
        private async UniTask<BasePanel> LoadAndOpenAsync(PanelConfig config, CancellationToken cancellationToken)
        {
            IAssetHandle<GameObject> pendingHandle = null;
            GameObject instance = null;
            BasePanel panel = null;
            try
            {
                if (m_Root == null)
                    throw new InvalidOperationException("未设置 UI 根节点。");

                if (!m_PrefabCache.TryGetValue(config.PanelId, out PrefabEntry entry) ||
                    entry.Prefab == null || !entry.Handle.IsValid)
                {
                    pendingHandle = await m_Assets.LoadAsync<GameObject>(config.ResourcePath, cancellationToken);
                    if (m_IsDisposed)
                        throw new OperationCanceledException();
                    if (!pendingHandle.IsValid)
                        throw new InvalidOperationException($"面板资源无效：{config.PanelId}");

                    entry = new PrefabEntry(pendingHandle.Asset, pendingHandle);
                    m_PrefabCache[config.PanelId] = entry;
                    pendingHandle = null;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (m_IsDisposed)
                    throw new OperationCanceledException();

                UILayerRoot layerRoot = GetLayerRoot(config.Layer);
                instance = UnityEngine.Object.Instantiate(entry.Prefab, layerRoot.Root, false);
                panel = instance.GetComponent<BasePanel>();
                if (panel == null)
                    throw new InvalidOperationException($"面板预制体缺少 BasePanel：{config.PanelId}");

                panel.Configure(config);
                panel.Initialize();
                m_CachedPanels[config.PanelId] = panel;
                OpenCachedPanel(panel, config);
                return panel;
            }
            catch (OperationCanceledException)
            {
                pendingHandle?.Dispose();
                DestroyFailedPanel(config.PanelId, panel, instance);
                return null;
            }
            catch (Exception exception)
            {
                pendingHandle?.Dispose();
                DestroyFailedPanel(config.PanelId, panel, instance);
                ReleasePrefab(config.PanelId);
                Debug.LogError($"打开面板失败：{config.PanelId}；{exception.Message}");
                return null;
            }
        }

        /// <summary>激活缓存面板并登记打开状态。</summary>
        private void OpenCachedPanel(BasePanel panel, PanelConfig config)
        {
            m_OpenPanels[config.PanelId] = panel;
            m_CachedPanels[config.PanelId] = panel;
            panel.Open();
            SortLayer(config.Layer);
            RefreshCoverage();
        }

        /// <summary>根据弹窗数量更新覆盖、恢复与焦点通知。</summary>
        private void RefreshCoverage()
        {
            int popupCount = 0;
            BasePanel topPanel = null;
            foreach (BasePanel panel in m_OpenPanels.Values)
            {
                if (panel != null && panel.Layer == PanelLayer.Popup && panel.State == BasePanel.PanelState.Open)
                    popupCount++;

                if (panel != null && panel.Layer != PanelLayer.Popup &&
                    panel.State == BasePanel.PanelState.Open &&
                    (topPanel == null || panel.SortOrder > topPanel.SortOrder))
                {
                    topPanel = panel;
                }
            }

            foreach (BasePanel panel in m_OpenPanels.Values)
            {
                if (panel == null || panel.State != BasePanel.PanelState.Open)
                    continue;

                if (panel.Layer == PanelLayer.Popup)
                    continue;

                if (popupCount > 0)
                    panel.Cover();
                else if (panel.IsCovered)
                    panel.Reveal(panel == topPanel ? panel : null);
            }
        }

        /// <summary>每帧驱动打开面板。</summary>
        internal void Tick(float elapseSeconds, float realElapseSeconds)
        {
            foreach (BasePanel panel in m_OpenPanels.Values)
            {
                if (panel != null)
                    panel.Tick(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>按配置顺序重排同层面板实例。</summary>
        private void SortLayer(PanelLayer layer)
        {
            if (!m_Layers.TryGetValue(layer, out UILayerRoot layerRoot))
                return;

            var ordered = new List<BasePanel>();
            foreach (BasePanel panel in m_CachedPanels.Values)
            {
                if (panel != null && panel.Layer == layer && panel.transform.parent == layerRoot.Root)
                    ordered.Add(panel);
            }

            ordered.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
            for (int i = 0; i < ordered.Count; i++)
                ordered[i].transform.SetSiblingIndex(i);
        }

        /// <summary>创建三层 Canvas 根节点。</summary>
        private void EnsureLayers()
        {
            foreach (PanelLayer layer in Enum.GetValues(typeof(PanelLayer)))
                GetLayerRoot(layer);
        }

        /// <summary>获取或创建指定层的根节点。</summary>
        private UILayerRoot GetLayerRoot(PanelLayer layer)
        {
            if (m_Layers.TryGetValue(layer, out UILayerRoot existing) && existing != null)
                return existing;

            GameObject layerObject = new GameObject(layer + "Canvas", typeof(RectTransform));
            layerObject.transform.SetParent(m_Root, false);
            UILayerRoot layerRoot = layerObject.AddComponent<UILayerRoot>();
            layerRoot.Initialize(layer);
            m_Layers[layer] = layerRoot;
            if (m_UpdateDriver == null && m_Root != null)
                m_UpdateDriver = m_Root.gameObject.AddComponent<UIUpdateDriver>();
            return layerRoot;
        }

        /// <summary>释放并销毁面板实例。</summary>
        private void DestroyPanel(BasePanel panel)
        {
            if (panel == null)
                return;

            panel.Release();
            UnityEngine.Object.Destroy(panel.gameObject);
        }

        /// <summary>清理加载失败的半成品实例与记录。</summary>
        private void DestroyFailedPanel(string name, BasePanel panel, GameObject instance)
        {
            m_OpenPanels.Remove(name);
            m_CachedPanels.Remove(name);
            if (panel != null)
            {
                DestroyPanel(panel);
                return;
            }

            if (instance != null)
                UnityEngine.Object.Destroy(instance);
        }

        /// <summary>释放全部预制体资源句柄。</summary>
        private void ReleasePrefabs()
        {
            foreach (PrefabEntry entry in m_PrefabCache.Values)
                entry.Handle.Dispose();
            m_PrefabCache.Clear();
        }

        /// <summary>释放指定面板的预制体句柄。</summary>
        private void ReleasePrefab(string name)
        {
            if (!m_PrefabCache.TryGetValue(name, out PrefabEntry entry))
                return;

            m_PrefabCache.Remove(name);
            entry.Handle.Dispose();
        }
        #endregion
    }
}
