using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteFW
{
    /// <summary>面板注册资产。</summary>
    [CreateAssetMenu(fileName = "UIPanelRegistry", menuName = "LiteFW/UI/Panel Registry")]
    public sealed class UIPanelRegistry : ScriptableObject
    {
        /// <summary>序列化保存的面板配置。</summary>
        [SerializeField] private List<PanelConfig> m_Panels = new List<PanelConfig>();

        /// <summary>全部已注册面板配置。</summary>
        public IReadOnlyList<PanelConfig> Panels => m_Panels;

        /// <summary>注册或覆盖同名的面板配置。</summary>
        public void Register(PanelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            for (int i = 0; i < m_Panels.Count; i++)
            {
                if (m_Panels[i].PanelId == config.PanelId)
                {
                    m_Panels[i] = config;
                    return;
                }
            }

            m_Panels.Add(config);
        }

        /// <summary>按面板名查找配置。</summary>
        public bool TryGet(string panelId, out PanelConfig config)
        {
            config = m_Panels.Find(panel => panel.PanelId == panelId);
            return config != null;
        }

        /// <summary>移除指定面板名的配置。</summary>
        public void Remove(string panelId)
        {
            m_Panels.RemoveAll(panel => panel.PanelId == panelId);
        }
    }
}
