using System;

namespace LiteFW
{
    /// <summary>UI 面板所属的大层。</summary>
    public enum PanelLayer
    {
        Background = 0,
        Main = 1,
        Popup = 2
    }

    /// <summary>面板层内顺序区间。</summary>
    public static class PanelLayerRules
    {
        public static void GetRange(PanelLayer layer, out int min, out int max)
        {
            min = layer switch
            {
                PanelLayer.Background => 0,
                PanelLayer.Main => 100,
                PanelLayer.Popup => 200,
                _ => throw new ArgumentOutOfRangeException(nameof(layer))
            };
            max = min + 99;
        }

        public static void Validate(PanelLayer layer, int order)
        {
            GetRange(layer, out int min, out int max);

            if (order < min || order > max)
                throw new ArgumentOutOfRangeException(nameof(order), $"层内顺序必须在 {min}–{max} 之间。");
        }

        public static int SortingOrder(PanelLayer layer)
        {
            return (int)layer;
        }
    }

    /// <summary>面板注册配置。</summary>
    [Serializable]
    public sealed class PanelConfig
    {
        public string PanelId;
        public string ResourcePath;
        public PanelLayer Layer;
        public int SortOrder;

        public PanelConfig(string panelId, string resourcePath, PanelLayer layer, int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(panelId))
                throw new ArgumentException("面板名不能为空。", nameof(panelId));
            if (string.IsNullOrWhiteSpace(resourcePath))
                throw new ArgumentException("资源路径不能为空。", nameof(resourcePath));

            PanelLayerRules.Validate(layer, sortOrder);
            PanelId = panelId;
            ResourcePath = resourcePath;
            Layer = layer;
            SortOrder = sortOrder;
        }
    }
}
