using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EarthBackground.Background;
using EarthBackground.Localization;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace EarthBackground.ViewModels
{
    public class SettingsWindowViewModel : ReactiveObject
    {
        private readonly CaptureOption _capture;
        private readonly OssOption _oss;
        private readonly IConfigureSaver _configureSaver;
        private readonly WallpaperService _wallpaperService;
        private readonly ILocalizationService _loc;
        private readonly IWallpaperMonitorProvider _monitorProvider;

        // Capture settings
        private bool _autoStart;
        private NameValue<string>? _selectedCaptor;
        private NameValue<Resolution>? _selectedResolution;
        private int _interval;
        private int _zoom;
        private int _recentHours;
        private int _loopPauseMilliseconds;
        private bool _dynamicWallpaper;
        private bool _setWallpaper;
        private bool _saveWallpaper;
        private bool _allDynamicWallpaperMonitors = true;
        private bool _dynamicWallpaperMonitorListVisible;
        private string _savePath = string.Empty;

        // Download settings
        private NameValue<string>? _selectedDownloader;
        private string _username = string.Empty;
        private string _apiKey = string.Empty;
        private string _apiSecret = string.Empty;
        private string _domain = string.Empty;
        private string _bucket = string.Empty;
        private NameValue<string>? _selectedZone;
        private bool _usernameEnabled;
        private bool _apiKeyEnabled;
        private bool _apiSecretEnabled;
        private bool _zoneEnabled;
        private bool _domainEnabled;
        private bool _bucketEnabled;
        private bool _chooseSavePathEnabled;

        public ObservableCollection<NameValue<string>> Captors { get; } = new();
        public ObservableCollection<NameValue<Resolution>> Resolutions { get; } = new();
        public ObservableCollection<NameValue<string>> Downloaders { get; } = new();
        public ObservableCollection<NameValue<string>> Zones { get; } = new();
        public ObservableCollection<MonitorSelectionItem> DynamicWallpaperMonitors { get; } = new();

        public bool AutoStart
        {
            get => _autoStart;
            set => this.RaiseAndSetIfChanged(ref _autoStart, value);
        }

        public NameValue<string>? SelectedCaptor
        {
            get => _selectedCaptor;
            set => this.RaiseAndSetIfChanged(ref _selectedCaptor, value);
        }

        public NameValue<Resolution>? SelectedResolution
        {
            get => _selectedResolution;
            set => this.RaiseAndSetIfChanged(ref _selectedResolution, value);
        }

        public int Interval
        {
            get => _interval;
            set => this.RaiseAndSetIfChanged(ref _interval, value);
        }

        public int Zoom
        {
            get => _zoom;
            set => this.RaiseAndSetIfChanged(ref _zoom, value);
        }

        public int RecentHours
        {
            get => _recentHours;
            set => this.RaiseAndSetIfChanged(ref _recentHours, value);
        }

        public int LoopPauseMilliseconds
        {
            get => _loopPauseMilliseconds;
            set => this.RaiseAndSetIfChanged(ref _loopPauseMilliseconds, value);
        }

        public bool DynamicWallpaper
        {
            get => _dynamicWallpaper;
            set
            {
                this.RaiseAndSetIfChanged(ref _dynamicWallpaper, value);
                RefreshDynamicWallpaperMonitorVisibility();
            }
        }

        public bool SetWallpaper
        {
            get => _setWallpaper;
            set => this.RaiseAndSetIfChanged(ref _setWallpaper, value);
        }

        public bool SaveWallpaper
        {
            get => _saveWallpaper;
            set
            {
                this.RaiseAndSetIfChanged(ref _saveWallpaper, value);
                ChooseSavePathEnabled = value;
            }
        }

        public bool AllDynamicWallpaperMonitors
        {
            get => _allDynamicWallpaperMonitors;
            set
            {
                var wasAll = _allDynamicWallpaperMonitors;
                this.RaiseAndSetIfChanged(ref _allDynamicWallpaperMonitors, value);
                if (wasAll && !value && !DynamicWallpaperMonitors.Any(monitor => monitor.IsSelected))
                {
                    var firstMonitor = DynamicWallpaperMonitors.FirstOrDefault();
                    if (firstMonitor != null)
                    {
                        firstMonitor.IsSelected = true;
                    }
                }

                RefreshDynamicWallpaperMonitorVisibility();
            }
        }

        public bool DynamicWallpaperMonitorListVisible
        {
            get => _dynamicWallpaperMonitorListVisible;
            set => this.RaiseAndSetIfChanged(ref _dynamicWallpaperMonitorListVisible, value);
        }

        public string SavePath
        {
            get => _savePath;
            set => this.RaiseAndSetIfChanged(ref _savePath, value);
        }

        public NameValue<string>? SelectedDownloader
        {
            get => _selectedDownloader;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDownloader, value);
                OnDownloaderChanged(value?.Value);
            }
        }

        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => this.RaiseAndSetIfChanged(ref _apiKey, value);
        }

        public string ApiSecret
        {
            get => _apiSecret;
            set => this.RaiseAndSetIfChanged(ref _apiSecret, value);
        }

        public string Domain
        {
            get => _domain;
            set => this.RaiseAndSetIfChanged(ref _domain, value);
        }

        public string Bucket
        {
            get => _bucket;
            set => this.RaiseAndSetIfChanged(ref _bucket, value);
        }

        public NameValue<string>? SelectedZone
        {
            get => _selectedZone;
            set => this.RaiseAndSetIfChanged(ref _selectedZone, value);
        }

        public bool UsernameEnabled
        {
            get => _usernameEnabled;
            set => this.RaiseAndSetIfChanged(ref _usernameEnabled, value);
        }

        public bool ApiKeyEnabled
        {
            get => _apiKeyEnabled;
            set => this.RaiseAndSetIfChanged(ref _apiKeyEnabled, value);
        }

        public bool ApiSecretEnabled
        {
            get => _apiSecretEnabled;
            set => this.RaiseAndSetIfChanged(ref _apiSecretEnabled, value);
        }

        public bool ZoneEnabled
        {
            get => _zoneEnabled;
            set => this.RaiseAndSetIfChanged(ref _zoneEnabled, value);
        }

        public bool DomainEnabled
        {
            get => _domainEnabled;
            set => this.RaiseAndSetIfChanged(ref _domainEnabled, value);
        }

        public bool BucketEnabled
        {
            get => _bucketEnabled;
            set => this.RaiseAndSetIfChanged(ref _bucketEnabled, value);
        }

        public bool ChooseSavePathEnabled
        {
            get => _chooseSavePathEnabled;
            set => this.RaiseAndSetIfChanged(ref _chooseSavePathEnabled, value);
        }

        public ReactiveCommand<Unit, Unit> ChooseSavePathCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        // Reference to the window for folder picker
        public Window? OwnerWindow { get; set; }

        // Localized labels for AXAML binding
        public string WindowTitle => _loc["Settings_WindowTitle"];
        public string HeaderTitle => _loc["Settings_Header"];
        public string Label_CaptureSection => _loc["Settings_CaptureSection"];
        public string Label_DownloadSection => _loc["Settings_DownloadSection"];
        public string Label_AutoStart => _loc["Settings_AutoStart"];
        public string Label_DynamicWallpaper => _loc["Settings_DynamicWallpaper"];
        public string Label_SetWallpaper => _loc["Settings_SetWallpaper"];
        public string Label_SaveWallpaper => _loc["Settings_SaveWallpaper"];
        public string Label_Satellite => _loc["Settings_Satellite"];
        public string Label_Resolution => _loc["Settings_Resolution"];
        public string Label_Interval => _loc["Settings_Interval"];
        public string Label_IntervalUnit => _loc["Settings_IntervalUnit"];
        public string Label_Zoom => _loc["Settings_Zoom"];
        public string Label_ZoomUnit => _loc["Settings_ZoomUnit"];
        public string Label_RecentHours => _loc["Settings_RecentHours"];
        public string Label_LoopPauseMilliseconds => _loc["Settings_LoopPauseMilliseconds"];
        public string Label_DynamicWallpaperMonitors => _loc["Settings_DynamicWallpaperMonitors"];
        public string Label_AllDynamicWallpaperMonitors => _loc["Settings_AllDynamicWallpaperMonitors"];
        public string Label_SavePath => _loc["Settings_SavePath"];
        public string Label_ChoosePath => _loc["Settings_ChoosePath"];
        public string Label_Downloader => _loc["Settings_Downloader"];
        public string Label_Username => _loc["Settings_Username"];
        public string Label_ApiKey => _loc["Settings_ApiKey"];
        public string Label_ApiSecret => _loc["Settings_ApiSecret"];
        public string Label_Domain => _loc["Settings_Domain"];
        public string Label_Bucket => _loc["Settings_Bucket"];
        public string Label_Zone => _loc["Settings_Zone"];
        public string Label_Save => _loc["Settings_Save"];

        public SettingsWindowViewModel(
            IOptionsMonitor<CaptureOption> captureOption,
            IOptionsMonitor<OssOption> ossOption,
            IConfigureSaver saver,
            WallpaperService wallpaperService,
            ILocalizationService loc,
            IWallpaperMonitorProvider monitorProvider)
        {
            _configureSaver = saver;
            _wallpaperService = wallpaperService;
            _loc = loc;
            _monitorProvider = monitorProvider;
            _capture = captureOption.CurrentValue;
            _oss = ossOption.CurrentValue;

            // Initialize captors
            foreach (var name in NameConsts.CaptorNames)
                Captors.Add(new NameValue<string>(_loc[name], name));

            SelectedCaptor = Captors.FirstOrDefault(c => c.Value == _capture.Captor) ?? Captors.FirstOrDefault();

            // Initialize resolutions
            foreach (Resolution res in Enum.GetValues(typeof(Resolution)))
                Resolutions.Add(new NameValue<Resolution>(res.GetName(), res));

            SelectedResolution = Resolutions.FirstOrDefault(r => r.Value == _capture.Resolution) ?? Resolutions.FirstOrDefault();

            // Initialize downloaders
            foreach (var name in NameConsts.DownloaderNames)
                Downloaders.Add(new NameValue<string>(_loc[name], name));

            // Capture settings
            AutoStart = _capture.AutoStart;
            DynamicWallpaper = _capture.DynamicWallpaper;
            SetWallpaper = _capture.SetWallpaper;
            SaveWallpaper = _capture.SaveWallpaper;
            SavePath = AppPaths.ResolveInAppDirectory(_capture.SavePath);
            Interval = _capture.Interval;
            Zoom = _capture.Zoom;
            RecentHours = _capture.RecentHours;
            LoopPauseMilliseconds = _capture.LoopPauseMilliseconds;
            ChooseSavePathEnabled = _capture.SaveWallpaper;
            InitializeDynamicWallpaperMonitors();
            RefreshDynamicWallpaperMonitorVisibility();

            // Download settings
            if (!string.IsNullOrEmpty(_oss.CloudName) && _oss.IsEnable)
                SelectedDownloader = Downloaders.FirstOrDefault(d => d.Value == _oss.CloudName) ?? Downloaders.FirstOrDefault();
            else
                SelectedDownloader = Downloaders.FirstOrDefault();

            Username = _oss.UserName ?? string.Empty;
            ApiKey = _oss.ApiKey ?? string.Empty;
            ApiSecret = _oss.ApiSecret ?? string.Empty;
            Domain = _oss.Domain ?? string.Empty;
            Bucket = _oss.Bucket ?? string.Empty;

            OnDownloaderChanged(SelectedDownloader?.Value);

            if (!string.IsNullOrWhiteSpace(_oss.Zone))
            {
                var zone = Zones.FirstOrDefault(z => z.Value == _oss.Zone);
                if (zone != null) SelectedZone = zone;
            }

            ChooseSavePathCommand = ReactiveCommand.CreateFromTask(OnChooseSavePath, outputScheduler: AvaloniaScheduler.Instance);
            SaveCommand = ReactiveCommand.CreateFromTask(OnSave, outputScheduler: AvaloniaScheduler.Instance);
        }

        private void OnDownloaderChanged(string? cloud)
        {
            switch (cloud)
            {
                case NameConsts.DirectDownload:
                    UsernameEnabled = false;
                    ApiKeyEnabled = false;
                    ApiSecretEnabled = false;
                    ZoneEnabled = false;
                    DomainEnabled = false;
                    BucketEnabled = false;
                    break;
                case NameConsts.Cloudinary:
                    UsernameEnabled = true;
                    ApiKeyEnabled = true;
                    ApiSecretEnabled = true;
                    ZoneEnabled = false;
                    DomainEnabled = false;
                    BucketEnabled = false;
                    break;
                case NameConsts.Qiqiuyun:
                    UsernameEnabled = false;
                    ApiKeyEnabled = true;
                    ApiSecretEnabled = true;
                    ZoneEnabled = true;
                    DomainEnabled = true;
                    BucketEnabled = true;
                    Zones.Clear();
                    foreach (var z in new[] { "z0", "z1", "z2", "na0", "as0" })
                        Zones.Add(new NameValue<string>(_loc[z], z));
                    break;
                default:
                    UsernameEnabled = false;
                    ApiKeyEnabled = false;
                    ApiSecretEnabled = false;
                    ZoneEnabled = false;
                    DomainEnabled = false;
                    BucketEnabled = false;
                    break;
            }
        }

        private void InitializeDynamicWallpaperMonitors()
        {
            DynamicWallpaperMonitors.Clear();
            var selectedIds = new System.Collections.Generic.HashSet<string>(
                _capture.DynamicWallpaperMonitorIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            AllDynamicWallpaperMonitors = selectedIds.Count == 0;

            foreach (var monitor in _monitorProvider.GetMonitors())
            {
                DynamicWallpaperMonitors.Add(new MonitorSelectionItem(monitor, selectedIds.Contains(monitor.Id)));
            }
        }

        private void RefreshDynamicWallpaperMonitorVisibility()
        {
            DynamicWallpaperMonitorListVisible = DynamicWallpaper && !AllDynamicWallpaperMonitors;
        }

        private async Task OnChooseSavePath()
        {
            if (OwnerWindow == null) return;

            var result = await OwnerWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = _loc["Settings_ChoosePathTitle"],
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                SavePath = result[0].Path.LocalPath;
                _capture.SavePath = SavePath;
                _capture.WallpaperFolder = SavePath;
            }
        }

        private async Task OnSave()
        {
            _capture.Captor = SelectedCaptor?.Value ?? string.Empty;
            _capture.Interval = Interval;
            _capture.Resolution = SelectedResolution?.Value ?? Resolution.r_1376;
            _capture.DynamicWallpaper = DynamicWallpaper;
            _capture.SaveWallpaper = SaveWallpaper;
            _capture.SetWallpaper = SetWallpaper;
            _capture.Zoom = Zoom;
            _capture.RecentHours = RecentHours;
            _capture.LoopPauseMilliseconds = LoopPauseMilliseconds;
            _capture.DynamicWallpaperMonitorIds = AllDynamicWallpaperMonitors
                ? Array.Empty<string>()
                : DynamicWallpaperMonitors.Where(monitor => monitor.IsSelected).Select(monitor => monitor.Id).ToArray();
            _capture.AutoStart = AutoStart;
            _capture.SavePath = AppPaths.ResolveInAppDirectory(_capture.SavePath);
            _capture.WallpaperFolder = _capture.SavePath;

            _oss.IsEnable = true;
            _oss.CloudName = SelectedDownloader?.Value ?? NameConsts.DirectDownload;
            _oss.UserName = string.IsNullOrWhiteSpace(Username) ? _oss.UserName : Username;
            _oss.ApiKey = string.IsNullOrWhiteSpace(ApiKey) ? _oss.ApiKey : ApiKey;
            _oss.ApiSecret = string.IsNullOrWhiteSpace(ApiSecret) ? _oss.ApiSecret : ApiSecret;
            _oss.Bucket = string.IsNullOrWhiteSpace(Bucket) ? _oss.Bucket : Bucket;
            _oss.Domain = string.IsNullOrWhiteSpace(Domain) ? _oss.Domain :
                (!Domain.Contains("http://") && !Domain.Contains("https://")) ? $"http://{Domain}" : Domain;
            var selectZone = SelectedZone?.Value;
            _oss.Zone = string.IsNullOrWhiteSpace(selectZone) ? _oss.Zone : selectZone;

            await _configureSaver.SaveAsync(_capture, _oss);

            if (File.Exists(NameConsts.ImageIdPath))
                File.Delete(NameConsts.ImageIdPath);

            _wallpaperService.TriggerUpdate();

            // Handle AutoStart
            EarthBackground.AutoStart.Set(nameof(EarthBackground), _capture.AutoStart);

            OwnerWindow?.Close();
        }
    }
}
