using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Duel
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

    public virtual void OnDisable()
    {
        ClosePanel();
    }
}
}
