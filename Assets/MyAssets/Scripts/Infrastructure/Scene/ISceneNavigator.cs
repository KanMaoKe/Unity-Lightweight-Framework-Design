using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteFW
{
    /// <summary>场景导航服务。</summary>
    public interface ISceneNavigator
    {
        /// <summary>异步加载场景。</summary>
        AsyncOperation LoadScene(string sceneName, LoadSceneMode mode);
    }
}
