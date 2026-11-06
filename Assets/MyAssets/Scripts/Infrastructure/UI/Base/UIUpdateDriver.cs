using UnityEngine;

namespace LiteFW
{
    /// <summary>把 Unity Update 转发给 UI 管理器。</summary>
    internal sealed class UIUpdateDriver : MonoBehaviour
    {
        /// <summary>每帧驱动面板更新。</summary>
        private void Update()
        {
            if (FrameworkBootstrap.Instance != null && FrameworkBootstrap.Instance.UI != null)
                FrameworkBootstrap.Instance.UI.Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
}
