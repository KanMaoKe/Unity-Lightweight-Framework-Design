#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LiteFW.Editor
{
    /// <summary>生成器参数窗口。</summary>
    public sealed class UIPanelGeneratorWindow : EditorWindow
    {
        /// <summary>面板注册资产路径。</summary>
        internal const string RegistryPath = "Assets/Resources/UI/UIPanelRegistry.asset";

        /// <summary>目标预制体路径。</summary>
        private string m_PrefabPath;
        /// <summary>窗口当前选择的层。</summary>
        private PanelLayer m_Layer = PanelLayer.Main;
        /// <summary>窗口当前选择的层内顺序。</summary>
        private int m_Order = 100;

        /// <summary>打开指定预制体的生成参数窗口。</summary>
        public static void Open(string prefabPath)
        {
            UIPanelGeneratorWindow window = GetWindow<UIPanelGeneratorWindow>("生成 UI 脚本");
            window.m_PrefabPath = prefabPath;
            window.Show();
        }

        /// <summary>绘制参数与冲突提示并触发生成。</summary>
        private void OnGUI()
        {
            if (string.IsNullOrEmpty(m_PrefabPath) || AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath) == null)
            {
                EditorGUILayout.HelpBox("选中的 UI 预制体已不存在，请重新选择。", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("预制体", m_PrefabPath);
            EditorGUILayout.LabelField("面板名", System.IO.Path.GetFileNameWithoutExtension(m_PrefabPath));
            EditorGUILayout.LabelField("资源路径", UIPanelGenerator.GetResourcePath(m_PrefabPath));
            m_Layer = (PanelLayer)EditorGUILayout.EnumPopup("所属层", m_Layer);

            PanelLayerRules.GetRange(m_Layer, out int min, out int max);
            m_Order = EditorGUILayout.IntSlider("层内顺序", m_Order, min, max);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("该层已注册面板", EditorStyles.boldLabel);
            string panelId = System.IO.Path.GetFileNameWithoutExtension(m_PrefabPath);
            UIPanelRegistry registry = AssetDatabase.LoadAssetAtPath<UIPanelRegistry>(RegistryPath);
            if (registry != null)
            {
                foreach (PanelConfig config in registry.Panels)
                {
                    if (config.Layer == m_Layer)
                        EditorGUILayout.LabelField($"{config.PanelId}    {config.SortOrder}");
                }

                foreach (PanelConfig config in registry.Panels)
                {
                    if (config.PanelId != panelId && config.Layer == m_Layer && config.SortOrder == m_Order)
                    {
                        EditorGUILayout.HelpBox($"顺序 {m_Order} 已被 {config.PanelId} 使用，请选择其他顺序。", MessageType.Warning);
                        break;
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("尚未创建注册资产，首次生成时会自动创建。", MessageType.Info);
            }

            EditorGUILayout.Space(12);
            if (GUILayout.Button("生成脚本与注册信息"))
            {
                try
                {
                    UIPanelGenerator.GenerateFiles(m_PrefabPath, m_Layer, m_Order);
                    Close();
                }
                catch (System.Exception exception)
                {
                    EditorUtility.DisplayDialog("生成失败", exception.Message, "确定");
                }
            }
        }
    }
}
#endif
