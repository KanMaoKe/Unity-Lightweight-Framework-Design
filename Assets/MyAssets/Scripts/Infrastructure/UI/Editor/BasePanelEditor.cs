#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LiteFW.Editor
{
    /// <summary>面板 Inspector 控件绑定工具。</summary>
    [CustomEditor(typeof(BasePanel), true)]
    public sealed class BasePanelEditor : global::UnityEditor.Editor
    {
        #region 编辑器使用入口
        /// <summary>绘制默认 Inspector 与绑定控件按钮。</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8);

            if (GUILayout.Button("绑定控件"))
                BindControls();
        }
        #endregion

        #region 生成与扫描实现
        /// <summary>将扫描到的控件引用写入面板字段。</summary>
        private void BindControls()
        {
            BasePanel panel = (BasePanel)target;
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(panel.gameObject);
            if (string.IsNullOrEmpty(prefabPath))
                prefabPath = AssetDatabase.GetAssetPath(panel.gameObject);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogWarning("无法确定面板预制体路径，无法自动绑定。");
                return;
            }

            SerializedObject serialized = new SerializedObject(panel);
            int bound = 0;
            foreach (ScannedControl control in UIPanelGenerator.ScanControls(prefabPath))
            {
                SerializedProperty property = serialized.FindProperty(control.FieldName);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                Transform child = panel.transform.Find(control.Name);
                if (child == null)
                {
                    foreach (Transform candidate in panel.GetComponentsInChildren<Transform>(true))
                    {
                        if (candidate.name == control.Name)
                        {
                            child = candidate;
                            break;
                        }
                    }
                }

                if (child == null)
                    continue;

                Component component = child.GetComponent(control.Info.ComponentType);
                if (component == null)
                    continue;

                property.objectReferenceValue = component;
                bound++;
            }

            serialized.ApplyModifiedProperties();
            Debug.Log($"已绑定 {bound} 个控件。");
        }
        #endregion
    }
}
#endif
