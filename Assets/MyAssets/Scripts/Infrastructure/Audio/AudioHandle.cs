namespace LiteFW
{
    /// <summary>播放句柄：标识一次音频播放，可用于主动停止。</summary>
    public readonly struct AudioHandle
    {
        /// <summary>创建播放句柄。</summary>
        internal AudioHandle(int id)
        {
            Id = id;
        }

        /// <summary>播放标识。</summary>
        internal int Id { get; }

        /// <summary>是否持有有效的播放标识。</summary>
        public bool IsValid => Id > 0;
    }
}
