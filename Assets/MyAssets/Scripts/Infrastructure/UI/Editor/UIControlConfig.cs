#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace LiteFW.Editor
{
    /// <summary>UI 控件命名约定。</summary>
    public static class UIControlConfig
    {
        public static readonly Dictionary<string, UIComponentInfo> ControlMap = new Dictionary<string, UIComponentInfo>
        {
            { "Btn", new UIComponentInfo(typeof(Button), "Button") },
            { "Text", new UIComponentInfo(typeof(Text), "Text") },
            { "Img", new UIComponentInfo(typeof(Image), "Image") },
            { "Toggle", new UIComponentInfo(typeof(Toggle), "Toggle") },
            { "Slider", new UIComponentInfo(typeof(Slider), "Slider") },
            { "Input", new UIComponentInfo(typeof(InputField), "InputField") },
            { "Dropdown", new UIComponentInfo(typeof(Dropdown), "Dropdown") },
            { "Scroll", new UIComponentInfo(typeof(ScrollRect), "ScrollRect") }
        };

        public static readonly HashSet<string> DefaultUnityNames = new HashSet<string>
        {
            "Button", "Text", "Image", "Raw Image", "RawImage", "Toggle", "Slider",
            "InputField", "Input Field", "Dropdown", "Scroll View", "Panel", "Canvas"
        };
    }

    public sealed class UIComponentInfo
    {
        public readonly Type ComponentType;
        public readonly string TypeName;

        public UIComponentInfo(Type componentType, string typeName)
        {
            ComponentType = componentType;
            TypeName = typeName;
        }
    }
}
#endif
