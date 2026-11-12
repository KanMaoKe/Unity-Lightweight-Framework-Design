namespace LiteFW
{
    /// <summary>Tag 数据源，仅在管理器构造期间使用。</summary>
    public interface IGameplayTagSource
    {
        /// <summary>向注册器提交标签路径。</summary>
        void Collect(IGameplayTagRegistration registration);
    }
}
