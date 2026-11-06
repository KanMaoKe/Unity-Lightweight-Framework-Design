using System;

namespace LiteFW
{
    /// <summary>
    /// 已注册 GameplayTag 的轻量句柄；相等比较基于稳定 Id。
    /// </summary>
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        /// <summary>无效标签。</summary>
        public static readonly GameplayTag Invalid = default;

        readonly int m_Id;

        /// <summary>用内部 Id 创建标签。</summary>
        internal GameplayTag(int id)
        {
            m_Id = id;
        }

        /// <summary>内部稳定 Id；0 表示无效。</summary>
        public int Id => m_Id;

        /// <summary>标签是否有效。</summary>
        public bool IsValid => m_Id > 0;

        /// <summary>按 Id 比较标签。</summary>
        public bool Equals(GameplayTag other) => m_Id == other.m_Id;

        /// <summary>按 Id 比较标签。</summary>
        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);

        /// <summary>计算标签哈希。</summary>
        public override int GetHashCode() => m_Id;

        public static bool operator ==(GameplayTag left, GameplayTag right) => left.m_Id == right.m_Id;

        public static bool operator !=(GameplayTag left, GameplayTag right) => left.m_Id != right.m_Id;

        /// <summary>输出标签调试信息。</summary>
        public override string ToString() => IsValid ? $"GameplayTag({m_Id})" : "GameplayTag(Invalid)";
    }
}
