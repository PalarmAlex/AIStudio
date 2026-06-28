using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>Строка второй колонки пульта: воздействие метрики среды с выбором + (давление) или − (отпускание).</summary>
  public sealed class EnvironmentProbeActionItem : INotifyPropertyChanged
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    private bool _isPressure;
    private bool _isRelease;

    /// <summary>Давление метрики (+).</summary>
    public bool IsPressure
    {
      get => _isPressure;
      set
      {
        if (_isPressure == value)
          return;
        _isPressure = value;
        if (value)
          _isRelease = false;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsRelease));
      }
    }

    /// <summary>Отпускание метрики (−).</summary>
    public bool IsRelease
    {
      get => _isRelease;
      set
      {
        if (_isRelease == value)
          return;
        _isRelease = value;
        if (value)
          _isPressure = false;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsPressure));
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
