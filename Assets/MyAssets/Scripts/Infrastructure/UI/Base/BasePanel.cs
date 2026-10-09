using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 面板基类：定义面板的打开与关闭流程。
    /// </summary>
    public class BasePanel : MonoBehaviour
    {
        protected bool IsRemove = false;

        /// <summary>
        /// 面板名称。
        /// </summary>
        protected string panelName;

        /// <summary>
        /// 打开面板。
        /// </summary>
        public virtual void OpenPanel(string panelName)
        {
            this.panelName = panelName;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 关闭面板：从管理器中移除记录并销毁自身。
        /// </summary>
        public virtual void ClosePanel()
        {
            IsRemove = true;
            UIManager.Instance.NotifyClosed(panelName);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        /// <summary>
        /// 兜底关闭：编辑器里手动禁用对象时，避免管理器残留面板记录。
        /// </summary>
        public virtual void OnDisable()
        {
            ClosePanel();
        }
    }
}
