using System;
using UnityEngine;

namespace LiteFW
{
    /// <summary>Tag 数据源，仅在管理器构造期间使用。</summary>
    public interface IGameplayTagSource
    {
        /// <summary>向注册器提交标签路径。</summary>
        void Collect(IGameplayTagRegistration registration);
    }

    /// <summary>由字符串数组提供标签路径的数据源。</summary>
    public sealed class NativeGameplayTagSource : IGameplayTagSource
    {
        /// <summary>业务提供的标签路径。</summary>
        private readonly string[] m_Paths;

        /// <summary>用标签路径创建数据源。</summary>
        public NativeGameplayTagSource(params string[] paths)
        {
            m_Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>将路径注册到标签管理器。</summary>
        public void Collect(IGameplayTagRegistration registration)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            for (int i = 0; i < m_Paths.Length; i++)
                registration.Register(m_Paths[i]);
        }
    }

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
