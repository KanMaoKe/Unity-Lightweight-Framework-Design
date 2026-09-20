using UnityEngine;

namespace Duel
{
/// <summary>
/// 黑板管理器：全局单例，持有所有黑板数据。
/// 其他系统通过 BlackboardManager.Instance.xxx 访问。
/// </summary>
public class BlackboardManager : MonoBehaviour
{
    public static BlackboardManager Instance { get; private set; }

    public InputBlackboard Input { get; private set; }
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

}
}
