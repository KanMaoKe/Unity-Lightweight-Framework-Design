using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteFW
{
    /// <summary>黑板管理器：按类型持有黑板，具体黑板由业务侧注册。</summary>
    public class BlackboardManager : MonoBehaviour
    {
        #region 业务入口 / 单例
        /// <summary>全局单例。</summary>
        public static BlackboardManager Instance { get; private set; }
        #endregion

        #region 框架内部实现 / 黑板存储
        /// <summary>类型到黑板实例的映射。</summary>
        private readonly Dictionary<Type, object> m_Blackboards = new Dictionary<Type, object>();
        #endregion

        #region 框架生命周期
        /// <summary>确认单例归属并跨场景保留。</summary>
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        /// <summary>清空黑板并释放单例。</summary>
        void OnDestroy()
        {
            if (Instance != this)
                return;

            m_Blackboards.Clear();
            Instance = null;
        }
        #endregion

        #region 业务入口 / 黑板访问
        /// <summary>注册黑板；同类型重复注册会覆盖。</summary>
        public void Register<T>(T blackboard) where T : class
        {
            m_Blackboards[typeof(T)] = blackboard;
        }

        /// <summary>取黑板；未注册时返回 null。</summary>
        public T Get<T>() where T : class
        {
            return m_Blackboards.TryGetValue(typeof(T), out object blackboard) ? (T)blackboard : null;
        }
        #endregion
    }
}
