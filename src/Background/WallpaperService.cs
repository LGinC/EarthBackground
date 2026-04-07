using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EarthBackground.Background
{
    public class WallpaperService(
        IServiceProvider serviceProvider,
        ILogger<WallpaperService> logger,
        IOptionsMonitor<CaptureOption> options,
        IBackgroudSetProvider backgroundSetProvider,
        WindowsDynamicWallpaperSetter dynamicWallpaperSetter)
        : BackgroundService
    {
        private int _ossFetchCount;
        private CancellationTokenSource? _customCancellationTokenSource;

        public event Action<string>? StatusChanged;
        public event Action<int, int>? ProgressChanged;
        public event Action<string>? ImageSaved;
        public event Action<Exception>? ErrorOccurred;

        public bool IsRunning { get; private set; }

        private TaskCompletionSource<bool>? _triggerTcs;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Default to running if configured, or wait for manual start?
            // Original app started on button click.
            // We will wait for Start command.
            while (!stoppingToken.IsCancellationRequested)
            {
                if (IsRunning)
                {
                    try
                    {
                        await RunCycleAsync(stoppingToken);

                        // Wait for interval
                        int intervalMinutes = options.CurrentValue.Interval;
                        if (intervalMinutes < 1) intervalMinutes = 10;

                        _triggerTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                        try
                        {
                            var delayTask = Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
                            var triggerTask = _triggerTcs.Task;

                            // Wait for either delay or trigger
                            await Task.WhenAny(delayTask, triggerTask);
                        }
                        finally
                        {
                            _triggerTcs = null;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in WallpaperService cycle");
                        ErrorOccurred?.Invoke(ex);
                        // Wait a bit before retrying on error to avoid tight loop
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        public void TriggerUpdate()
        {
            _triggerTcs?.TrySetResult(true);
        }

        public void Start()
        {
            _customCancellationTokenSource = new CancellationTokenSource();
            IsRunning = true;
            StatusChanged?.Invoke("Running");
        }

        public void Stop()
        {
            _customCancellationTokenSource?.Cancel();
            IsRunning = false;
            dynamicWallpaperSetter.StopDynamicBackground();
            StatusChanged?.Invoke("Stopped");
        }

        private async Task RunCycleAsync(CancellationToken token)
        {
            var combinedToken = _customCancellationTokenSource != null
                ? CancellationTokenSource.CreateLinkedTokenSource(token, _customCancellationTokenSource.Token).Token
                : token;
            try
            {
                await RunCycleInternalAsync(combinedToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "error in cycle");
            }
        }

        private async Task RunCycleInternalAsync(CancellationToken token)
        {
            StatusChanged?.Invoke("Initializing...");

            using var scope = serviceProvider.CreateScope();
            var captorKey = options.CurrentValue.Captor;
            var captor = scope.ServiceProvider.GetRequiredKeyedService<ICaptor>(captorKey);
            var setter = backgroundSetProvider.GetSetter();

            logger.LogInformation("Starting capture with {captorKey}", captorKey);
            StatusChanged?.Invoke("Downloading...");

            var currentProgress = 0;
            var totalProgress = 0;

            void setTotalHandler(int t) { totalProgress = t; currentProgress = 0; ProgressChanged?.Invoke(currentProgress, totalProgress); }
            void setProgressHandler() { currentProgress++; ProgressChanged?.Invoke(currentProgress, totalProgress); }

            var currentOptions = options.CurrentValue;
            {
                var useDynamic = currentOptions.DynamicWallpaper;
                var recentHours = currentOptions.RecentHours > 0 ? currentOptions.RecentHours : 24;
                var frameIntervalMs = currentOptions.FrameIntervalMs > 0 ? currentOptions.FrameIntervalMs : 500;

                if (useDynamic)
                {
                    // 动态模式：用帧级总进度，不订阅 tile 级下载事件
                    const int dynamicSetupSteps = 2;
                    var frameProgressTotal = 0;

                    void onFrameComplete(int done, int total)
                    {
                        frameProgressTotal = total;
                        ProgressChanged?.Invoke(done, total + dynamicSetupSteps);
                    }

                    void onDynamicSetupProgress(int done, int total)
                    {
                        var baseTotal = frameProgressTotal > 0 ? frameProgressTotal : recentHours;
                        ProgressChanged?.Invoke(baseTotal + done, baseTotal + total);
                    }

                    var imagePaths = await captor.GetImagePaths(recentHours, onFrameComplete, token);

                    _ossFetchCount++;
                    if (_ossFetchCount > 3)
                    {
                        _ossFetchCount = 0;
                        await captor.ResetAsync(token);
                    }

                    if (imagePaths.Count > 0)
                    {
                        logger.LogInformation("Dynamic wallpaper: {Count} frames", imagePaths.Count);
                        ImageSaved?.Invoke(imagePaths[0]);

                        if (currentOptions.SetWallpaper)
                        {
                            StatusChanged?.Invoke("Setting Wallpaper...");
                            await dynamicWallpaperSetter.SetDynamicBackgroundAsync(imagePaths, frameIntervalMs, onDynamicSetupProgress, token);
                        }

                        if (currentOptions.SaveWallpaper)
                        {
                            SaveWallpaperLocal(imagePaths[0]);
                        }
                    }

                    StatusChanged?.Invoke("Complete");
                    ProgressChanged?.Invoke(imagePaths.Count + dynamicSetupSteps, imagePaths.Count + dynamicSetupSteps);
                }
                else
                {
                    // 单张静态壁纸：订阅 tile 级下载进度
                    captor.Downloader.SetTotal += setTotalHandler;
                    captor.Downloader.SetCurrentProgress += setProgressHandler;
                    try
                    {
                        var imagePath = await captor.GetImagePath(token);

                        _ossFetchCount++;
                        if (_ossFetchCount > 3)
                        {
                            _ossFetchCount = 0;
                            await captor.ResetAsync(token);
                        }

                        logger.LogInformation("Wallpaper saved: {ImagePath}", imagePath);
                        ImageSaved?.Invoke(imagePath);

                        if (currentOptions.SetWallpaper)
                        {
                            StatusChanged?.Invoke("Setting Wallpaper...");
                            await setter.SetBackgroundAsync(imagePath, token);
                        }

                        if (currentOptions.SaveWallpaper)
                        {
                            SaveWallpaperLocal(imagePath);
                        }

                        StatusChanged?.Invoke("Complete");
                        ProgressChanged?.Invoke(totalProgress, totalProgress);
                    }
                    finally
                    {
                        captor.Downloader.SetTotal -= setTotalHandler;
                        captor.Downloader.SetCurrentProgress -= setProgressHandler;
                    }
                }
            }
        }

        private void SaveWallpaperLocal(string imagePath)
        {
            try
            {
                var info = new FileInfo(imagePath);
                var savePath = AppPaths.ResolveInAppDirectory(options.CurrentValue.SavePath);
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                var dest = Path.Combine(savePath, info.Name);
                if (dest == imagePath) return;
                if (File.Exists(dest)) File.Delete(dest);
                File.Copy(imagePath, dest);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save wallpaper locally");
            }
        }
    }
}
