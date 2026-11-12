using UnityEngine;

namespace LiteFW
{
    /// <summary>输入服务占位：当前仅维护主相机引用。</summary>
    public class InputManager : MonoBehaviour
    {
        #region 框架内部实现 / 相机状态
        /// <summary>缓存的主相机。</summary>
        private Camera mainCamera;
        #endregion

        #region 框架生命周期
        /// <summary>缓存主相机；丢失后重新获取。</summary>
        void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }
        #endregion
    }
}
