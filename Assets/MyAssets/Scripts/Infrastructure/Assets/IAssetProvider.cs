using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteFW
{
    /// <summary>统一资源加载与句柄释放契约。</summary>
    public interface IAssetProvider
    {
        /// <summary>按资源 key 异步加载；调用方负责释放句柄。</summary>
        UniTask<IAssetHandle<T>> LoadAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;
    }

    /// <summary>请求的资源不存在。</summary>
    public sealed class AssetNotFoundException : InvalidOperationException
    {
        /// <summary>创建资源不存在异常。</summary>
        public AssetNotFoundException(string message) : base(message)
        {
        }
    }

    /// <summary>已加载资源的持有句柄。</summary>
    public interface IAssetHandle<T> : IDisposable where T : UnityEngine.Object
    {
        /// <summary>加载完成的资源。</summary>
        T Asset { get; }

        /// <summary>句柄是否仍然有效。</summary>
        bool IsValid { get; }
    }
}
