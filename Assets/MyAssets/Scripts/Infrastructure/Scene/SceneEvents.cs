using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteFW
{
    /// <summary>场景开始加载事件。</summary>
    public readonly struct SceneLoadingEvent : IEventMessage
    {
        /// <summary>创建场景开始加载事件。</summary>
        public SceneLoadingEvent(string sceneName, LoadSceneMode mode, AsyncOperation operation)
        {
            SceneName = sceneName;
            Mode = mode;
            Operation = operation;
        }

        /// <summary>场景名。</summary>
        public string SceneName { get; }
        /// <summary>场景加载模式。</summary>
        public LoadSceneMode Mode { get; }
        /// <summary>Unity 异步加载操作。</summary>
        public AsyncOperation Operation { get; }
    }

    /// <summary>场景加载完成事件。</summary>
    public readonly struct SceneLoadedEvent : IEventMessage
    {
        /// <summary>创建场景加载完成事件。</summary>
        public SceneLoadedEvent(Scene scene, LoadSceneMode mode)
        {
            Scene = scene;
            Mode = mode;
        }

        /// <summary>已加载场景。</summary>
        public Scene Scene { get; }
        /// <summary>场景加载模式。</summary>
        public LoadSceneMode Mode { get; }
    }
}
