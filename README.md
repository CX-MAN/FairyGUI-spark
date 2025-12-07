# FairyGUI for 星火编辑器 (Spark Editor)

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)

> 将 [FairyGUI](https://www.fairygui.com/) UI框架适配到星火编辑器（Spark Editor）的完整实现

## 📖 项目简介

本项目是 FairyGUI UI框架在星火编辑器（Spark Editor）平台上的完整适配实现。FairyGUI 是一个专业的 UI 编辑器，支持可视化编辑 UI 界面，并导出为跨平台的 UI 资源。本项目将 FairyGUI 的核心功能移植到星火编辑器的 WasiCore 框架中，让开发者可以在星火编辑器中使用 FairyGUI 创建和管理 UI 界面。

### ✨ 核心特性

- ✅ **完整的 UI 组件支持** - Button、Label、Image、List、ScrollPane、Window 等
- ✅ **屏幕自适应** - 支持设计分辨率适配，自动缩放到不同屏幕尺寸
- ✅ **拖拽功能** - 完整的拖拽（Drag & Drop）支持
- ✅ **事件系统** - 完整的事件分发和处理机制
- ✅ **资源管理** - 支持 FairyGUI 包（Package）和资源加载
- ✅ **控制器系统** - 支持 FairyGUI 的 Controller 状态切换
- ✅ **关系系统** - 支持 UI 元素之间的关联关系
- ✅ **示例代码** - 包含多个完整的示例 Demo

## 🚀 快速开始

### 前置要求

- 星火编辑器（Spark Editor）v2.0+
- .NET 9.0 SDK
- Visual Studio 2022 或 Rider

### 安装步骤

1. **克隆项目**
   ```bash
   git clone https://github.com/your-username/FairyGUI-spark.git
   cd FairyGUI-spark/fgui
   ```

2. **配置指引**
   

3. **准备 UI 资源**
   
   将 FairyGUI 编辑器导出的 `.bytes` 文件放到 `星火编辑器/version-2000/AppBundle/ui` 目录下。

4. **编译项目**
   ```bash
   dotnet build src/GameEntry.csproj -c Client-Debug
   ```

### 基本使用

```csharp
#if CLIENT
using SCEFGUI;
using SCEFGUI.Render;
using SCEFGUI.UI;

// 1. 初始化 FGUI（在游戏启动时调用一次）
var adapter = new SCEAdapter();
FGUIManager.Initialize(adapter, designWidth: 1136, designHeight: 640);

// 2. 将 FGUIRoot 添加到舞台
FGUIRoot.Instance.AddToStage();

// 3. 加载 UI 包
FGUIManager.AddPackage("ui/Basics", (name, ext) => {
    string path = $"res/ui/{name}{ext}";
    return File.Exists(path) ? File.ReadAllBytes(path) : null;
});

// 4. 创建 UI 组件
var mainView = FGUIManager.CreateObject("Basics", "Main") as FGUIComponent;
if (mainView != null)
{
    mainView.SetXY(0, 0);
    FGUIRoot.Instance.AddChild(mainView);
}
#endif
```

## 📁 项目结构

```
fgui/
├── src/
│   └── FGUI/
│       ├── Core/              # 核心类
│       │   ├── FGUIObject.cs  # UI对象基类
│       │   ├── FGUIPackage.cs # 包管理
│       │   └── ...
│       ├── UI/                # UI组件
│       │   ├── FGUIButton.cs
│       │   ├── FGUIComponent.cs
│       │   ├── FGUITextField.cs
│       │   └── ...
│       ├── Render/            # 渲染适配层
│       │   ├── SCEAdapter.cs  # 星火编辑器适配器
│       │   └── SCERenderContext.cs
│       ├── Event/             # 事件系统
│       ├── Samples/           # 示例代码
│       │   ├── Basics/        # 基础示例
│       │   ├── Bag/           # 背包示例
│       │   └── VirtualList/  # 虚拟列表示例
│       └── FGUIManager.cs     # 管理器入口
├── res/
│   └── ui/                    # UI资源文件（.bytes）
└── README.md
```

## 🎯 功能特性详解

### 屏幕适配

支持设计分辨率自动适配，默认设计分辨率为 1136x640：

```csharp
// 设置设计分辨率和适配模式
FGUIManager.SetContentScaleFactor(
    designResolutionX: 1136,
    designResolutionY: 640,
    matchMode: ScreenMatchMode.MatchWidthOrHeight  // 或 MatchWidth / MatchHeight
);
```

### 拖拽功能

支持完整的拖拽操作：

```csharp
// 方式1：直接设置 Draggable
var button = obj.GetChild("dragButton") as FGUIButton;
button.Draggable = true;

// 方式2：使用 DragDropManager（带拖拽代理）
button.OnDragStart.Add(ctx => {
    ctx.PreventDefault();
    DragDropManager.StartDrag(button, ctx.Data, button.Icon);
});
```

### 事件处理

```csharp
// 点击事件
button.OnClick.Add(ctx => {
    Game.Logger.LogInformation("Button clicked!");
});

// 拖拽事件
obj.OnDragStart.Add(ctx => { /* 拖拽开始 */ });
obj.OnDragMove.Add(ctx => { /* 拖拽中 */ });
obj.OnDragEnd.Add(ctx => { /* 拖拽结束 */ });
obj.OnDrop.Add(ctx => { /* 放置 */ });
```

## 📝 示例代码

项目包含多个完整的示例：

- **Basics** - 基础组件演示（Button、Text、Image、List 等）
- **Bag** - 背包系统示例
- **VirtualList** - 虚拟列表示例（大数据量优化）

查看 `src/FGUI/Samples/` 目录获取更多示例代码。

## ⚠️ 已知限制

由于星火编辑器平台的限制，以下 FairyGUI 特性暂不支持：

- ❌ 文字描边（Stroke）
- ❌ 文字阴影（Shadow）
- ❌ 文字下划线（Underline）
- ❌ 行间距/字间距（Leading/LetterSpacing）
- ❌ 部分动画效果

这些限制会在日志中显示警告信息（每种警告只显示一次）。

## 🔧 开发指南

### 添加新的 UI 组件

1. 在 `src/FGUI/UI/` 目录下创建新的组件类
2. 继承 `FGUIObject` 或 `FGUIComponent`
3. 在 `FGUIObjectFactory.cs` 中注册组件类型
4. 在 `SCERenderContext.cs` 中添加渲染逻辑

### 调试技巧

- 查看日志：`logs/lua/wasm-default-*.log`
- 搜索 FGUI 相关日志：`grep "\[FGUI\]" logs/lua/wasm-default-*.log`
- 启用详细日志：在代码中使用 `Game.Logger.LogInformation()`

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

- [FairyGUI](https://www.fairygui.com/) - 优秀的 UI 编辑器框架
- 星火编辑器团队 - 提供强大的游戏开发平台
- 点点大佬提供了适配的基础代码

## 📞 联系方式

如有问题或建议，请通过以下方式联系：

- 提交 [Issue](https://github.com/CX-MAN/FairyGUI-spark/issues)

---

**⭐ 如果这个项目对你有帮助，请给个 Star！**
