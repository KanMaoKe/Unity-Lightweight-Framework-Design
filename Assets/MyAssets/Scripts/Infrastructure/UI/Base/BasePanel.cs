using UnityEngine;

namespace LiteFW
{
    /// <summary>
    /// 面板基类：定义面板的打开与关闭流程。
    /// </summary>
    public class BasePanel : MonoBehaviour
    {
        /// <summary>面板自行关闭时触发，用于同步管理器记录。</summary>
        internal event System.Action<BasePanel> Closed;

        /// <summary>
        /// 面板名称。
        /// </summary>
        protected string panelName;

        /// <summary>注册的面板名。</summary>
        public string PanelId => panelName;

        /// <summary>面板当前状态。</summary>
        public PanelState State { get; private set; } = PanelState.Hidden;

        /// <summary>面板层。</summary>
        public PanelLayer Layer { get; private set; }

        /// <summary>层内显示顺序。</summary>
        public int SortOrder { get; private set; }

        /// <summary>面板是否已初始化。</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>面板是否被 Popup 覆盖并暂停。</summary>
        public bool IsCovered { get; private set; }

        /// <summary>初始化面板配置。</summary>
        internal void Configure(PanelConfig config)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));

            panelName = config.PanelId;
            Layer = config.Layer;
            SortOrder = config.SortOrder;
            gameObject.name = config.PanelId;
        }

        /// <summary>首次创建时初始化。</summary>
        internal void Initialize(object userData = null)
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
            State = PanelState.Hidden;
            OnInit(userData);
        }

        /// <summary>
        /// 打开面板。
        /// </summary>
        internal void Open(object userData = null)
        {
            if (!IsInitialized)
                Initialize(userData);

            gameObject.SetActive(true);
            State = PanelState.Open;
            OnOpen(userData);
        }

        /// <summary>
        /// 关闭面板并保留实例。
        /// </summary>
        internal void Close()
        {
            CloseInternal(false);
        }

        /// <summary>被 Popup 覆盖时通知。</summary>
        internal void Cover()
        {
            if (State != PanelState.Open || IsCovered)
                return;

            IsCovered = true;
            OnCover();
            OnPause();
        }

        /// <summary>Popup 关闭后恢复时通知。</summary>
        internal void Reveal(object userData = null)
        {
            if (State != PanelState.Open || !IsCovered)
                return;

            IsCovered = false;
            OnReveal();
            OnResume();
            OnRefocus(userData);
        }

        /// <summary>驱动面板每帧更新。</summary>
        internal void Tick(float elapseSeconds, float realElapseSeconds)
        {
            if (State == PanelState.Open)
                OnUpdate(elapseSeconds, realElapseSeconds);
        }

        /// <summary>销毁前关闭并通知业务清理。</summary>
        internal void Release()
        {
            if (State == PanelState.Released)
                return;

            CloseInternal(true);
            State = PanelState.Released;
        }

        /// <summary>按状态关闭面板并通知管理器。</summary>
        private void CloseInternal(bool isShutdown)
        {
            if (State == PanelState.Released)
                return;

            bool wasOpen = State == PanelState.Open;
            State = PanelState.Hidden;
            IsCovered = false;
            if (wasOpen || isShutdown)
                OnClose(isShutdown, null);
            gameObject.SetActive(false);
            Closed?.Invoke(this);
        }

        /// <summary>首次创建回调。</summary>
        protected virtual void OnInit(object userData)
        {
        }

        /// <summary>每次打开回调。</summary>
        protected virtual void OnOpen(object userData)
        {
        }

        /// <summary>每次关闭回调。</summary>
        protected virtual void OnClose(bool isShutdown, object userData)
        {
        }

        /// <summary>被 Popup 覆盖时调用。</summary>
        protected virtual void OnCover()
        {
        }

        /// <summary>Popup 关闭后重新显示时调用。</summary>
        protected virtual void OnReveal()
        {
        }

        /// <summary>被 Popup 覆盖时调用，不停止自身更新。</summary>
        protected virtual void OnPause()
        {
        }

        /// <summary>Popup 关闭后恢复时调用。</summary>
        protected virtual void OnResume()
        {
        }

        /// <summary>重新获得焦点时调用。</summary>
        protected virtual void OnRefocus(object userData)
        {
        }

        /// <summary>面板打开时每帧调用。</summary>
        protected virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>销毁前清理。</summary>
        private void OnDestroy()
        {
            State = PanelState.Released;
        }

        /// <summary>面板状态。</summary>
        public enum PanelState
        {
            Open,
            Hidden,
            Released
        }
    }
}
