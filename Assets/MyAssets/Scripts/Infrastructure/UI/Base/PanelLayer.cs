using System;

namespace LiteFW
{
    /// <summary>UI 面板所属的大层。</summary>
    public enum PanelLayer
    {
        /// <summary>背景层。</summary>
        Background = 0,
        /// <summary>主界面层。</summary>
        Main = 1,
        /// <summary>弹窗层。</summary>
        Popup = 2
    }

    /// <summary>面板层内顺序区间。</summary>
    public static class PanelLayerRules
    {
        /// <summary>获取指定层允许的层内顺序区间。</summary>
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

        /// <summary>校验层内顺序是否合法，非法时抛出异常。</summary>
        public static void Validate(PanelLayer layer, int order)
        {
            GetRange(layer, out int min, out int max);

            if (order < min || order > max)
                throw new ArgumentOutOfRangeException(nameof(order), $"层内顺序必须在 {min}–{max} 之间。");
        }

        /// <summary>将大层映射为 Canvas 的 SortingOrder。</summary>
        public static int SortingOrder(PanelLayer layer)
        {
            return (int)layer;
        }
    }

    /// <summary>面板注册配置。</summary>
    [Serializable]
    public sealed class PanelConfig
    {
        /// <summary>面板唯一标识。</summary>
        public string PanelId;
        /// <summary>资源加载路径。</summary>
        public string ResourcePath;
        /// <summary>所属大层。</summary>
        public PanelLayer Layer;
        /// <summary>层内显示顺序。</summary>
        public int SortOrder;

        /// <summary>创建面板配置，层内顺序越界时抛出异常。</summary>
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
