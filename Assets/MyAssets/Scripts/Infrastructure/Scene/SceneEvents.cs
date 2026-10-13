using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteFW
{
    /// <summary>
    /// 场景开始加载事件。
    /// </summary>
    public readonly struct SceneLoadingEvent : IEventMessage
    {
        public SceneLoadingEvent(string sceneName, LoadSceneMode mode, AsyncOperation operation)
        {
            SceneName = sceneName;
            Mode = mode;
            Operation = operation;
        }

        public string SceneName { get; }
        public LoadSceneMode Mode { get; }
        public AsyncOperation Operation { get; }
    }

    /// <summary>
    /// 场景加载完成事件。
    /// </summary>
    public readonly struct SceneLoadedEvent : IEventMessage
    {
        public SceneLoadedEvent(Scene scene, LoadSceneMode mode)
        {
            Scene = scene;
            Mode = mode;
        }

        public Scene Scene { get; }
        public LoadSceneMode Mode { get; }
    }
}
