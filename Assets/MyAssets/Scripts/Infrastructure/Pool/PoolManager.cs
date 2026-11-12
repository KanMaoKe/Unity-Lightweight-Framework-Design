using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteFW
{
    /// <summary>按预制体统一创建、复用和释放对象池。</summary>
    public sealed class PoolManager : IDisposable
    {
        #region 业务入口
        /// <summary>创建对象池管理器。</summary>
        public PoolManager(Transform root)
        {
            m_Root = root != null ? root : throw new ArgumentNullException(nameof(root));
        }

        /// <summary>获取或创建指定预制体的对象池。</summary>
        public PrefabPool<T> GetOrCreate<T>(
            T prefab,
            int initialCapacity = 0,
            int minCachedCount = 0) where T : Component
        {
            if (m_IsDisposed)
                throw new ObjectDisposedException(nameof(PoolManager));
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (m_Pools.TryGetValue(prefab, out PoolEntry entry))
            {
                if (entry.InitialCapacity != initialCapacity || entry.MinCachedCount != minCachedCount)
                {
                    throw new InvalidOperationException(
                        $"预制体 {prefab.name} 已使用其他池参数创建：initialCapacity={entry.InitialCapacity}, minCachedCount={entry.MinCachedCount}。");
                }

                if (entry.Pool is PrefabPool<T> existing)
                    return existing;

                throw new InvalidOperationException($"预制体 {prefab.name} 已用其他类型创建过对象池。");
            }

            var pool = new PrefabPool<T>(prefab, m_Root, initialCapacity, minCachedCount);
            m_Pools.Add(prefab, new PoolEntry(prefab, initialCapacity, minCachedCount, pool));
            return pool;
        }

        /// <summary>销毁指定预制体的对象池。</summary>
        public bool Release<T>(T prefab) where T : Component
        {
            if (prefab == null || !m_Pools.TryGetValue(prefab, out PoolEntry entry))
                return false;

            m_Pools.Remove(prefab);
            entry.Dispose();
            return true;
        }
        #endregion

        #region 框架生命周期
        /// <summary>销毁全部对象池。</summary>
        public void Dispose()
        {
            if (m_IsDisposed)
                return;

            m_IsDisposed = true;
            foreach (PoolEntry entry in m_Pools.Values)
                entry.Dispose();
            m_Pools.Clear();
        }
        #endregion

        #region 框架内部实现
        /// <summary>池实例与创建参数的记录。</summary>
        private sealed class PoolEntry : IDisposable
        {
            /// <summary>创建池记录。</summary>
            internal PoolEntry(Component prefab, int initialCapacity, int minCachedCount, IDisposable pool)
            {
                Prefab = prefab;
                InitialCapacity = initialCapacity;
                MinCachedCount = minCachedCount;
                Pool = pool;
            }

            /// <summary>池对应的预制体。</summary>
            internal Component Prefab { get; }

            /// <summary>预热数量。</summary>
            internal int InitialCapacity { get; }

            /// <summary>最少保留的空闲数量。</summary>
            internal int MinCachedCount { get; }

            /// <summary>池实例。</summary>
            internal IDisposable Pool { get; }

            /// <summary>销毁池及其实例。</summary>
            public void Dispose()
            {
                Pool.Dispose();
            }
        }

        /// <summary>预制体到池的映射。</summary>
        private readonly Dictionary<Component, PoolEntry> m_Pools = new Dictionary<Component, PoolEntry>();

        /// <summary>池根节点。</summary>
        private readonly Transform m_Root;

        /// <summary>是否已释放。</summary>
        private bool m_IsDisposed;
        #endregion
    }
}
