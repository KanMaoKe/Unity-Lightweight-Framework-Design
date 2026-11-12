namespace LiteFW
{
    /// <summary>对象池租借与归还回调。</summary>
    public interface IPoolLifecycle
    {
        /// <summary>对象被取出时调用。</summary>
        void OnPoolRent();

        /// <summary>对象被归还时调用。</summary>
        void OnPoolReturn();
    }
}
