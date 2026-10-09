using UnityEngine;
namespace LiteFW
{
    /// <summary>
    /// 黑板管理器：全局单例，持有所有黑板数据。
    /// 其他系统通过 BlackboardManager.Instance.xxx 访问。
    /// </summary>
    public class BlackboardManager : MonoBehaviour
    {
        /// <summary>全局单例。</summary>
        public static BlackboardManager Instance { get; private set; }

        /// <summary>输入黑板。</summary>
        public InputBlackboard Input { get; private set; }

        /// <summary>仲裁黑板。</summary>
        public ArbiterBlackboard Arbiter { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Input = new InputBlackboard();
            Arbiter = new ArbiterBlackboard();
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            Input = null;
            Arbiter = null;
        }
    }
}
