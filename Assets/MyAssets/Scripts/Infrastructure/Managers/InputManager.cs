using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 输入采集：负责把玩家输入写入输入黑板。
    /// 具体按键映射随项目而定，框架阶段仅保留相机引用，接入玩法时再补充读取逻辑。
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>缓存的主相机。</summary>
        private Camera mainCamera;

        /// <summary>补齐主相机引用。</summary>
        void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }
    }
}
