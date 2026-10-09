using UnityEngine;
namespace LiteFW
{
    /// <summary>
    /// 输入黑板：存储所有玩家输入数据，由 InputManager 写入，其他系统读取。
    /// </summary>
    public class InputBlackboard
    {
        /// <summary>移动输入（WASD，已归一化）。</summary>
        public Vector2 moveInput;

        /// <summary>瞄准位置（鼠标世界坐标）。</summary>
        public Vector3 mouseWorldPos;

        /// <summary>持续按住：冲刺（Shift）。</summary>
        public bool sprintHeld;

        /// <summary>持续按住：开火（鼠标左键）。</summary>
        public bool fireHeld;

        /// <summary>持续按住：近战（鼠标右键）。</summary>
        public bool meleeHeld;

        /// <summary>本帧按下：拾取（E）。</summary>
        public bool EPressed;

        /// <summary>本帧按下：换弹（R）。</summary>
        public bool reloadPressed;

        /// <summary>本帧按下：背包（Tab）。</summary>
        public bool inventoryPressed;

        /// <summary>本帧按下：菜单（Esc）。</summary>
        public bool menuPressed;

        /// <summary>本帧按下的武器槽（数字键 1/2/3），-1 表示本帧没按。</summary>
        public int weaponSlotPressed = -1;
    }
}
