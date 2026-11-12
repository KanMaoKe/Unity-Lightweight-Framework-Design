using System;
using System.Collections.Generic;
namespace LiteFW
{
    /// <summary>批量管理订阅并统一取消。</summary>
    public class EventGroup
    {
        /// <summary>登记的退订动作。</summary>
        private readonly List<Action> unsubscribeActions = new List<Action>();

        /// <summary>订阅事件并记录退订动作。</summary>
        public void Subscribe<T>(Action<T> handler) where T : struct, IEventMessage
        {
            EventBus.Subscribe(handler);
            unsubscribeActions.Add(() => EventBus.Unsubscribe(handler));
        }

        /// <summary>订阅作用域事件并记录退订动作。</summary>
        public void Subscribe<T>(object scope, Action<T> handler) where T : struct, IEventMessage
        {
            EventBus.Subscribe(scope, handler);
            unsubscribeActions.Add(() => EventBus.Unsubscribe(scope, handler));
        }

        /// <summary>取消该组内全部订阅。</summary>
        public void UnsubscribeAll()
        {
            foreach (var action in unsubscribeActions)
                action();
            unsubscribeActions.Clear();
        }
    }
}
