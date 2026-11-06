#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace LiteFW.Editor
{
    /// <summary>UI 控件命名约定。</summary>
    public static class UIControlConfig
    {
        /// <summary>控件前缀到组件信息的映射。</summary>
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

        /// <summary>Unity 默认控件名称，不生成字段。</summary>
        public static readonly HashSet<string> DefaultUnityNames = new HashSet<string>
        {
            "Button", "Text", "Image", "Raw Image", "RawImage", "Toggle", "Slider",
            "InputField", "Input Field", "Dropdown", "Scroll View", "Panel", "Canvas"
        };
    }

    /// <summary>控件前缀对应的组件信息。</summary>
    public sealed class UIComponentInfo
    {
        /// <summary>控件组件类型。</summary>
        public readonly Type ComponentType;
        /// <summary>生成字段时使用的类型名。</summary>
        public readonly string TypeName;

        /// <summary>创建组件信息。</summary>
        public UIComponentInfo(Type componentType, string typeName)
        {
            ComponentType = componentType;
            TypeName = typeName;
        }
    }
}
#endif
