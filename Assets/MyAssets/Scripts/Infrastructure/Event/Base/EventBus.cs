using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LiteFW
{
    /// <summary>事件数据标记接口，实现类型必须为 struct。</summary>
    public interface IEventMessage
    {
    }

    /// <summary>
    /// 全局事件总线；仅限 Unity 主线程使用。
    /// 发布期间变更订阅会延迟到发布结束生效。
    /// </summary>
    public static class EventBus
    {
        #region 业务入口
        /// <summary>订阅事件。</summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct, IEventMessage
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetOrCreateChannel<T>().Subscribe(handler);
        }

        /// <summary>取消订阅。</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IEventMessage
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            Type key = typeof(T);
            if (events.TryGetValue(key, out var obj))
            {
                var channel = (EventChannel<T>)obj;
                channel.Unsubscribe(handler);
                if (channel.IsEmpty)
                    events.Remove(key);
            }
        }

        /// <summary>发布事件；单个订阅者异常不影响其他订阅者。</summary>
        public static void Publish<T>(T eventData) where T : struct, IEventMessage
        {
            Type key = typeof(T);
            if (!events.TryGetValue(key, out var obj))
                return;

            var channel = (EventChannel<T>)obj;
            channel.Publish(eventData);
            if (channel.IsEmpty)
                events.Remove(key);
        }
        /// <summary>订阅作用域事件；仅相同 scope 的发布触发。</summary>
        public static void Subscribe<T>(object scope, Action<T> handler) where T : struct, IEventMessage
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetOrCreateScopedChannel<T>(scope).Subscribe(handler);
        }

        /// <summary>取消作用域事件订阅。</summary>
        public static void Unsubscribe<T>(object scope, Action<T> handler) where T : struct, IEventMessage
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var key = new ScopedEventKey(scope, typeof(T));
            if (scopedEvents.TryGetValue(key, out var obj))
            {
                var channel = (EventChannel<T>)obj;
                channel.Unsubscribe(handler);
                if (channel.IsEmpty)
                    scopedEvents.Remove(key);
            }
        }

        /// <summary>发布作用域事件；仅相同 scope 的订阅者收到。</summary>
        public static void Publish<T>(object scope, T eventData) where T : struct, IEventMessage
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            var key = new ScopedEventKey(scope, typeof(T));
            if (!scopedEvents.TryGetValue(key, out var obj))
                return;

            var channel = (EventChannel<T>)obj;
            channel.Publish(eventData);
            if (channel.IsEmpty)
                scopedEvents.Remove(key);
        }

        /// <summary>清理指定作用域下的全部订阅。</summary>
        public static void ClearScope(object scope)
        {
            if (scope == null)
                return;

            var keys = new List<ScopedEventKey>();
            foreach (ScopedEventKey key in scopedEvents.Keys)
            {
                if (ReferenceEquals(key.Scope, scope))
                    keys.Add(key);
            }

            for (int i = 0; i < keys.Count; i++)
                scopedEvents.Remove(keys[i]);
        }

        /// <summary>清理全部事件订阅。</summary>
        public static void Clear()
        {
            events.Clear();
            scopedEvents.Clear();
        }
        #endregion

        #region 框架内部实现
        /// <summary>事件类型到通道的映射。</summary>
        private static readonly Dictionary<Type, object> events = new Dictionary<Type, object>();
        /// <summary>作用域键到通道的映射。</summary>
        private static readonly Dictionary<ScopedEventKey, object> scopedEvents = new Dictionary<ScopedEventKey, object>();

        /// <summary>获取或创建事件通道。</summary>
        private static EventChannel<T> GetOrCreateChannel<T>() where T : struct, IEventMessage
        {
            Type key = typeof(T);
            if (events.TryGetValue(key, out var obj))
                return (EventChannel<T>)obj;

            var channel = new EventChannel<T>();
            events.Add(key, channel);
            return channel;
        }

        /// <summary>获取或创建作用域事件通道。</summary>
        private static EventChannel<T> GetOrCreateScopedChannel<T>(object scope) where T : struct, IEventMessage
        {
            var key = new ScopedEventKey(scope, typeof(T));
            if (scopedEvents.TryGetValue(key, out var obj))
                return (EventChannel<T>)obj;

            var channel = new EventChannel<T>();
            scopedEvents.Add(key, channel);
            return channel;
        }

        /// <summary>作用域键：由作用域对象与事件类型组成。</summary>
        private readonly struct ScopedEventKey : IEquatable<ScopedEventKey>
        {
            /// <summary>事件隔离的作用域对象。</summary>
            private readonly object scope;
            /// <summary>通道对应的事件类型。</summary>
            private readonly Type eventType;

            /// <summary>作用域对象。</summary>
            public object Scope => scope;

            /// <summary>创建作用域键。</summary>
            public ScopedEventKey(object scope, Type eventType)
            {
                this.scope = scope;
                this.eventType = eventType;
            }

            /// <summary>按作用域引用与事件类型比较。</summary>
            public bool Equals(ScopedEventKey other)
            {
                return ReferenceEquals(scope, other.scope) && eventType == other.eventType;
            }

            /// <summary>按作用域引用与事件类型比较。</summary>
            public override bool Equals(object obj)
            {
                return obj is ScopedEventKey other && Equals(other);
            }

            /// <summary>计算作用域键的哈希。</summary>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(scope) * 397) ^ eventType.GetHashCode();
                }
            }
        }

        /// <summary>单一事件类型的订阅者通道。</summary>
        private sealed class EventChannel<T> where T : struct, IEventMessage
        {
            /// <summary>当前生效的订阅者。</summary>
            private List<Action<T>> listeners = new List<Action<T>>();
            /// <summary>发布期间暂存的订阅变更。</summary>
            private List<Action<T>> pending = new List<Action<T>>();

            /// <summary>发布嵌套深度。</summary>
            private int publishDepth;

            /// <summary>发布期间是否有待生效的修改。</summary>
            private bool hasPendingChanges;

            /// <summary>通道是否为空（不在发布中且无订阅者）。</summary>
            public bool IsEmpty => publishDepth == 0 && listeners.Count == 0;

            /// <summary>添加订阅者；重复订阅会被忽略。</summary>
            public void Subscribe(Action<T> handler)
            {
                List<Action<T>> list = GetMutableListeners();
                if (!list.Contains(handler))
                    list.Add(handler);
            }

            /// <summary>移除订阅者。</summary>
            public void Unsubscribe(Action<T> handler)
            {
                GetMutableListeners().Remove(handler);
            }

            /// <summary>通知全部订阅者。</summary>
            public void Publish(T eventData)
            {
                publishDepth++;
                try
                {
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        Action<T> handler = listeners[i];
                        try
                        {
                            handler?.Invoke(eventData);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"事件处理器异常 - 事件类型: {typeof(T).Name}, 错误: {e}");
                        }
                    }
                }
                finally
                {
                    publishDepth--;
                    if (publishDepth == 0 && hasPendingChanges)
                    {
                        (listeners, pending) = (pending, listeners);
                        pending.Clear();
                        hasPendingChanges = false;
                    }
                }
            }

            /// <summary>获取当前可写的订阅列表。</summary>
            private List<Action<T>> GetMutableListeners()
            {
                if (publishDepth == 0)
                    return listeners;

                if (!hasPendingChanges)
                {
                    pending.AddRange(listeners);
                    hasPendingChanges = true;
                }
                return pending;
            }
        }
        #endregion
    }
}
