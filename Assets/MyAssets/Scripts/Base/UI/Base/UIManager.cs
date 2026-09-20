using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Duel
{
public class UIManager
{
    //单例
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new UIManager();
            }
            return _instance;
        }
    }
    //所有Panel的父类
    private Transform _uiroot;
    public Transform UIRoot
    {
        get
        {
            if( _uiroot == null)
            {
                // 按需面板层：优先 PanelLayer，找不到再退回旧命名 Canvas
                var layer = GameObject.Find("PanelLayer");
                var canvas = layer != null ? layer : GameObject.Find("Canvas");
                _uiroot = canvas != null ? canvas.transform : null;
            }
            return _uiroot;
        }
    }
    //路径的字典
    public Dictionary<string, string> PathDict;
    //预制体
    public Dictionary<string, GameObject> PrefabDict;
    //panel
    public Dictionary<string, BasePanel> PanelDict;

    //初始化
    private UIManager()
    {
        InitDicts();
    }

    //初始化字典
    private void InitDicts()
    {
        PrefabDict = new Dictionary<string, GameObject>();
        PanelDict = new Dictionary<string, BasePanel>();
        PathDict = new Dictionary<string, string>()
        {
            {UICount.MainPanel, "Prefabs/UIPanel/MainPanel" },
            {UICount.InventoryPanel, "Prefabs/UIPanel/InventoryPanel" }
        };
    }

    public BasePanel OpenPanel(string name)
    {
        //检测panel是否有打开
        BasePanel panel = null;
        if(PanelDict.TryGetValue(name, out panel))
        {
            Debug.LogError("界面已经打开" + name);
            return null;
        }

        //检测路径是否存在
        string path = "";
        if(!PathDict.TryGetValue(name,out path))
        {
            Debug.LogError("界面名称错误，或者未配置路径：" + name);
            return null;
        }

        //检测预制件是否有存在缓存
        GameObject panelPrefab = null;
        if(!PrefabDict.TryGetValue(name,out panelPrefab))
        {
            string realPath = path;
            panelPrefab = Resources.Load<GameObject>(realPath);
            PrefabDict.Add(name, panelPrefab);
        }

        GameObject panelObject = GameObject.Instantiate(panelPrefab, UIRoot, false);
        panel = panelObject.GetComponent<BasePanel>();
        panel.OpenPanel(name);
        PanelDict.Add(name, panel);
        return panel;
    }

    public bool ClosePanel(string name)
    {
        BasePanel panel = null;
        if(!PanelDict.TryGetValue(name, out panel))
        {
            Debug.LogError("界面未打开："+name);
            return false;
        }

        panel.ClosePanel();
        return true;
    }
}
}
