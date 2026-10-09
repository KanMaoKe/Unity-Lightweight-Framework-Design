using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 基于 Resources 的资源提供器：开发期过渡实现，接入 YooAsset 后替换。
    /// </summary>
    public sealed class ResourcesAssetProvider : IAssetProvider
    {
        /// <summary>
        /// 从 Resources 目录加载资源；加载失败时输出错误并返回 null。
        /// </summary>
        public T Load<T>(string key) where T : Object
        {
            T asset = Resources.Load<T>(key);
            if (asset == null)
                Debug.LogError($"资源加载失败：{key}（类型 {typeof(T).Name}）");
            return asset;
        }
    }
}
