using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 通用 prefab 组件对象池：预热复用实例，并按使用压力动态调整缓存数量。
    /// </summary>
    public sealed class PrefabPool<T> : IDisposable where T : Component
    {
        const float k_CachePressureFactor = 0.75f;
        const float k_CacheDecayDelay = 10f;
        const float k_CacheDecayInterval = 5f;

        readonly T m_Prefab;
        readonly Transform m_PoolRoot;
        readonly string m_InstanceName;
        readonly int m_InitialCapacity;
        readonly int m_MinCachedCount;
        readonly Stack<T> m_FreeItems;
        readonly List<T> m_ClonedItems;
        readonly HashSet<T> m_RentedItems;

        bool m_Prewarmed;
        int m_ActiveCount;
        int m_TargetCachedCount;
        float m_LastPressureTime;
        float m_LastDecayTime;

        /// <param name="prefab">池化对象的模板 prefab。</param>
        /// <param name="poolRoot">空闲实例的挂载节点。</param>
        /// <param name="initialCapacity">预热数量。</param>
        /// <param name="minCachedCount">最少保留的空闲实例数量。</param>
        /// <param name="instanceName">实例名，默认取 prefab 名。</param>
        public PrefabPool(
            T prefab,
            Transform poolRoot,
            int initialCapacity = 0,
            int minCachedCount = 0,
            string instanceName = null)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "初始容量不能小于 0。");
            if (minCachedCount < 0)
                throw new ArgumentOutOfRangeException(nameof(minCachedCount), "最小缓存数量不能小于 0。");

            m_Prefab = prefab;
            m_PoolRoot = poolRoot;
            m_InstanceName = string.IsNullOrWhiteSpace(instanceName) ? prefab.name : instanceName;
            m_InitialCapacity = initialCapacity;
            m_MinCachedCount = minCachedCount;
            m_TargetCachedCount = Mathf.Max(m_InitialCapacity, m_MinCachedCount);
            m_FreeItems = new Stack<T>(m_InitialCapacity);
            m_ClonedItems = new List<T>(m_InitialCapacity);
            m_RentedItems = new HashSet<T>();
        }

        /// <summary>池当前拥有的实例总数（含借出与空闲）。</summary>
        public int CountAll => m_ClonedItems.Count;

        /// <summary>当前已借出（使用中）的实例数量。</summary>
        public int CountActive => m_ActiveCount;

        /// <summary>当前空闲（缓存中）的实例数量。</summary>
        public int CountInactive => m_FreeItems.Count;

        /// <summary>
        /// 预热：提前创建初始容量的空闲实例。
        /// </summary>
        public void Prewarm()
        {
            if (m_Prewarmed)
                return;

            for (int i = 0; i < m_InitialCapacity; i++)
                m_FreeItems.Push(CreateClone());

            m_Prewarmed = true;
        }

        /// <summary>
        /// 取出一个实例：池中有空闲则复用，否则新建。
        /// </summary>
        public T Get(Transform parent)
        {
            Prewarm();

            T item = m_FreeItems.Count > 0
                ? m_FreeItems.Pop()
                : CreateClone();

            m_RentedItems.Add(item);
            m_ActiveCount++;
            RaiseCachedTarget();
            item.transform.SetParent(parent, false);
            item.gameObject.SetActive(true);
            NotifyRent(item);
            return item;
        }

        /// <summary>
        /// 归还实例：超过目标缓存数量的实例会被销毁。
        /// </summary>
        public void Release(T item)
        {
            if (!item)
                return;

            if (!m_RentedItems.Remove(item))
                throw new InvalidOperationException($"{nameof(PrefabPool<T>)} 收到未租借或重复归还的实例：{item.name}");

            m_ActiveCount--;
            DecayCachedTarget();
            NotifyReturn(item);
            item.gameObject.SetActive(false);
            item.transform.SetParent(m_PoolRoot, false);
            if (m_FreeItems.Count >= m_TargetCachedCount)
            {
                DestroyClone(item);
                return;
            }

            m_FreeItems.Push(item);
        }

        /// <summary>
        /// 销毁池中全部实例并复位状态。
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < m_ClonedItems.Count; i++)
            {
                if (m_ClonedItems[i])
                    UnityEngine.Object.Destroy(m_ClonedItems[i].gameObject);
            }

            m_ClonedItems.Clear();
            m_FreeItems.Clear();
            m_RentedItems.Clear();

            m_Prewarmed = false;
            m_ActiveCount = 0;
            m_TargetCachedCount = Mathf.Max(m_InitialCapacity, m_MinCachedCount);
            m_LastPressureTime = 0f;
            m_LastDecayTime = 0f;
        }

        T CreateClone()
        {
            T clone = UnityEngine.Object.Instantiate(m_Prefab, m_PoolRoot, false);
            clone.name = m_InstanceName;
            clone.gameObject.SetActive(false);
            m_ClonedItems.Add(clone);
            return clone;
        }

        /// <summary>
        /// 按当前活跃数量提升目标缓存数量：活跃越多，预留越多。
        /// </summary>
        void RaiseCachedTarget()
        {
            int demandedCachedCount = Mathf.CeilToInt(m_ActiveCount * k_CachePressureFactor);
            if (demandedCachedCount <= m_TargetCachedCount)
                return;

            m_TargetCachedCount = Mathf.Max(m_MinCachedCount, demandedCachedCount);
            m_LastPressureTime = Time.unscaledTime;
            m_LastDecayTime = m_LastPressureTime;
        }

        /// <summary>
        /// 压力消退后逐步下调目标缓存数量，直到最小缓存数量。
        /// </summary>
        void DecayCachedTarget()
        {
            if (m_TargetCachedCount <= m_MinCachedCount)
                return;

            float now = Time.unscaledTime;
            if (now - m_LastPressureTime < k_CacheDecayDelay)
                return;

            if (now - m_LastDecayTime < k_CacheDecayInterval)
                return;

            int demandedCachedCount = Mathf.CeilToInt(m_ActiveCount * k_CachePressureFactor);
            int nextCachedCount = Mathf.Max(m_MinCachedCount, m_TargetCachedCount - 1);
            nextCachedCount = Mathf.Max(nextCachedCount, demandedCachedCount);
            if (nextCachedCount >= m_TargetCachedCount)
                return;

            m_TargetCachedCount = nextCachedCount;
            m_LastDecayTime = now;
        }

        void DestroyClone(T item)
        {
            m_ClonedItems.Remove(item);
            UnityEngine.Object.Destroy(item.gameObject);
        }

        static void NotifyRent(T item)
        {
            if (item is IPoolLifecycle lifecycle)
                lifecycle.OnPoolRent();
        }

        static void NotifyReturn(T item)
        {
            if (item is IPoolLifecycle lifecycle)
                lifecycle.OnPoolReturn();
        }
    }
}
