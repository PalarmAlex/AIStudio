using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AIStudio.Common;
using System.Collections.Generic;

namespace AIStudio.ViewModels
{
  public class StyleLogsViewModel : INotifyPropertyChanged, IDisposable
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed = false;
    private StyleLogGroup _selectedRow;

    public ObservableCollection<StyleLogGroup> StyleLogGroups { get; } = new ObservableCollection<StyleLogGroup>();
    public ICommand ClearLogsCommand { get; }

    public StyleLogGroup SelectedRow
    {
      get => _selectedRow;
      set
      {
        _selectedRow = value;
        OnPropertyChanged(nameof(SelectedRow));
      }
    }

    public StyleLogsViewModel()
    {
      ClearLogsCommand = new RelayCommand(_ => ClearLogs());

      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(500)
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    private void RefreshDisplay()
    {
      if (_disposed) return;

      // Сохраняем текущее выделение перед обновлением
      var previouslySelectedPulse = SelectedRow?.Pulse;

      // Группируем данные по пульсу
      var groupedData = MemoryLogManager.Instance.StyleLogEntries
          .GroupBy(entry => entry.Pulse)
          .OrderByDescending(g => g.Key) // Сначала новые пульсы
          .Select(g => new StyleLogGroup
          {
            Pulse = g.Key,
            Timestamp = g.First().Timestamp, // Берем время первого элемента группы
            BaseStyles = g.Where(e => e.Stage == "Base")
                           .Select(e => new StyleInfo
                           {
                             StyleId = e.StyleId,
                             StyleName = e.StyleName,
                             Weight = e.Weight
                           })
                           .ToList(),
            AfterAntagonistStyles = g.Where(e => e.Stage == "AfterAntagonists")
                                     .Select(e => new StyleInfo
                                     {
                                       StyleId = e.StyleId,
                                       StyleName = e.StyleName,
                                       Weight = e.Weight
                                     })
                                     .ToList(),
            FinalStyles = g.Where(e => e.Stage == "Final")
                           .Select(e => new StyleInfo
                           {
                             StyleId = e.StyleId,
                             StyleName = e.StyleName,
                             Weight = e.Weight
                           })
                           .ToList()
          })
          .ToList();

      // Обновляем коллекцию
      Application.Current.Dispatcher.Invoke(() =>
      {
        // Сохраняем выделение
        StyleLogGroup rowToSelect = null;

        StyleLogGroups.Clear();
        foreach (var group in groupedData)
        {
          StyleLogGroups.Add(group);

          // Восстанавливаем выделение если нужно
          if (previouslySelectedPulse.HasValue && group.Pulse == previouslySelectedPulse.Value)
          {
            rowToSelect = group;
          }
        }

        // Восстанавливаем выделение после обновления данных
        if (rowToSelect != null)
        {
          SelectedRow = rowToSelect;
        }
      });
    }

    private void ClearLogs()
    {
      if (_disposed) return;

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleLogGroups.Clear();
        SelectedRow = null;
      });

      // Также очищаем исходные данные
      MemoryLogManager.Instance.ClearStyleLogs();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
      if (_disposed) return;
      _refreshTimer?.Stop();
      _disposed = true;
    }


    /// <summary>
    /// Группированные данные по пульсам
    /// </summary>
    public class StyleLogGroup
    {
      public DateTime Timestamp { get; set; }
      public int Pulse { get; set; }

      // Базовые стили
      public List<StyleInfo> BaseStyles { get; set; } = new List<StyleInfo>();

      // Стили после антагонистов
      public List<StyleInfo> AfterAntagonistStyles { get; set; } = new List<StyleInfo>();

      // Финальные стили
      public List<StyleInfo> FinalStyles { get; set; } = new List<StyleInfo>();

      public string DisplayTime => Timestamp.ToString("HH:mm:ss");
      public string DisplayPulse => Pulse.ToString();

      // Для отображения в таблице
      public string DisplayBaseStyles => string.Join("\n", BaseStyles.Select(s => s.ToString()));
      public string DisplayAfterAntagonistStyles => string.Join("\n", AfterAntagonistStyles.Select(s => s.ToString()));
      public string DisplayFinalStyles => string.Join("\n", FinalStyles.Select(s => s.ToString()));
    }

    /// <summary>
    /// Информация о стиле
    /// </summary>
    public class StyleInfo
    {
      public int StyleId { get; set; }
      public string StyleName { get; set; } = string.Empty;
      public int Weight { get; set; }
      public override string ToString()
      {
        return $"{StyleName} (ID:{StyleId}) | Вес:{Weight}";
      }
    }
  }
}