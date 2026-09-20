namespace Duel
{
/// <summary>
/// 标记接口，用于约束所有事件类型。
/// 作为 struct 的事件类型不会被装箱，因为 EventBus 使用泛型参数。
/// </summary>
public interface IEventMessage
{
}
}
