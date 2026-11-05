using UnityEngine;
using UnityEngine.UI;

namespace LiteFW
{
    /// <summary>单个 UI 层的 Canvas 根节点。</summary>
    public sealed class UILayerRoot : MonoBehaviour
    {
        private Canvas m_Canvas;
        private CanvasScaler m_CanvasScaler;

        public PanelLayer Layer { get; private set; }
        public RectTransform Root => (RectTransform)transform;

        public void Initialize(PanelLayer layer)
        {
            Layer = layer;
            name = layer + "Canvas";

            m_Canvas = GetComponent<Canvas>();
            if (m_Canvas == null)
                m_Canvas = gameObject.AddComponent<Canvas>();

            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = PanelLayerRules.SortingOrder(layer);
            m_Canvas.overrideSorting = true;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            if (m_CanvasScaler == null)
                m_CanvasScaler = GetComponent<CanvasScaler>();
            if (m_CanvasScaler == null)
                m_CanvasScaler = gameObject.AddComponent<CanvasScaler>();

            m_CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            m_CanvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            m_CanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            m_CanvasScaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = Root;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
