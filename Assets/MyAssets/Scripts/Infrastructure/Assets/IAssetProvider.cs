using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 资源加载抽象：业务不直接依赖具体加载方式（Resources / YooAsset 可切换）。
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// 按资源 key 加载资源；key 为 Resources 相对路径，如 "Audio/HitSound"。
        /// </summary>
        T Load<T>(string key) where T : Object;
    }
}
