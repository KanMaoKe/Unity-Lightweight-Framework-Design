using UnityEngine;

namespace Duel
{
    /// <summary>
    /// 全局固定音效配置（SO）：玩家受伤、UI、波次等不随对象变化的音效都放这里。
    /// 变化音效（武器 / 怪物）各挂在对应资产上，不在这里。
    /// </summary>
    [CreateAssetMenu(fileName = "SFXConfig", menuName = "Duel/SFXConfig")]
    public class SFXConfig : ScriptableObject
    {
        private static SFXConfig _instance;

        public static SFXConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<SFXConfig>("SFXConfig");
                return _instance;
            }
        }

        [Header("玩家")]
        [Tooltip("多个受击音随机挑一个")]
        public AudioClip[] playerHurtSounds;

        [Header("拾取")]
        [Tooltip("拾取物品时播放")]
        public AudioClip pickupSound;

        // 以后：UI 点击、波次开始/结束、环境音都往这里加，不散落在组件里
    }
}
