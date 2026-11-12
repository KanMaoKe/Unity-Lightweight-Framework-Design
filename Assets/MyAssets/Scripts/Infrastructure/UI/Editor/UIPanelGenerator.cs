#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LiteFW.Editor
{
    /// <summary>根据选中的 UI 预制体生成面板脚本和注册信息。</summary>
    public static class UIPanelGenerator
    {
        #region 编辑器使用入口
        /// <summary>校验选中对象并打开生成窗口。</summary>
        [MenuItem("生成我们的自定义脚本/UI/生成 UI 脚本")]
        private static void Generate()
        {
            UnityEngine.Object selected = Selection.activeObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("生成 UI 脚本", "请先选中一个 UI 预制体。", "确定");
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("生成 UI 脚本", "选中的对象不是 UI 预制体。", "确定");
                return;
            }

            UIPanelGeneratorWindow.Open(prefabPath);
        }
        #endregion

        #region 生成与扫描实现
        /// <summary>Core 脚本输出目录。</summary>
        private const string CoreOutputDir = "Assets/MyAssets/Scripts/UI/Core/";
        /// <summary>Logic 脚本输出目录。</summary>
        private const string LogicOutputDir = "Assets/MyAssets/Scripts/UI/Logic/";
        /// <summary>生成脚本使用的命名空间。</summary>
        private const string Namespace = "LiteFW";

        /// <summary>生成面板脚本并写入注册信息。</summary>
        internal static void GenerateFiles(string prefabPath, PanelLayer layer, int order)
        {
            PanelLayerRules.Validate(layer, order);
            string className = Path.GetFileNameWithoutExtension(prefabPath);
            if (!IsIdentifier(className))
                throw new InvalidOperationException("预制体名称必须是有效的 C# 类名。");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("无法读取 UI 预制体。");

            BasePanel attached = prefab.GetComponent<BasePanel>();
            if (attached != null && attached.GetType().Name != className)
                throw new InvalidOperationException($"预制体根节点已挂载 {attached.GetType().Name}，不能生成 {className}。");

            List<ScannedControl> controls = ScanControls(prefabPath);
            string resourcePath = GetResourcePath(prefabPath);
            ValidateRegistration(className, resourcePath, layer, order);
            EnsureUniqueFieldNames(controls);
            RegisterPanel(className, resourcePath, layer, order);
            GenerateCore(className, controls, layer, order, resourcePath);
            GenerateLogic(className);
            AssetDatabase.Refresh();
        }

        /// <summary>把面板配置写入注册资产。</summary>
        private static void RegisterPanel(string className, string resourcePath, PanelLayer layer, int order)
        {
            const string registryPath = UIPanelGeneratorWindow.RegistryPath;
            UIPanelRegistry registry = AssetDatabase.LoadAssetAtPath<UIPanelRegistry>(registryPath);
            if (registry == null)
            {
                string directory = Path.GetDirectoryName(registryPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                registry = ScriptableObject.CreateInstance<UIPanelRegistry>();
                AssetDatabase.CreateAsset(registry, registryPath);
            }

            registry.Register(new PanelConfig(className, resourcePath, layer, order));
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            if (FrameworkBootstrap.Instance != null && FrameworkBootstrap.Instance.UI != null)
                FrameworkBootstrap.Instance.UI.RegisterPanel(new PanelConfig(className, resourcePath, layer, order));
        }

        /// <summary>检查资源路径与层内顺序冲突。</summary>
        private static void ValidateRegistration(string className, string resourcePath, PanelLayer layer, int order)
        {
            if (!resourcePath.StartsWith("UI/", StringComparison.Ordinal))
                throw new InvalidOperationException("请把 UI 预制体放在 Assets/Resources/UI/ 下，再生成脚本。");

            UIPanelRegistry registry = AssetDatabase.LoadAssetAtPath<UIPanelRegistry>(UIPanelGeneratorWindow.RegistryPath);
            if (registry == null)
                return;

            foreach (PanelConfig config in registry.Panels)
            {
                if (config.PanelId == className)
                {
                    if (config.ResourcePath != resourcePath)
                        throw new InvalidOperationException($"面板 {className} 已注册到其他资源路径：{config.ResourcePath}");
                    continue;
                }
                if (config.Layer == layer && config.SortOrder == order)
                    throw new InvalidOperationException($"顺序 {order} 已被 {config.PanelId} 使用，请选择其他顺序。");
            }
        }

        /// <summary>检查控件是否会生成重名字段。</summary>
        private static void EnsureUniqueFieldNames(List<ScannedControl> controls)
        {
            var names = new HashSet<string>();
            foreach (ScannedControl control in controls)
            {
                if (!names.Add(control.FieldName))
                    throw new InvalidOperationException($"控件名称会生成重复字段：{control.FieldName}，请先修改重复名称。");
            }
        }

        /// <summary>判断字符串能否作为 C# 标识符。</summary>
        private static bool IsIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || (!char.IsLetter(value[0]) && value[0] != '_'))
                return false;

            for (int i = 1; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
                    return false;
            }

            return true;
        }

        /// <summary>扫描预制体中符合命名约定的控件。</summary>
        internal static List<ScannedControl> ScanControls(string prefabPath)
        {
            var results = new List<ScannedControl>();
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return results;

            try
            {
                ScanNode(root, results);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return results;
        }

        /// <summary>将预制体路径转换为 Resources 下的资源 key。</summary>
        internal static string GetResourcePath(string prefabPath)
        {
            const string marker = "/Resources/";
            int index = prefabPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return prefabPath;

            string path = prefabPath.Substring(index + marker.Length);
            return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                ? path.Substring(0, path.Length - ".prefab".Length)
                : path;
        }

        /// <summary>递归收集符合命名约定的控件。</summary>
        private static void ScanNode(GameObject go, List<ScannedControl> results)
        {
            string name = go.name;
            string normalized = name.Replace(" (Legacy)", string.Empty).Replace("(Legacy)", string.Empty);
            if (!UIControlConfig.DefaultUnityNames.Contains(normalized))
            {
                foreach (KeyValuePair<string, UIComponentInfo> pair in UIControlConfig.ControlMap)
                {
                    if (!name.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase) || name.Length == pair.Key.Length)
                        continue;

                    if (go.GetComponent(pair.Value.ComponentType) != null)
                    {
                        results.Add(new ScannedControl
                        {
                            Name = name,
                            FieldName = "m_" + SanitizeIdentifier(name),
                            Info = pair.Value
                        });
                    }
                    break;
                }
            }

            foreach (Transform child in go.transform)
                ScanNode(child.gameObject, results);
        }

        /// <summary>生成控件字段声明。</summary>
        private static void GenerateCore(string className, List<ScannedControl> controls, PanelLayer layer, int order, string resourcePath)
        {
            EnsureDirectory(CoreOutputDir);
            var sb = new StringBuilder();
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine($"// {className}.Core.cs —— 工具自动生成，请勿手动修改");
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine();
            sb.AppendLine("namespace LiteFW");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {className} 面板自动生成的部分。");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public partial class {className} : BasePanel");
            sb.AppendLine("    {");
            sb.AppendLine("        #region 框架内部实现");
            sb.AppendLine();
            sb.AppendLine("        // ===== UI 控件变量（工具自动扫描预制体生成）=====");
            if (controls.Count == 0)
                sb.AppendLine("        // (未检测到符合命名约定的控件)");
            foreach (ScannedControl control in controls)
                sb.AppendLine($"        [SerializeField] private {control.Info.TypeName} {control.FieldName};");
            sb.AppendLine();
            sb.AppendLine("        #endregion");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(CoreOutputDir, className + ".Core.cs"), sb.ToString(), Encoding.UTF8);
        }

        /// <summary>生成界面逻辑骨架，已存在则跳过。</summary>
        private static void GenerateLogic(string className)
        {
            EnsureDirectory(LogicOutputDir);
            string filePath = Path.Combine(LogicOutputDir, className + ".cs");
            if (File.Exists(filePath))
                return;

            var sb = new StringBuilder();
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine($"// {className}.cs —— 在此处编写界面逻辑");
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace LiteFW");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {className} 界面逻辑");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public partial class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        #region 业务入口");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 界面初始化（仅首次创建时调用一次）");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        protected override void OnInit(object userData)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnInit(userData);");
            sb.AppendLine();
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 界面打开时调用 —— 在此处注册按钮/事件");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        protected override void OnOpen(object userData)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnOpen(userData);");
            sb.AppendLine();
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 界面关闭时调用 —— 在此处反注册按钮/事件");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        protected override void OnClose(bool isShutdown, object userData)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnClose(isShutdown, userData);");
            sb.AppendLine();
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        #endregion");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>把任意字符串转为合法标识符。</summary>
        private static string SanitizeIdentifier(string value)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i == 0 && !char.IsLetter(c) && c != '_')
                    sb.Append('_');
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            return sb.Length == 0 ? "_" : sb.ToString();
        }

        /// <summary>确保输出目录存在。</summary>
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        #endregion
    }

    /// <summary>扫描到的一个控件及其生成信息。</summary>
    public sealed class ScannedControl
    {
        /// <summary>控件节点名。</summary>
        public string Name;
        /// <summary>生成的字段名。</summary>
        public string FieldName;
        /// <summary>控件组件信息。</summary>
        public UIComponentInfo Info;
    }
}
#endif
