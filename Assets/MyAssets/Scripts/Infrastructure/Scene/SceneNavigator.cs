using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteFW
{
    /// <summary>场景导航服务，发布场景加载事件。</summary>
    internal sealed class SceneNavigator : ISceneNavigator, IDisposable
    {
        #region 框架生命周期
        /// <summary>创建导航服务并订阅 Unity 场景加载完成事件。</summary>
        public SceneNavigator()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>注销 Unity 场景回调。</summary>
        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion

        #region 业务入口
        /// <summary>异步加载场景并发布加载开始事件。</summary>
        public AsyncOperation LoadScene(string sceneName, LoadSceneMode mode)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            EventBus.Publish(new SceneLoadingEvent(sceneName, mode, operation));
            return operation;
        }
        #endregion

        #region 框架内部实现
        /// <summary>Unity 场景加载完成后发布事件。</summary>
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EventBus.Publish(new SceneLoadedEvent(scene, mode));
        }
        #endregion
    }
}
