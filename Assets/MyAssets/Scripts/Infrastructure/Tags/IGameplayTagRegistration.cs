namespace LiteFW
{
    /// <summary>
    /// 构建期向 Manager 注册路径；运行时密封后不可再注册。
    /// </summary>
    public interface IGameplayTagRegistration
    {
        /// <summary>注册一条标签路径。</summary>
        void Register(string path);
    }
}
