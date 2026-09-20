using System;

namespace Duel
{
    public enum AbilityType
    {
        Move,//移动
        Rotate,//旋转
        Fire,//射击
        Reload,//重新装填
        Pickup//拾取
    }

    /// <summary>锁来源：谁在锁。后续状态效果（如眩晕）再加。</summary>
    public enum AbilityLockSource
    {
        Death,
        Reloading,
        InventoryOpen
    }

    /// <summary>
    /// 玩家仲裁黑板（纯本地，不进网络）：
    /// 和输入黑板一样由 BlackboardManager 持有， v/.
    /// 内部用位掩码：每个能力一个 int，每个 bit 代表一个锁来源，非 0 = 被锁。
    /// 对外查询统一是 bool。
    /// </summary>
    public class ArbiterBlackboard
    {
        // 每个能力一个位掩码：bit 位 = (int)AbilityLockSource
        private readonly int[] locks;

        public ArbiterBlackboard()
        {
            var types = Enum.GetValues(typeof(AbilityType));
            locks = new int[types.Length];
        }

        #region 查询（统一 bool）

        public bool Can(AbilityType ability) => locks[(int)ability] == 0;

        public bool CanMove => Can(AbilityType.Move);
        public bool CanRotate => Can(AbilityType.Rotate);
        public bool CanFire => Can(AbilityType.Fire);
        public bool CanReload => Can(AbilityType.Reload);
        public bool CanPickup => Can(AbilityType.Pickup);

        /// <summary>返回当前阻塞该能力的来源（供 UI 显示原因），没被锁返回 null</summary>
        public AbilityLockSource? GetBlockingSource(AbilityType ability)
        {
            int mask = locks[(int)ability];
            if (mask == 0) return null;

            // 返回最低位的来源（结果确定）
            int bit = 0;
            while (((mask >> bit) & 1) == 0) bit++;
            return (AbilityLockSource)bit;
        }

        #endregion

        #region 加锁 / 解锁

        /// <summary>加锁（同一来源重复加锁是幂等的）</summary>
        public void Lock(AbilityType ability, AbilityLockSource source)
        {
            locks[(int)ability] |= 1 << (int)source;
        }

        /// <summary>解锁</summary>
        public void Unlock(AbilityType ability, AbilityLockSource source)
        {
            locks[(int)ability] &= ~(1 << (int)source);
        }

        /// <summary>用同一来源一次性锁/解锁所有能力（如死亡时全锁、复活时全解）</summary>
        public void SetAll(AbilityLockSource source, bool locked)
        {
            int bit = 1 << (int)source;
            for (int i = 0; i < locks.Length; i++)
            {
                if (locked) locks[i] |= bit;
                else locks[i] &= ~bit;
            }
        }

        #endregion
    }
}
