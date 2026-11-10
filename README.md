# LiteFW · 个人 Unity 轻量框架

一套可复用的 Unity 6 框架基础代码，不绑定具体玩法。提供事件总线、资源加载、音频、三层 UI、对象池、GameplayTag、场景导航和黑板容器。

## 使用

1. 在 Unity Package Manager 安装 **UniTask**（当前项目使用 2.5.11）。
2. 从 [Releases](../../releases) 下载 `LiteFW.unitypackage`，导入目标 Unity 工程。
3. 在启动场景创建空物体，挂载 `LiteFW.FrameworkBootstrap`。
4. 通过 `FrameworkBootstrap.Instance` 使用 UI、音频、对象池、标签、场景和黑板服务。

## 主要入口

```csharp
var bootstrap = LiteFW.FrameworkBootstrap.Instance;

await bootstrap.UI.OpenPanelAsync("Main");
bootstrap.Pools.GetOrCreate(prefab);
bootstrap.Tags.RequestTag("State.Debuff.Burn");
bootstrap.Scenes.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
```

启动器完成异步初始化后，`FrameworkBootstrap.IsReady` 为 `true`。

## 模块

- **事件总线**：类型化事件、scope 隔离、批量退订。
- **资源加载**：`IAssetProvider` 统一入口，默认 `ResourcesAssetProvider`。
- **音频**：异步播放、跟随、BGM、播放器对象池。
- **UI**：Background / Main / Popup 三层 Canvas、异步加载、实例缓存、生命周期钩子。
- **对象池**：`PoolManager` 统一管理 `PrefabPool<T>`。
- **GameplayTag**：路径注册、层级查询、Tag 容器。
- **场景导航**：异步加载并发布场景事件。
- **黑板容器**：按类型注册和读取业务黑板。

## 依赖与边界

- Unity 6000.5.8f1。
- UniTask 2.5.11。
- 默认使用 Unity Resources；YooAsset 尚未提供具体 Provider。
- 包内不包含业务面板、测试资源、场景、工程设置或第三方依赖。

## 下载

`.unitypackage` 通过 [GitHub Releases](../../releases) 发布，不把二进制包放进 Git 历史。
