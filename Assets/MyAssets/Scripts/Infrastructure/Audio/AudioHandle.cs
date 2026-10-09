namespace LiteFW
{
    /// <summary>
    /// 播放句柄：标识一次音频播放，可用于主动停止。
    /// </summary>
    public readonly struct AudioHandle
    {
        internal AudioHandle(int id)
        {
            Id = id;
        }

        internal int Id { get; }

        /// <summary>
        /// 是否持有有效的播放标识。
        /// </summary>
        public bool IsValid => Id > 0;
    }
}
