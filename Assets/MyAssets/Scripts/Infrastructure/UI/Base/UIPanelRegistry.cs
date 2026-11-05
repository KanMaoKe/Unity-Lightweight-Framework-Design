using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteFW
{
    /// <summary>面板注册资产。</summary>
    [CreateAssetMenu(fileName = "UIPanelRegistry", menuName = "LiteFW/UI/Panel Registry")]
    public sealed class UIPanelRegistry : ScriptableObject
    {
        [SerializeField] private List<PanelConfig> m_Panels = new List<PanelConfig>();

        public IReadOnlyList<PanelConfig> Panels => m_Panels;

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

        public bool TryGet(string panelId, out PanelConfig config)
        {
            config = m_Panels.Find(panel => panel.PanelId == panelId);
            return config != null;
        }

        public void Remove(string panelId)
        {
            m_Panels.RemoveAll(panel => panel.PanelId == panelId);
        }
    }
}
