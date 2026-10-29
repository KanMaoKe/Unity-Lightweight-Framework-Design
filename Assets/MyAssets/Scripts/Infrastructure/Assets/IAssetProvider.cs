using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 资源加载抽象：统一异步加载与释放契约。
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// 按资源 key 异步加载资源；调用方负责释放返回的句柄。
        /// </summary>
        UniTask<IAssetHandle<T>> LoadAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;
    }

    /// <summary>
    /// 资源句柄：持有已加载资源并负责释放。
    /// </summary>
    public interface IAssetHandle<T> : IDisposable where T : UnityEngine.Object
    {
        /// <summary>加载完成的资源。</summary>
        T Asset { get; }

        /// <summary>句柄是否仍然有效。</summary>
        bool IsValid { get; }
    }
}
