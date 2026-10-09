using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteFW
{
    /// <summary>
    /// 场景导航：封装场景加载，并把场景生命周期事件发布到全局事件总线。
    /// </summary>
    public sealed class SceneNavigator : ISceneNavigator, IDisposable
    {
        public SceneNavigator()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// 异步加载场景并发布加载开始事件。
        /// </summary>
        public AsyncOperation LoadScene(string sceneName, LoadSceneMode mode)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            EventBus.Publish(new SceneLoadingEvent(sceneName, mode, operation));
            return operation;
        }

        /// <summary>
        /// 注销 Unity 场景回调。
        /// </summary>
        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EventBus.Publish(new SceneLoadedEvent(scene, mode));
        }
    }
}
