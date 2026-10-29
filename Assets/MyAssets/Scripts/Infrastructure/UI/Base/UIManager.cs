using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 面板管理器：负责面板的注册、打开、关闭与预制体缓存。
    /// 资源通过 IAssetProvider 加载（默认 Resources，接入 YooAsset 时替换）。
    /// </summary>
    public class UIManager : IDisposable
    {
        private static UIManager _instance;

        /// <summary>
        /// 全局单例。
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UIManager();
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, string> m_PathDict = new Dictionary<string, string>();
        private readonly Dictionary<string, PrefabEntry> m_PrefabDict = new Dictionary<string, PrefabEntry>();
        private readonly Dictionary<string, BasePanel> m_PanelDict = new Dictionary<string, BasePanel>();
        private readonly HashSet<string> m_LoadingPanels = new HashSet<string>();

        private IAssetProvider m_Assets = new ResourcesAssetProvider();
        private Transform m_Root;

        private sealed class PrefabEntry
        {
            internal PrefabEntry(GameObject prefab, IAssetHandle<GameObject> handle)
            {
                Prefab = prefab;
                Handle = handle;
            }

            internal GameObject Prefab { get; }
            internal IAssetHandle<GameObject> Handle { get; }
        }

        /// <summary>
        /// 设置面板父节点（所有面板实例的挂载点）。
        /// </summary>
        public void SetRoot(Transform root)
        {
            m_Root = root;
        }

        /// <summary>
        /// 替换资源提供器；已缓存的预制体会先释放。
        /// </summary>
        public void SetAssetProvider(IAssetProvider provider)
        {
            ReleaseCachedPrefabs();
            m_Assets = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// 注册面板：面板名与预制体路径。
        /// </summary>
        public void RegisterPanel(string name, string path)
        {
            m_PathDict[name] = path;
        }

        /// <summary>
        /// 指定面板是否已打开。
        /// </summary>
        public bool IsOpen(string name)
        {
            return m_PanelDict.ContainsKey(name);
        }

        /// <summary>
        /// 异步打开指定面板；已打开、加载中、未注册路径或未设置根节点时记录错误并返回 null。
        /// </summary>
        public async UniTask<BasePanel> OpenPanelAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            if (m_PanelDict.ContainsKey(name) || m_LoadingPanels.Contains(name))
            {
                Debug.LogError("界面已经打开或正在加载：" + name);
                return null;
            }

            if (!m_PathDict.TryGetValue(name, out string path))
            {
                Debug.LogError("界面未注册，或者未配置路径：" + name);
                return null;
            }

            if (m_Root == null)
            {
                Debug.LogError("未设置面板父节点，请先调用 SetRoot：" + name);
                return null;
            }

            m_LoadingPanels.Add(name);
            IAssetHandle<GameObject> pendingHandle = null;
            try
            {
                PrefabEntry entry = null;
                if (!m_PrefabDict.TryGetValue(name, out entry) || entry.Prefab == null || !entry.Handle.IsValid)
                {
                    pendingHandle = await m_Assets.LoadAsync<GameObject>(path, cancellationToken);
                    if (!pendingHandle.IsValid)
                        throw new InvalidOperationException($"面板资源无效：{name}");

                    entry = new PrefabEntry(pendingHandle.Asset, pendingHandle);
                    m_PrefabDict[name] = entry;
                    pendingHandle = null;
                }

                cancellationToken.ThrowIfCancellationRequested();
                GameObject panelObject = UnityEngine.Object.Instantiate(entry.Prefab, m_Root, false);
                BasePanel panel = panelObject.GetComponent<BasePanel>();
                if (panel == null)
                {
                    Debug.LogError("面板预制体上缺少 BasePanel 组件：" + name);
                    UnityEngine.Object.Destroy(panelObject);
                    return null;
                }

                panel.OpenPanel(name);
                m_PanelDict.Add(name, panel);
                return panel;
            }
            catch (OperationCanceledException)
            {
                pendingHandle?.Dispose();
                return null;
            }
            catch (Exception exception)
            {
                pendingHandle?.Dispose();
                ReleasePrefab(name);
                Debug.LogError($"打开面板失败：{name}；{exception.Message}");
                return null;
            }
            finally
            {
                m_LoadingPanels.Remove(name);
            }
        }

        /// <summary>
        /// 关闭指定面板。
        /// </summary>
        public bool ClosePanel(string name)
        {
            if (!m_PanelDict.TryGetValue(name, out BasePanel panel))
            {
                Debug.LogError("界面未打开：" + name);
                return false;
            }

            m_PanelDict.Remove(name);
            panel.ClosePanel();
            return true;
        }

        /// <summary>
        /// 移除面板记录（面板被外部关闭或销毁时的兜底）。
        /// </summary>
        internal void NotifyClosed(string name)
        {
            m_PanelDict.Remove(name);
        }

        /// <summary>
        /// 释放缓存的预制体资源句柄。
        /// </summary>
        public void Dispose()
        {
            ReleaseCachedPrefabs();
            m_PathDict.Clear();
            m_PanelDict.Clear();
            m_LoadingPanels.Clear();
            _instance = null;
        }

        private void ReleaseCachedPrefabs()
        {
            foreach (PrefabEntry entry in m_PrefabDict.Values)
                entry.Handle.Dispose();
            m_PrefabDict.Clear();
        }

        private void ReleasePrefab(string name)
        {
            if (!m_PrefabDict.TryGetValue(name, out PrefabEntry entry))
                return;

            m_PrefabDict.Remove(name);
            entry.Handle.Dispose();
        }
    }
}
