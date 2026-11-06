using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 全局事件总线。所有操作应在 Unity 主线程执行。
    /// 发布事件时自动捕获异常，单个订阅者错误不影响其他订阅者。
    /// 发布期间订阅/退订会延迟到本次发布结束后生效（双缓冲），避免遍历时修改集合，同时不产生快照分配。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, object> events = new Dictionary<Type, object>();
        private static readonly Dictionary<ScopedEventKey, object> scopedEvents = new Dictionary<ScopedEventKey, object>();

        /// <summary>
        /// 订阅事件。
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct, IEventMessage
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetOrCreateChannel<T>().Subscribe(handler);
        }

        /// <summary>
        /// 取消订阅。
        /// </summary>
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

        /// <summary>
        /// 发布事件。每个订阅者独立执行，单个异常输出日志但不影响后续订阅者。
        /// </summary>
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

        /// <summary>
        /// 获取或创建某事件类型对应的通道。
        /// </summary>
        private static EventChannel<T> GetOrCreateChannel<T>() where T : struct, IEventMessage
        {
            Type key = typeof(T);
            if (events.TryGetValue(key, out var obj))
                return (EventChannel<T>)obj;

            var channel = new EventChannel<T>();
            events.Add(key, channel);
            return channel;
        }

        /// <summary>
        /// 订阅指定作用域的事件。只有 scope 相同的发布才会触发。
        /// </summary>
        public static void Subscribe<T>(object scope, Action<T> handler) where T : struct, IEventMessage
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetOrCreateScopedChannel<T>(scope).Subscribe(handler);
        }

        /// <summary>
        /// 取消指定作用域的事件订阅。
        /// </summary>
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

        /// <summary>
        /// 发布指定作用域的事件。只有相同 scope 的订阅者会收到。
        /// </summary>
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

        /// <summary>
        /// 获取或创建某作用域下、某事件类型对应的通道。
        /// </summary>
        private static EventChannel<T> GetOrCreateScopedChannel<T>(object scope) where T : struct, IEventMessage
        {
            var key = new ScopedEventKey(scope, typeof(T));
            if (scopedEvents.TryGetValue(key, out var obj))
                return (EventChannel<T>)obj;

            var channel = new EventChannel<T>();
            scopedEvents.Add(key, channel);
            return channel;
        }

        /// <summary>
        /// 作用域键：由「作用域对象 + 事件类型」组成，用于隔离同名事件。
        /// </summary>
        private readonly struct ScopedEventKey : IEquatable<ScopedEventKey>
        {
            private readonly object scope;
            private readonly Type eventType;

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

        /// <summary>
        /// 事件通道：维护某一类型事件的所有订阅者。
        /// 采用双缓冲（写时复制）：发布期间订阅/退订先写入 pending 列表，
        /// 等发布完全结束后再切换生效，既避免遍历中改集合，又无需每次复制快照。
        /// </summary>
        private sealed class EventChannel<T> where T : struct, IEventMessage
        {
            private List<Action<T>> listeners = new List<Action<T>>();
            private List<Action<T>> pending = new List<Action<T>>();

            /// <summary>发布嵌套深度。</summary>
            private int publishDepth;

            /// <summary>发布期间是否有待生效的修改。</summary>
            private bool hasPendingChanges;

            /// <summary>通道是否为空（不在发布中且无订阅者）。</summary>
            public bool IsEmpty => publishDepth == 0 && listeners.Count == 0;

            /// <summary>
            /// 添加订阅者（重复订阅会被忽略）。
            /// </summary>
            public void Subscribe(Action<T> handler)
            {
                List<Action<T>> list = GetMutableListeners();
                if (!list.Contains(handler))
                    list.Add(handler);
            }

            /// <summary>
            /// 移除订阅者。
            /// </summary>
            public void Unsubscribe(Action<T> handler)
            {
                GetMutableListeners().Remove(handler);
            }

            /// <summary>
            /// 通知所有订阅者。
            /// </summary>
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

            /// <summary>
            /// 获取当前可写的列表：不在发布中直接改正式列表；发布中则改待生效列表，发布结束后再切换。
            /// </summary>
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
    }
}
