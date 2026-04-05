using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace EarthBackground.Background
{
    /// <summary>
    /// Windows 动态壁纸设置器
    /// 原理：向 Progman 发送特殊消息生成 WorkerW 子窗口，
    /// 然后将自定义绘图窗口设为 WorkerW 的子窗口，实现在桌面图标下方播放图片序列。
    /// 参考：https://www.codeproject.com/Articles/856020/Draw-Behind-Desktop-Icons-in-Windows-plus
    /// </summary>
    public class WindowsDynamicWallpaperSetter : IBackgroundSetter, IDisposable
    {
        public string Platform => nameof(OSPlatform.Windows);

        private readonly ILogger<WindowsDynamicWallpaperSetter> _logger;
        private CancellationTokenSource? _animationCts;
        private Task? _animationTask;

        // 动态壁纸窗口
        private IntPtr _workerW = IntPtr.Zero;
        private IntPtr _hostParentHwnd = IntPtr.Zero;
        private IntPtr _wallpaperHwnd = IntPtr.Zero;
        private IntPtr _currentBitmapHandle = IntPtr.Zero;

        public WindowsDynamicWallpaperSetter(ILogger<WindowsDynamicWallpaperSetter> logger)
        {
            _logger = logger;
        }

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string? lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        private const uint SMTO_NORMAL = 0x0000;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_EX_NOACTIVATE = 0x08000000;
        private const uint SS_BITMAP = 0x0000000E;
        private const uint STM_SETIMAGE = 0x0172;
        private const int IMAGE_BITMAP = 0;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const int SW_SHOW = 5;

        #endregion

        public Task SetBackgroundAsync(string filePath, CancellationToken token = default)
        {
            // 单张静态壁纸：停止动态播放，回退到系统壁纸设置
            StopDynamicBackground();
            Wallpaper.Set(filePath);
            return Task.CompletedTask;
        }

        public async Task SetDynamicBackgroundAsync(IReadOnlyList<string> filePaths, int frameIntervalMs = 500, CancellationToken token = default)
        {
            if (filePaths == null || filePaths.Count == 0) return;

            // 停止之前的动画
            StopDynamicBackground();

            // 获取桌面壁纸承载层
            _workerW = GetWorkerW(out var progman);
            if (_workerW == IntPtr.Zero)
            {
                _logger.LogWarning("未能获取 WorkerW 窗口，回退到静态壁纸");
                Wallpaper.Set(filePaths[0]);
                return;
            }

            _logger.LogInformation(
                "获取到 WorkerW: 0x{WorkerW:X}, Progman: 0x{Progman:X}, WorkerWClass={WorkerWClass}, ProgmanClass={ProgmanClass}",
                _workerW.ToInt64(),
                progman.ToInt64(),
                GetWindowClassName(_workerW),
                GetWindowClassName(progman));

            int screenW = GetSystemMetrics(SM_CXSCREEN);
            int screenH = GetSystemMetrics(SM_CYSCREEN);

            await Dispatcher.UIThread.InvokeAsync(() => EnsureWallpaperHostWindow(screenW, screenH, progman));
            if (_wallpaperHwnd == IntPtr.Zero)
            {
                _logger.LogWarning("未能创建动态壁纸承载窗口，回退到静态壁纸");
                Wallpaper.Set(filePaths[0]);
                return;
            }

            // 预加载所有图片
            var bitmaps = new List<Bitmap>();
            try
            {
                foreach (var path in filePaths)
                {
                    try
                    {
                        bitmaps.Add(new Bitmap(path));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "加载图片失败: {Path}", path);
                    }
                }

                if (bitmaps.Count == 0)
                {
                    _logger.LogWarning("没有可用图片帧");
                    return;
                }

                _animationCts = new CancellationTokenSource();
                var cts = _animationCts;

                _animationTask = Task.Run(() => RunAnimationLoop(bitmaps, screenW, screenH, frameIntervalMs, cts.Token), cts.Token);
                _logger.LogInformation("动态壁纸已启动，共 {Count} 帧，帧间隔 {Interval}ms", bitmaps.Count, frameIntervalMs);
            }
            catch
            {
                foreach (var b in bitmaps) b.Dispose();
                throw;
            }

            await Task.CompletedTask;
        }

        public void StopDynamicBackground()
        {
            _animationCts?.Cancel();
            try
            {
                _animationTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
            _animationCts?.Dispose();
            _animationCts = null;
            _animationTask = null;
            Dispatcher.UIThread.Post(DestroyWallpaperHostWindow);
            _workerW = IntPtr.Zero;
            _hostParentHwnd = IntPtr.Zero;
            _logger.LogInformation("动态壁纸已停止");
        }

        /// <summary>
        /// 动画循环：将帧更新到 WorkerW 下的子窗口
        /// </summary>
        private void RunAnimationLoop(List<Bitmap> bitmaps, int screenW, int screenH, int frameIntervalMs, CancellationToken token)
        {
            int frameIndex = 0;

            _logger.LogInformation("动画循环开始，屏幕分辨率: {W}x{H}", screenW, screenH);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var bitmap = bitmaps[frameIndex % bitmaps.Count];
                    Dispatcher.UIThread.InvokeAsync(() => PresentFrame(bitmap, screenW, screenH)).GetAwaiter().GetResult();
                    frameIndex++;

                    Thread.Sleep(frameIntervalMs);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "动画帧绘制失败");
                    Thread.Sleep(1000);
                }
            }

            // 清理：释放 bitmaps
            foreach (var b in bitmaps) b.Dispose();
            _logger.LogInformation("动画循环结束");
        }

        /// <summary>
        /// 创建挂在 WorkerW 下的宿主窗口。直接绘制 WorkerW 的 DC 不会形成持久壁纸层，
        /// 这里改为创建一个真实子窗口，让每一帧都显示在该窗口上。
        /// </summary>
        private void EnsureWallpaperHostWindow(int screenW, int screenH, IntPtr progman)
        {
            foreach (var parent in GetHostParentCandidates(_workerW, progman))
            {
                if (parent == IntPtr.Zero) continue;

                if (_wallpaperHwnd != IntPtr.Zero && _hostParentHwnd != parent)
                {
                    DestroyWallpaperHostWindow();
                }

                if (_wallpaperHwnd == IntPtr.Zero)
                {
                    _wallpaperHwnd = CreateWindowEx(
                        WS_EX_NOACTIVATE,
                        "Static",
                        null,
                        WS_CHILD | WS_VISIBLE | SS_BITMAP,
                        0,
                        0,
                        screenW,
                        screenH,
                        parent,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    if (_wallpaperHwnd == IntPtr.Zero)
                    {
                        _logger.LogWarning(
                            "创建壁纸宿主窗口失败，Parent=0x{Parent:X}, ParentClass={ParentClass}, Error={Error}",
                            parent.ToInt64(),
                            GetWindowClassName(parent),
                            Marshal.GetLastWin32Error());
                        continue;
                    }
                }

                SetParent(_wallpaperHwnd, parent);
                SetWindowPos(_wallpaperHwnd, IntPtr.Zero, 0, 0, screenW, screenH, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                ShowWindow(_wallpaperHwnd, SW_SHOW);
                _hostParentHwnd = parent;

                _logger.LogInformation(
                    "动态壁纸宿主窗口已创建: Host=0x{Host:X}, Parent=0x{Parent:X}, ParentClass={ParentClass}, Size={W}x{H}",
                    _wallpaperHwnd.ToInt64(),
                    parent.ToInt64(),
                    GetWindowClassName(parent),
                    screenW,
                    screenH);
                return;
            }
        }

        private void PresentFrame(Bitmap bitmap, int screenW, int screenH)
        {
            if (_wallpaperHwnd == IntPtr.Zero) return;

            if (_hostParentHwnd != IntPtr.Zero)
            {
                _logger.LogDebug(
                    "呈现动态壁纸帧: Host=0x{Host:X}, Parent=0x{Parent:X}, ParentClass={ParentClass}",
                    _wallpaperHwnd.ToInt64(),
                    _hostParentHwnd.ToInt64(),
                    GetWindowClassName(_hostParentHwnd));
            }

            using var scaledBitmap = new Bitmap(screenW, screenH);
            using (var g = Graphics.FromImage(scaledBitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, screenW, screenH);
            }

            IntPtr nextBitmap = scaledBitmap.GetHbitmap();
            IntPtr previousBitmap = SendMessage(_wallpaperHwnd, STM_SETIMAGE, new IntPtr(IMAGE_BITMAP), nextBitmap);

            if (previousBitmap != IntPtr.Zero && previousBitmap != _currentBitmapHandle)
            {
                DeleteObject(previousBitmap);
            }

            if (_currentBitmapHandle != IntPtr.Zero && _currentBitmapHandle != previousBitmap)
            {
                DeleteObject(_currentBitmapHandle);
            }

            _currentBitmapHandle = nextBitmap;
        }

        private void DestroyWallpaperHostWindow()
        {
            if (_wallpaperHwnd != IntPtr.Zero)
            {
                DestroyWindow(_wallpaperHwnd);
                _wallpaperHwnd = IntPtr.Zero;
            }

            if (_currentBitmapHandle != IntPtr.Zero)
            {
                DeleteObject(_currentBitmapHandle);
                _currentBitmapHandle = IntPtr.Zero;
            }

            _hostParentHwnd = IntPtr.Zero;
        }

        /// <summary>
        /// 获取 WorkerW 窗口句柄，兼容 Win10/Win11 23H2 及以前、Win11 24H2+
        /// 原理：向 Progman 发送 0x052C 消息触发桌面分层，
        /// 然后找到位于桌面图标层（SHELLDLL_DefView）下方的 WorkerW
        /// 参考：https://www.codeproject.com/Articles/856020/Draw-Behind-Desktop-Icons-in-Windows-plus
        ///       https://blog.cast1e.top/posts/windeskchange/wdc/
        /// </summary>
        private IntPtr GetWorkerW(out IntPtr progman)
        {
            // 1. 找到 Progman 窗口
            progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                _logger.LogWarning("未找到 Progman 窗口");
                return IntPtr.Zero;
            }

            // 2. 向 Progman 发送两次特殊消息，触发 WorkerW 生成
            //    wParam=0, lParam=0 和 wParam=0, lParam=1
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out _);
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, new IntPtr(1), SMTO_NORMAL, 1000, out _);

            // 3. 优先尝试 Win11 24H2+ 方式：WorkerW 是 Progman 的直接子窗口
            IntPtr workerW = TryGetWorkerWFromProgman(progman);
            if (workerW != IntPtr.Zero)
            {
                _logger.LogInformation("Win11 24H2+ 模式：WorkerW 是 Progman 子窗口");
                return workerW;
            }

            // 4. 回退到旧版方式：枚举顶层窗口，找含 SHELLDLL_DefView 的 WorkerW，
            //    然后找其后的兄弟 WorkerW
            workerW = TryGetWorkerWFromTopLevel();
            if (workerW != IntPtr.Zero)
            {
                _logger.LogInformation("旧版模式：WorkerW 是顶层窗口");
                return workerW;
            }

            _logger.LogWarning("两种方式均未找到 WorkerW");
            return IntPtr.Zero;
        }

        /// <summary>
        /// Win11 24H2+ 方式：SHELLDLL_DefView 和 WorkerW 都是 Progman 的直接子窗口
        /// </summary>
        private IntPtr TryGetWorkerWFromProgman(IntPtr progman)
        {
            // 检查 SHELLDLL_DefView 是否在 Progman 下
            IntPtr shellDllDefView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellDllDefView == IntPtr.Zero) return IntPtr.Zero;

            // WorkerW 也是 Progman 的子窗口，在 SHELLDLL_DefView 之后
            IntPtr workerW = FindWindowEx(progman, shellDllDefView, "WorkerW", null);
            return workerW;
        }

        /// <summary>
        /// 旧版方式（Win10/Win11 23H2-）：枚举顶层窗口
        /// 找到含 SHELLDLL_DefView 的顶层 WorkerW，然后找其后的兄弟 WorkerW
        /// </summary>
        private IntPtr TryGetWorkerWFromTopLevel()
        {
            IntPtr workerW = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                IntPtr shellDllDefView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDllDefView != IntPtr.Zero)
                {
                    // 找到含 SHELLDLL_DefView 的窗口（第一个 WorkerW），
                    // 在根级别从该窗口之后找下一个 WorkerW（第二个 WorkerW）
                    workerW = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                    return false; // 停止枚举
                }
                return true;
            }, IntPtr.Zero);
            return workerW;
        }

        public void Dispose()
        {
            StopDynamicBackground();
        }

        private IEnumerable<IntPtr> GetHostParentCandidates(IntPtr workerW, IntPtr progman)
        {
            yield return workerW;

            if (progman != IntPtr.Zero && progman != workerW)
            {
                yield return progman;
            }
        }

        private string GetWindowClassName(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return "<zero>";

            var className = new System.Text.StringBuilder(256);
            var length = GetClassName(hwnd, className, className.Capacity);
            return length > 0 ? className.ToString() : "<unknown>";
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
