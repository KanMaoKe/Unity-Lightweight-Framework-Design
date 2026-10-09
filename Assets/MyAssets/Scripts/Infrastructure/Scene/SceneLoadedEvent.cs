using UnityEngine.SceneManagement;

namespace LiteFW
{
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
