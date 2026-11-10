#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiteFW.Editor
{
    /// <summary>删除 UI 预制体时同步移除注册配置，不自动删除业务脚本。</summary>
    internal sealed class UIPanelAssetWatcher : AssetModificationProcessor
    {
        /// <summary>资源删除前清理对应面板注册。</summary>
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (!assetPath.StartsWith("Assets/Resources/UI/", System.StringComparison.Ordinal) ||
                !assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                return AssetDeleteResult.DidNotDelete;
            }

            UIPanelRegistry registry = AssetDatabase.LoadAssetAtPath<UIPanelRegistry>(UIPanelGeneratorWindow.RegistryPath);
            if (registry == null)
                return AssetDeleteResult.DidNotDelete;

            string panelId = Path.GetFileNameWithoutExtension(assetPath);
            if (!registry.TryGet(panelId, out PanelConfig config))
                return AssetDeleteResult.DidNotDelete;

            if (config.ResourcePath != UIPanelGenerator.GetResourcePath(assetPath))
                return AssetDeleteResult.DidNotDelete;

            registry.Remove(panelId);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"已移除面板注册：{panelId}。请确认是否需要保留脚本：" +
                $"Assets/MyAssets/Scripts/UI/Core/{panelId}.Core.cs，" +
                $"Assets/MyAssets/Scripts/UI/Logic/{panelId}.cs");

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
#endif
