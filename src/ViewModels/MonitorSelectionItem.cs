using EarthBackground.Background;
using ReactiveUI;

namespace EarthBackground.ViewModels
{
    public sealed class MonitorSelectionItem : ReactiveObject
    {
        private bool _isSelected;

        public MonitorSelectionItem(WallpaperMonitor monitor, bool isSelected)
        {
            Id = monitor.Id;
            Name = monitor.DisplayName;
            IsSelected = isSelected;
        }

        public string Id { get; }
        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
    }
}
