using System;
using System.Collections.Generic;

namespace Duel
{
/// <summary>
/// 事件组：自动管理多个订阅，便于一次性取消全部。
/// 典型用法：在 MonoBehaviour 的 OnEnable 中调用 Subscribe，在 OnDisable 中调用 UnsubscribeAll。
/// </summary>
public class EventGroup
{
    private readonly List<Action> unsubscribeActions = new List<Action>();

    /// <summary>
    /// 订阅事件并自动记录，以便后续批量取消
    /// </summary>
    public void Subscribe<T>(Action<T> handler) where T : IEventMessage
    {
        EventBus.Subscribe(handler);
        unsubscribeActions.Add(() => EventBus.Unsubscribe(handler));
    }

    /// <summary>
    /// 取消该组内所有订阅
    /// </summary>
    public void UnsubscribeAll()
    {
        foreach (var action in unsubscribeActions)
            action();
        unsubscribeActions.Clear();
    }
}
}
