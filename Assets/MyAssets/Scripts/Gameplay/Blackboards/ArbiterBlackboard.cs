using System;

namespace LiteFW
{
    /// <summary>
    /// 能力类型。
    /// </summary>
    public enum AbilityType
    {
        /// <summary>移动。</summary>
        Move,

        /// <summary>旋转。</summary>
        Rotate,

        /// <summary>射击。</summary>
        Fire,

        /// <summary>重新装填。</summary>
        Reload,

        /// <summary>拾取。</summary>
        Pickup
    }

    /// <summary>
    /// 能力锁来源：谁在锁。后续状态效果（如眩晕）再加。
    /// </summary>
    public enum AbilityLockSource
    {
        /// <summary>死亡。</summary>
        Death,

        /// <summary>换弹中。</summary>
        Reloading,

        /// <summary>背包打开。</summary>
        InventoryOpen
    }

    /// <summary>
    /// 玩家仲裁黑板（纯本地，不进网络）：和输入黑板一样由 BlackboardManager 持有。
    /// 内部用位掩码：每个能力一个 int，每个 bit 代表一个锁来源，非 0 表示被锁。
    /// 对外查询统一为 bool。
    /// </summary>
    public class ArbiterBlackboard
    {
        private readonly int[] locks;

        public ArbiterBlackboard()
        {
            var types = Enum.GetValues(typeof(AbilityType));
            locks = new int[types.Length];
        }

        /// <summary>查询指定能力是否可用。</summary>
        public bool Can(AbilityType ability) => locks[(int)ability] == 0;

        /// <summary>是否可移动。</summary>
        public bool CanMove => Can(AbilityType.Move);

        /// <summary>是否可旋转。</summary>
        public bool CanRotate => Can(AbilityType.Rotate);

        /// <summary>是否可开火。</summary>
        public bool CanFire => Can(AbilityType.Fire);

        /// <summary>是否可换弹。</summary>
        public bool CanReload => Can(AbilityType.Reload);

        /// <summary>是否可拾取。</summary>
        public bool CanPickup => Can(AbilityType.Pickup);

        /// <summary>
        /// 返回当前阻塞该能力的来源（供 UI 显示原因）；未被锁时返回 null。
        /// </summary>
        public AbilityLockSource? GetBlockingSource(AbilityType ability)
        {
            int mask = locks[(int)ability];
            if (mask == 0) return null;

            int bit = 0;
            while (((mask >> bit) & 1) == 0) bit++;
            return (AbilityLockSource)bit;
        }

        /// <summary>加锁（同一来源重复加锁是幂等的）。</summary>
        public void Lock(AbilityType ability, AbilityLockSource source)
        {
            locks[(int)ability] |= 1 << (int)source;
        }

        /// <summary>解锁。</summary>
        public void Unlock(AbilityType ability, AbilityLockSource source)
        {
            locks[(int)ability] &= ~(1 << (int)source);
        }

        /// <summary>用同一来源一次性锁/解锁所有能力（如死亡时全锁、复活时全解）。</summary>
        public void SetAll(AbilityLockSource source, bool locked)
        {
            int bit = 1 << (int)source;
            for (int i = 0; i < locks.Length; i++)
            {
                if (locked) locks[i] |= bit;
                else locks[i] &= ~bit;
            }
        }
    }
}
