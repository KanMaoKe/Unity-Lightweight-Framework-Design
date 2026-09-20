using UnityEngine;

namespace Duel
{
public class InputManager : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        var input = BlackboardManager.Instance.Input;

        // 移动（WASD，归一化）
        float h = UnityEngine.Input.GetAxisRaw("Horizontal");
        float v = UnityEngine.Input.GetAxisRaw("Vertical");
        input.moveInput = new Vector2(h, v).normalized;

        // 瞄准（鼠标世界坐标）
        if (mainCamera != null)
            input.mouseWorldPos = mainCamera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);

        // 持续按住
        input.sprintHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift);
        input.fireHeld = UnityEngine.Input.GetMouseButton(0);
        input.meleeHeld = UnityEngine.Input.GetMouseButton(1);

        // 单次按下
        input.EPressed = UnityEngine.Input.GetKeyDown(KeyCode.E);
        input.reloadPressed = UnityEngine.Input.GetKeyDown(KeyCode.R);
        input.inventoryPressed = UnityEngine.Input.GetKeyDown(KeyCode.Tab);
        input.menuPressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        // 切武器：数字键 1/2/3
        input.weaponSlotPressed = -1;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) input.weaponSlotPressed = 0;
        else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) input.weaponSlotPressed = 1;
        else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) input.weaponSlotPressed = 2;
    }
}
}
