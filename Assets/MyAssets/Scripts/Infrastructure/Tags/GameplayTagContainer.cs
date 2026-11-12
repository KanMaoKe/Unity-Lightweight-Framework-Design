using System;
using System.Collections.Generic;

namespace LiteFW
{
    /// <summary>Tag 集合；父子匹配需要标签管理器。</summary>
    public sealed class GameplayTagContainer
    {
        #region 框架内部实现 / 标签存储
        /// <summary>容器内标签的 Id 集合。</summary>
        readonly HashSet<int> m_TagIds = new();
        #endregion

        #region 业务入口 / 集合操作与查询
        /// <summary>容器内标签数量。</summary>
        public int Count => m_TagIds.Count;

        /// <summary>添加标签，无效标签会抛出异常。</summary>
        public void Add(GameplayTag tag)
        {
            if (!tag.IsValid)
                throw new ArgumentException("不能向容器添加 Invalid Tag。", nameof(tag));
            m_TagIds.Add(tag.Id);
        }

        /// <summary>移除标签。</summary>
        public bool Remove(GameplayTag tag) => tag.IsValid && m_TagIds.Remove(tag.Id);

        /// <summary>清空容器。</summary>
        public void Clear() => m_TagIds.Clear();

        /// <summary>精确拥有该 Tag。</summary>
        public bool Has(GameplayTag tag) => tag.IsValid && m_TagIds.Contains(tag.Id);

        /// <summary>是否拥有该 Tag 或其子孙。</summary>
        public bool Has(GameplayTag tag, bool includeChildren, IGameplayTagManager manager)
        {
            if (!tag.IsValid)
                return false;
            if (m_TagIds.Contains(tag.Id))
                return true;
            if (!includeChildren)
                return false;
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            foreach (int ownedId in m_TagIds)
            {
                if (manager.IsChildOf(new GameplayTag(ownedId), tag))
                    return true;
            }

            return false;
        }

        /// <summary>是否拥有任意一个指定标签。</summary>
        public bool HasAny(IEnumerable<GameplayTag> tags, bool includeChildren, IGameplayTagManager manager)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            foreach (GameplayTag tag in tags)
            {
                if (Has(tag, includeChildren, manager))
                    return true;
            }

            return false;
        }

        /// <summary>是否拥有全部指定标签。</summary>
        public bool HasAll(IEnumerable<GameplayTag> tags, bool includeChildren, IGameplayTagManager manager)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            foreach (GameplayTag tag in tags)
            {
                if (!Has(tag, includeChildren, manager))
                    return false;
            }

            return true;
        }

        /// <summary>枚举容器内的标签。</summary>
        public IEnumerable<GameplayTag> Enumerate()
        {
            foreach (int id in m_TagIds)
                yield return new GameplayTag(id);
        }
        #endregion
    }
}
