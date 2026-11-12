using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteFW
{
    /// <summary>基于 Resources 的资源提供器。</summary>
    public sealed class ResourcesAssetProvider : IAssetProvider
    {
        #region 业务入口 / 资源加载
        /// <summary>从 Resources 异步加载资源；失败时抛出异常。</summary>
        public async UniTask<IAssetHandle<T>> LoadAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("资源 key 不能为空。", nameof(key));

            ResourceRequest request = Resources.LoadAsync<T>(key);
            UnityEngine.Object loaded = await request.ToUniTask(cancellationToken: cancellationToken);
            T asset = loaded as T;
            if (asset == null)
                throw new AssetNotFoundException($"资源不存在：{key}（类型 {typeof(T).Name}）");

            return new ResourcesAssetHandle<T>(asset);
        }
        #endregion
    }

    /// <summary>Resources 资源句柄，仅维护框架内有效性。</summary>
    internal sealed class ResourcesAssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        #region 框架内部实现 / 句柄状态
        /// <summary>已加载的资源实例。</summary>
        private readonly T m_Asset;
        /// <summary>框架内是否仍持有该资源。</summary>
        private bool m_IsValid = true;

        /// <summary>创建 Resources 资源句柄。</summary>
        internal ResourcesAssetHandle(T asset)
        {
            m_Asset = asset;
        }
        #endregion

        #region 业务入口 / 资源访问与释放
        /// <summary>已加载的资源。</summary>
        public T Asset => m_Asset;

        /// <summary>句柄是否仍然有效。</summary>
        public bool IsValid => m_IsValid && m_Asset != null;

        /// <summary>结束持有，使句柄失效。</summary>
        public void Dispose()
        {
            m_IsValid = false;
        }
        #endregion
    }
}
