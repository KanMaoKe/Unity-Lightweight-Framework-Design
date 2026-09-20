using System;
using System.Collections.Generic;
using UnityEngine;

namespace Duel
{
/// <summary>
/// 全局事件总线。所有操作应在 Unity 主线程执行。
/// 发布事件时自动捕获异常，单个订阅者错误不影响其他订阅者。
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> events = new Dictionary<Type, Delegate>();

    /// <summary>
    /// 订阅事件
    /// </summary>
    public static void Subscribe<T>(Action<T> handler) where T : IEventMessage
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        Type key = typeof(T);
        if (events.TryGetValue(key, out var existingDelegate))
            events[key] = Delegate.Combine(existingDelegate, handler);
        else
            events[key] = handler;
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : IEventMessage
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        Type key = typeof(T);
        if (events.TryGetValue(key, out var existingDelegate))
        {
            var newDelegate = Delegate.Remove(existingDelegate, handler);
            if (newDelegate == null)
                events.Remove(key);
            else
                events[key] = newDelegate;
        }
    }

    /// <summary>
    /// 发布事件。每个订阅者独立执行，单个异常输出日志但不影响后续订阅者。
    /// </summary>
    public static void Publish<T>(T eventData) where T : IEventMessage
    {
        Type key = typeof(T);
        if (!events.TryGetValue(key, out var delegateObj))
            return;

        if (delegateObj is Action<T> handler)
        {
            foreach (Action<T> singleHandler in handler.GetInvocationList())
            {
                try
                {
                    singleHandler?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"事件处理器异常 - 事件类型: {typeof(T).Name}, 错误: {e}");
                }
            }
        }
    }
}
}
