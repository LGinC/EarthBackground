# EarthBackground
![.NET Core Desktop](https://github.com/LGinC/EarthBackground/workflows/.NET%20Desktop%20Build/badge.svg)

基于 `.NET 10` 和 `Avalonia` 的地球壁纸工具，支持静态壁纸和 Windows / macOS / Linux X11 动态壁纸。

项目当前重点是：
- 抓取卫星云图分块并拼接为完整图片
- 复用已有 `frame_xxx.png`，避免重复下载和重复拼接
- 在 Windows / macOS / Linux X11 上将 PNG 帧序列直接流式播放为动态壁纸
- 提供本地配置、下载进度、错误通知和多语言 UI

## 当前功能

### 抓取源
- `Himawari` 向日葵 9 号
- `GOES` GOES-19
- `GeoKompsat` Geo-KOMPSAT-2A
- `Meteosat` Meteosat-12
- `fy-4` 风云 4B

### 下载方式
- 直接下载
- Cloudinary
- 七牛云

### 壁纸模式
- 静态壁纸：抓取最新一帧并设置为系统壁纸
- 动态壁纸：抓取最近一段时间的多帧 PNG，循环播放为动态桌面

### 平台支持

| 平台 | 动态壁纸 | 实现方式 | 说明 |
| --- | --- | --- | --- |
| Windows | 支持 | `WorkerW` + Avalonia 播放窗口 | 窗口嵌入桌面图标层下方 |
| macOS | 支持 | `NSWindow` desktop window level | 显示在壁纸之上、桌面图标之下；全屏 Space 遮挡是系统限制 |
| Linux X11 | 支持 | xwinwrap-like X11 root window 模式 | 设置 EWMH desktop/below/sticky 状态并 reparent 到 root |
| Linux Wayland | 暂不支持 | - | Avalonia native handle 不是 X11 时会明确报不支持 |

### 运行特性
- 支持多显示器动态壁纸播放
- 支持按帧缓存复用，已有 `frame_xxx.png` 时不再重复处理
- 支持受控并发下载和拼接，加快多帧生成速度
- 支持当帧集合未变化时跳过动态壁纸重建
- 支持主界面进度条显示下载、解析和播放准备进度
- 支持配置保存与开机自启动

## 动态壁纸实现

当前动态壁纸链路已经不再走“先生成 APNG，再解析 APNG 播放”的旧方案，而是改为直接播放 PNG 序列：

1. `WallpaperService` 周期性触发抓取
2. `Captor` 获取最近时间戳列表
3. 对缺失帧执行下载和拼接，已有帧直接复用
4. 平台动态壁纸设置器对帧路径排序
5. `WallpaperPlaybackWindow` 在后台调用 `PngSequencePlayer`，预解码下一帧
6. 通过两个复用位图和绘制层交叉淡化显示，不生成或保存过渡图片

这样做的好处：
- 避免 APNG 编码和再次解析带来的额外耗时
- 降低播放前的峰值内存占用
- 更适合“抓一批帧然后循环播放”的桌面壁纸场景

播放规则：
- 每个显示器窗口保留当前帧和下一帧两个位图缓冲，不预解码整段序列；单帧只显示，不启动动画计时器。
- 过渡占帧间隔的后 40%，最长 200 ms。例如 `FrameIntervalMs = 500` 时，保持 300 ms、淡化 200 ms；只在过渡期间按约 60 Hz 更新透明度。
- 淡化层的黑底和图像按整体透明度合成；父画布启用 `RequiresFullOpacityHandling`，避免实时合成器重复压暗。相同帧之间的过渡应保持亮度不变。
- 下一帧尚未解码完成时保持当前画面，不阻塞 UI；慢帧就绪后再开始完整过渡。
- 末帧保持 `LoopPauseMilliseconds` 指定的停顿，然后直接回到首帧，不把不连续的首尾云图混合。
- 窗口暂停时冻结过渡进度；关闭时等待正在解码的帧结束后释放位图。
- 位图缓冲之外仍有解码临时内存和渲染器纹理；交叉淡化不等于减少播放内存。

### 平台窗口策略

- Windows：旧桌面结构直接嵌入 `WorkerW`。Windows 11 24H2 的 raised desktop 使用 `0x052C / 0xD / 1` 激活透明图标层，并在 `Progman` 下创建分层宿主，放在 `SHELLDLL_DefView` 图标层之下、系统 `WorkerW` 静态壁纸层之上。
  - Avalonia 窗口类带 `CS_OWNDC`，不能直接添加 `WS_EX_LAYERED`，因此使用独立的系统 `STATIC` 宿主承载 Avalonia 子窗口，保留原渲染后端。
  - 按真实父窗口客户区转换显示器坐标，支持负坐标和非零桌面原点；检查原生嵌入调用结果，并在关闭时销毁宿主。
- macOS：通过 native `NSWindow` 设置 `level = kCGDesktopWindowLevel - 1`，并启用 `canJoinAllSpaces | stationary | ignoresCycle`；窗口无边框、无阴影、鼠标穿透。
- Linux X11：要求 `TopLevel.TryGetPlatformHandle()` 返回 `HandleDescriptor == "X11"` 或 `"XID"`，然后用 `libX11` 设置 `_NET_WM_WINDOW_TYPE_DESKTOP`、`_NET_WM_STATE_BELOW/SKIP_TASKBAR/SKIP_PAGER/STICKY`；若存在 `xfdesktop` 等已有桌面窗口，则将播放窗口堆叠到其上方，否则退回 root window 并 lower。

## 架构流程图

```mermaid
flowchart TD
    A["WallpaperService 周期调度"] --> B["Captor 获取时间戳列表"]
    B --> C{"是否已有 frame_xxx.png"}
    C -->|是| D["直接复用现有帧"]
    C -->|否| E["下载分块图片"]
    E --> F["拼接为 frame_xxx.png"]
    D --> G["形成按时间排序的帧序列"]
    F --> G
    G --> H{"DynamicWallpaper?"}
    H -->|否| I["IBackgroundSetter 设置静态壁纸"]
    H -->|是| J["IDynamicWallpaperSetter"]
    J --> K{"运行平台"}
    K -->|Windows| L["WorkerW 桌面子窗口"]
    K -->|macOS| M["NSWindow desktop level"]
    K -->|Linux X11| N["X11 root window / desktop atoms"]
    L --> O["PngSequencePlayer 后台解码下一帧"]
    M --> O
    N --> O
    O --> P["WallpaperPlaybackWindow 双缓冲交叉淡化"]
    P --> Q["多显示器动态壁纸播放"]
```

## 动态壁纸流程

```mermaid
sequenceDiagram
    participant S as WallpaperService
    participant C as Captor
    participant P as PngSequencePlayer
    participant D as Platform Setter
    participant W as WallpaperPlaybackWindow

    S->>C: 请求最近 RecentHours 的帧
    C->>C: 复用已有 frame_xxx.png
    C->>C: 下载并拼接缺失帧
    C-->>S: 返回 PNG 帧序列
    S->>D: SetDynamicBackgroundAsync(paths)
    D->>P: 打开按需 PNG 播放器
    D->>W: 为目标显示器创建播放窗口
    W->>W: 配置平台桌面层级
    loop 播放循环
        W->>P: 后台预解码下一帧
        P-->>W: 写入备用 WriteableBitmap
        W->>W: 就绪后交叉淡化并交换位图
    end
```

## 项目结构

`src` 下的主要模块：

- `Background`
  - 壁纸服务循环
  - Windows / macOS / Linux 动态壁纸设置
  - 平台显示器枚举和多显示器区域管理
- `Captors`
  - 各卫星抓取器
  - 分块下载、缓存命中、图片拼接
  - 多帧并发处理
- `Imaging`
  - PNG 序列播放器
  - 动态壁纸帧播放器接口
  - 早期 APNG 相关实现
- `Oss`
  - 直接下载
  - Cloudinary
  - 七牛云
- `Views`
  - Avalonia 窗口
  - 动态壁纸播放窗口
- `Platforms`
  - macOS `NSWindow` 原生配置
  - Linux X11 原生窗口属性配置
- `ViewModels`
  - 主界面逻辑
  - 设置界面逻辑
- `Localization`
  - 基于 `.resx` 的 UI 文本本地化

## 工作流程

### 静态壁纸
- 获取最新时间戳
- 下载缺失分块
- 拼接为完整 PNG
- 设置为系统壁纸

### 动态壁纸
- 获取最近 `RecentHours` 的时间戳列表
- 对每个时间戳检查是否已有 `frame_xxx.png`
- 缺失帧才下载并拼接
- 最终按时间顺序播放 PNG 帧序列，后台预解码下一帧并进行短时间交叉淡化
- Windows 按显示器创建独立窗口并嵌入 `WorkerW`
- macOS / Linux 同样按显示器创建独立窗口，避免跨屏窗口带来的 Mission Control / X11 桌面行为问题

### 发布

- 发布流程、渠道说明、维护者手册、安装示例已移至 [`Release.md`](./Release.md)。
- 日常开发和功能说明仍保留在本文件中。

macOS 个人 Tap 的预备安装命令（仅在发行包通过验证后启用）：

```bash
brew tap lginc/earthbackground https://github.com/LGinC/EarthBackground.git
brew install --cask lginc/earthbackground/earthbackground
```

本次 macOS 打包与数据路径修复已通过 ARM64 / Intel Native AOT、真实 GUI、配置保留和运行后签名验证。按选择暂不发版，旧 v2.6.0 包仍有原签名缺陷；个人 Tap 待新版本启用，未公证应用需系统手动授权。详情见 [`Release.md`](./Release.md#homebrew)。

## 配置说明

主要配置位于 `appsettings.json`：

- `CaptureOptions`
  - 抓取器
  - 分辨率
  - 更新间隔
  - 帧间隔
  - 缩放比例
  - 是否动态壁纸
  - 最近时长
  - 循环停顿
  - 保存路径
- `OssOptions`
  - 下载方式
  - 用户名
  - API Key / Secret
  - Domain / Bucket / Zone

历史配置说明仍可参考：

- [配置详解](https://github.com/LGinC/EarthBackground/wiki)

### 示例配置

```json
{
  "CaptureOptions": {
    "Captor": "fy-4",
    "AutoStart": false,
    "SetWallpaper": true,
    "SaveWallpaper": false,
    "WallpaperFolder": "images",
    "SavePath": "images",
    "Resolution": 2,
    "Zoom": 80,
    "Interval": 20,
    "FrameIntervalMinutes": 10,
    "DynamicWallpaper": true,
    "FrameIntervalMs": 500,
    "RecentHours": 24,
    "LoopPauseMilliseconds": 3000
  },
  "OssOptions": {
    "CloudName": "DirectDownload",
    "UserName": "",
    "ApiKey": "",
    "ApiSecret": "",
    "Zone": "",
    "Bucket": "",
    "Domain": "",
    "IsEnable": true
  }
}
```

字段说明补充：
- `Captor`
  - `Himawari`
  - `GOES`
  - `GeoKompsat`
  - `Meteosat`
  - `fy-4`
- `Resolution`
  - `0` = `688 x 688`
  - `1` = `1376 x 1376`
  - `2` = `2752 x 2752`
  - `3` = `5504 x 5504`
  - `4` = `11008 x 11008`
- `CloudName`
  - `DirectDownload`
  - `Cloudinary`
  - `Qiniuyun`
- `DynamicWallpaper`
  - `true` 时使用最近一段时间的 PNG 帧序列播放动态壁纸
- `Interval`
  - 更新间隔，单位分钟
- `FrameIntervalMinutes`
  - 抓取帧间隔，单位分钟，最小 10，最大 360，且不超过 `RecentHours` 对应的分钟数
- `FrameIntervalMs`
  - 每帧播放间隔，单位毫秒，包含保持和淡化时间；淡化占后 40%，最长 200 ms
- `RecentHours`
  - 回溯最近多少小时的时间戳来构建动画
- `LoopPauseMilliseconds`
  - 一轮播放完成后的停顿时间，单位毫秒

## 开发与运行

### 本地运行

```powershell
dotnet run --project .\src\EarthBackground.csproj
```

### 构建

```powershell
dotnet build .\src\EarthBackground.csproj
```

### 发布示例

```powershell
dotnet publish .\src\EarthBackground.csproj --framework net10.0-windows --runtime win-x64 --configuration Release --self-contained true
dotnet publish .\src\EarthBackground.csproj --framework net10.0 --runtime linux-x64 --configuration Release --self-contained true
dotnet publish .\src\EarthBackground.csproj --framework net10.0 --runtime osx-x64 --configuration Release --self-contained true
dotnet publish .\src\EarthBackground.csproj --framework net10.0 --runtime osx-arm64 --configuration Release --self-contained true
```

### 后台服务模式

```powershell
dotnet run --project .\src\EarthBackground.csproj -- --service
```

## 当前界面

项目已经迁移到 Avalonia 桌面 UI，主界面包含：
- 当前状态
- 总体进度条
- 开始 / 停止 / 设置 / 退出

设置页包含：
- 抓取设置
- 动态壁纸相关参数
- 下载器配置
- 保存路径选择

## 说明

- Windows 动态壁纸依赖桌面 `WorkerW` 行为，不同 Windows 版本会有兼容处理；24H2 若只激活旧式桌面消息，图标层可能继续覆盖静态壁纸，表现为图标附近黑块而其余背景不变。
- macOS 全屏 Space 不支持 desktop window level 覆盖全屏应用，进入全屏应用时窗口会被系统遮挡
- Linux 动态壁纸目前支持 X11；GNOME / KDE / 带桌面图标扩展的环境对图标层和点击行为可能不同，需要在目标桌面环境实测
- 仓库中仍保留部分 APNG 相关代码，主要用于过渡和后续兼容实验，当前默认播放路径是 PNG 序列

## 路线图
- [ ] 添加cli支持，方便接入AI
- [ ] 添加linux macOS静态壁纸更新
- [ ] 支持linux Wayland 动态壁纸更新
