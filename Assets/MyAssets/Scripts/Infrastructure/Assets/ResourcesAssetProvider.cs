using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 基于 Resources 的资源提供器：开发期与小型项目实现。
    /// </summary>
    public sealed class ResourcesAssetProvider : IAssetProvider
    {
        /// <summary>
        /// 从 Resources 目录异步加载资源；失败时抛出异常。
        /// </summary>
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
                throw new InvalidOperationException($"资源加载失败：{key}（类型 {typeof(T).Name}）");

            return new ResourcesAssetHandle<T>(asset);
        }
    }

    /// <summary>
    /// Resources 资源句柄：只负责框架内的有效性状态。
    /// </summary>
    internal sealed class ResourcesAssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        private readonly T m_Asset;
        private bool m_IsValid = true;

        /// <summary>创建 Resources 资源句柄。</summary>
        internal ResourcesAssetHandle(T asset)
        {
            m_Asset = asset;
        }

        /// <summary>已加载的资源。</summary>
        public T Asset => m_Asset;

        /// <summary>句柄是否仍然有效。</summary>
        public bool IsValid => m_IsValid && m_Asset != null;

        /// <summary>结束持有，使句柄失效。</summary>
        public void Dispose()
        {
            m_IsValid = false;
        }
    }
}
