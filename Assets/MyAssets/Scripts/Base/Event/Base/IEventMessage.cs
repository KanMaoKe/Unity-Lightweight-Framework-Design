namespace LiteFW
{
    /// <summary>
    /// 事件数据标记接口。
    /// 注意：实现此接口的事件类型必须是 struct（值类型）。
    /// 事件声明为 struct 后，发布时走值传递、零装箱、零堆分配，避免高频事件触发 GC。
    /// </summary>
    public interface IEventMessage
    {
    }
}
