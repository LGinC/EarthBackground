using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace EarthBackground.Localization;

/// <summary>
/// Live-updating localized string binding: Text="{loc:Localize MainWindow_Header}".
/// Binds to <see cref="LocalizedText.Value"/> with standard property change notifications
/// (Avalonia 12 does not reliably refresh indexer "Item[]" notifications).
/// Backed by embed tables via <see cref="ILocalizationService"/> — trim-safe, no ResourceManager.
/// </summary>
public class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension()
    {
    }

    public LocalizeExtension(string key) => Key = key;

    public string Key { get; set; } = "";

    /// <summary>Optional converter (e.g. strip emoji prefix on buttons).</summary>
    public IValueConverter? Converter { get; set; }

    public object? ConverterParameter { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return string.Empty;

        return CompiledBinding.Create<LocalizedText, string>(
            entry => entry.Value,
            source: LocalizedStrings.Instance.GetEntry(Key),
            converter: Converter,
            mode: BindingMode.OneWay,
            converterParameter: ConverterParameter);
    }
}

/// <summary>Single key live value; language switch raises PropertyChanged for Value.</summary>
public sealed class LocalizedText : INotifyPropertyChanged
{
    private readonly LocalizedStrings _owner;
    private readonly string _key;

    public LocalizedText(LocalizedStrings owner, string key)
    {
        _owner = owner;
        _key = key;
    }

    public string Value => _owner[_key];

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}

/// <summary>
/// Shared localize source: caches per-key <see cref="LocalizedText"/> entries and
/// refreshes them when <see cref="ILocalizationService.LanguageChanged"/> fires.
/// </summary>
public sealed class LocalizedStrings : INotifyPropertyChanged
{
    private readonly ConcurrentDictionary<string, LocalizedText> _entries = new(StringComparer.Ordinal);
    private ILocalizationService _service;
    private Action<string>? _handler;

    public static LocalizedStrings Instance { get; } = new();

    private LocalizedStrings()
    {
        // Bootstrap with preferred culture (no loc-diag). DI will Attach the real singleton.
        _service = new ResourceLocalizationService(ResourceLocalizationService.ResolvePreferredUiCulture());
    }

    public string this[string key] => _service[key];

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizedText GetEntry(string key) =>
        _entries.GetOrAdd(key, static (k, self) => new LocalizedText(self, k), this);

    /// <summary>Wire DI service once at startup (or in tests). Safe to call multiple times.</summary>
    public void Attach(ILocalizationService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (ReferenceEquals(_service, service) && _handler is not null)
            return;

        if (_handler is not null)
            _service.LanguageChanged -= _handler;

        _service = service;
        _handler = _ => NotifyAll();
        _service.LanguageChanged += _handler;
        NotifyAll();
    }

    private void NotifyAll()
    {
        foreach (var entry in _entries.Values)
            entry.NotifyChanged();

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
