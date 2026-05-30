# UnityGameObjectLayoutGroup

自定义布局组件，支持 **SpriteRenderer** 和 **RectTransform (UI)** 两种模式，具备网格布局、扇形布局、Tween 动画入场等能力。

---

## 功能

| 功能 | 说明 |
|------|------|
| 网格布局 | 水平/垂直排列子物体，支持 Padding/Spacing/CellSize |
| 扇形布局 | 以扇形排列子物体，可调角度/半径/圆心对齐 |
| Sprite 模式 | 基于 SpriteRenderer 的 2D 布局 |
| UI 模式 | 基于 RectTransform 的 UI 布局 |
| Tween 入场动画 | 子物体逐一飞入，支持弧形路径 + 旋转动画 |
| 动画控制 | `StopAnim()` 可随时中断 |
| Scene 视图手柄 | 扇形半径/角度/StartPoint 直接拖拽 |

---

## 快速开始

1. 创建一个空 GameObject
2. 挂载 `GameObjectLayoutGroup` 组件
3. 添加子物体（SpriteRenderer 或 UI 控件）
4. 在 Inspector 中调节参数

---

## Inspector 面板

| 区域 | 说明 |
|------|------|
| **Basic Settings** | Padding、CellSize、Axis、对齐方式、IncludeInactive、OverrideAnchor |
| **Layout Settings** | 折叠面板 — StartCorner、CellRotation、Spacing、ControlSize/Rotation |
| **Sector Settings** | 折叠面板 — EnableSector、Angle、Radius |
| **Tween Animation** | 折叠面板 — 入场动画参数 + Play 按钮 |

---

## 参数详解

### 基础参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Padding | — | 内容区域边距 |
| Cell Size | (100, 100) | 每个子项的尺寸 |
| Spacing | (10, 10) | 子项间距 |
| Axis | Horizontal | 排列方向 |
| Start Corner | UpperLeft | 起始角落 |
| Child Alignment | — | 整体对齐方式 |
| Auto Update | true | 子物体变化时自动刷新布局 |
| Include Inactive | false | 是否包含未激活的子物体 |
| Override Child Anchor | true | 是否重置子物体锚点为 (0,1) |

### 扇形参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Enable Sector | false | 启用扇形布局 |
| Angle | 0 | 扇形张开角度 |
| Radius | 0 | 扇形半径 |

### Tween 动画参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Ease | OutQuad | 位置动画曲线 |
| Start Point | (0,0) | 动画起始位置（Scene 可拖拽） |
| Duration | 0.3s | 单个动画时长 |
| Delay Between | 0.2s | 子项间的延迟 |
| Start Scale | (0.1, 0.1) | 起始缩放 |
| **Start Angle** | **-10°** | 起始 Z 旋转（扇形模式：物体倾斜飞出） |
| **Rotate Ease** | **OutBack** | 旋转曲线（轻微回弹） |
| **Path Arc Height** | **30** | 路径弧度高度（抛物线轨迹） |

---

## Scene 视图手柄

| 手柄 | 颜色 | 作用 |
|------|------|------|
| 扇形弧线 + 半径滑块 | 青色 | 拖拽调整扇形半径 |
| StartPoint | 黄色 | 拖拽调整 Tween 起始位置 |

---

## 代码示例

```csharp
// 手动触发布局（带动画）
layoutGroup.UpdateLayout(true);

// 手动触发布局（无动画）
layoutGroup.UpdateLayout(false);

// 停止所有播放中的动画
layoutGroup.StopAnim();

// 检查动画是否正在播放
bool isPlaying = layoutGroup.isPlayingTweenAnim;
```

---

## 文件结构

```
Runtime/
├── GameObjectLayoutGroup.cs          # 核心 + 生命周期 + 对齐辅助
├── GameObjectLayoutGroup.Tween.cs    # TweenAnim 类 + 入场动画
├── GameObjectLayoutGroup.Sprite.cs   # Sprite 布局 + 扇形布局
└── GameObjectLayoutGroup.Rect.cs     # Rect 布局 + 扇形布局
Editor/
└── GameObjectLayoutGroupEditor.cs    # Inspector + Scene 手柄
```
