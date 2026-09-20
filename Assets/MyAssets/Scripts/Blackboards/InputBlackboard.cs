using UnityEngine;

namespace Duel
{
/// <summary>
/// 输入黑板：存储所有玩家输入数据，由 InputManager 写入，其他系统读取。
/// </summary>
public class InputBlackboard
{
    // 移动（WASD，归一化向量）
    public Vector2 moveInput;

    // 瞄准（鼠标世界坐标）
    public Vector3 mouseWorldPos;

    // 持续按住
    public bool sprintHeld;    // Shift
    public bool fireHeld;      // 鼠标左键
    public bool meleeHeld;     // 鼠标右键

    // 单次按下（这一帧触发）
    [Tooltip("本帧是否按下 E 键")]
    public bool EPressed;          // E
    [Tooltip("本帧是否按下 R 键")]
    public bool reloadPressed;     // R
    [Tooltip("本帧是否按下 Tab 键")]
    public bool inventoryPressed;  // Tab
    [Tooltip("本帧是否按下 Esc 键")]
    public bool menuPressed;       // Esc
    [Tooltip("本帧按下的数字键武器槽，-1 表示没按")]
    public int weaponSlotPressed = -1;   // 数字键 1/2/3 对应武器槽，-1 表示本帧没按
}
}
