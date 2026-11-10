using System;
using UnityEngine;

namespace LiteFW
{
    /// <summary>业务标签路径配置。</summary>
    [CreateAssetMenu(fileName = "GameplayTagCatalog", menuName = "LiteFW/Tags/Tag Catalog")]
    public sealed class GameplayTagCatalog : ScriptableObject
    {
        /// <summary>业务提供的标签路径。</summary>
        [SerializeField] private string[] m_Paths = Array.Empty<string>();

        /// <summary>创建标签数据源。</summary>
        public IGameplayTagSource CreateSource()
        {
            return new NativeGameplayTagSource(m_Paths);
        }
    }
}
