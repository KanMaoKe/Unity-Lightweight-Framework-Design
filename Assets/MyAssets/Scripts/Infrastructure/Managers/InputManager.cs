using UnityEngine;

namespace LiteFW
{
    /// <summary>输入服务占位：当前仅维护主相机引用。</summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>缓存的主相机。</summary>
        private Camera mainCamera;

        /// <summary>缓存主相机；丢失后重新获取。</summary>
        void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }
    }
}
