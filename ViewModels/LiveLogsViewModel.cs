using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AIStudio.Common;

namespace AIStudio.ViewModels
{
  /// <summary>
  /// Модель представления для страницы живых логов
  /// </summary>
  public class LiveLogsViewModel : INotifyPropertyChanged, IDisposable
  {
    /// <summary>
    /// Событие изменения свойства
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly DispatcherTimer _refreshTimer;
    private bool _isAutoRefreshEnabled = true;
    private bool _disposed = false;

    /// <summary>
    /// Коллекция записей логов только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> LogEntries => MemoryLogManager.Instance.LogEntries;

    /// <summary>
    /// Команда очистки логов
    /// </summary>
    public ICommand ClearLogsCommand { get; }

    /// <summary>
    /// Команда переключения автообновления
    /// </summary>
    public ICommand ToggleAutoRefreshCommand { get; }

    /// <summary>
    /// Статус автообновления
    /// </summary>
    public string AutoRefreshStatus => _isAutoRefreshEnabled ? "Автообновление: ВКЛ" : "Автообновление: ВЫКЛ";

    /// <summary>
    /// Цвет индикатора автообновления
    /// </summary>
    public Brush AutoRefreshColor => _isAutoRefreshEnabled ? Brushes.Green : Brushes.Red;

    /// <summary>
    /// Конструктор модели представления живых логов
    /// </summary>
    public LiveLogsViewModel()
    {
      ClearLogsCommand = new RelayCommand(_ => ClearLogs());
      ToggleAutoRefreshCommand = new RelayCommand(_ => ToggleAutoRefresh());

      // Таймер для обновления интерфейса
      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(100) // 10 FPS для плавной анимации
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    /// <summary>
    /// Обновляет отображение логов
    /// </summary>
    private void RefreshDisplay()
    {
      if (_disposed) return;

      if (_isAutoRefreshEnabled)
      {
        // Принудительно обновляем привязку
        OnPropertyChanged(nameof(LogEntries));
      }
    }

    /// <summary>
    /// Очищает все логи
    /// </summary>
    private void ClearLogs()
    {
      if (_disposed) return;

      MemoryLogManager.Instance.Clear();
      OnPropertyChanged(nameof(LogEntries));
    }

    /// <summary>
    /// Переключает режим автообновления
    /// </summary>
    private void ToggleAutoRefresh()
    {
      if (_disposed) return;

      _isAutoRefreshEnabled = !_isAutoRefreshEnabled;
      OnPropertyChanged(nameof(AutoRefreshStatus));
      OnPropertyChanged(nameof(AutoRefreshColor));
    }

    /// <summary>
    /// Вызывает событие изменения свойства
    /// </summary>
    /// <param name="propertyName">Имя измененного свойства</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Освобождает ресурсы модели представления
    /// </summary>
    public void Dispose()
    {
      if (_disposed) return;

      _refreshTimer?.Stop();
      _disposed = true;
    }
  }
}