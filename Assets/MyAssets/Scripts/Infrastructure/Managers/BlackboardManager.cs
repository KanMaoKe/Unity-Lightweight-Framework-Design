using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 黑板管理器：全局单例，按类型持有黑板；具体黑板由业务侧注册。
    /// </summary>
    public class BlackboardManager : MonoBehaviour
    {
        /// <summary>全局单例。</summary>
        public static BlackboardManager Instance { get; private set; }

        private readonly Dictionary<Type, object> m_Blackboards = new Dictionary<Type, object>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;

            m_Blackboards.Clear();
            Instance = null;
        }

        /// <summary>
        /// 注册黑板（同类型重复注册会覆盖）。
        /// </summary>
        public void Register<T>(T blackboard) where T : class
        {
            m_Blackboards[typeof(T)] = blackboard;
        }

        /// <summary>
        /// 取黑板；未注册时返回 null。
        /// </summary>
        public T Get<T>() where T : class
        {
            return m_Blackboards.TryGetValue(typeof(T), out object blackboard) ? (T)blackboard : null;
        }
    }
}
