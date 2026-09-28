using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LiteFW
{
    public class BasePanel : MonoBehaviour
    {
        protected bool IsRemove = false;

        protected new string name;

        public virtual void OpenPanel(string name)
        {
            this.name = name;
            gameObject.SetActive(true);
        }

        public virtual void ClosePanel()
        {
            IsRemove = true;
            gameObject.SetActive(false);

            if (UIManager.Instance.PanelDict.ContainsKey(name))
            {
                UIManager.Instance.PanelDict.Remove(name);
            }

            Destroy(gameObject);
        }

        //按理来不需要，加的一个补丁，有时候编辑器窗口关掉了，导致没有按流程走的ClosePanel，就会导致，后续重新开启的时候字典里会有缓存
        public virtual void OnDisable()
        {
            ClosePanel();
        }
    }
}