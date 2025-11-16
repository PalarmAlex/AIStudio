using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System.Linq;
using System.Collections.Generic;

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

    // Зависимости
    private readonly GomeostasSystem _gomeostas;
    private readonly PerceptionImagesSystem _perceptionImagesSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly VerbalSensorChannel _verbalSensor;
    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;

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
    public LiveLogsViewModel(
        GomeostasSystem gomeostas,
        PerceptionImagesSystem perceptionImagesSystem,
        InfluenceActionSystem influenceActionSystem,
        VerbalSensorChannel verbalSensor,
        AdaptiveActionsSystem adaptiveActionsSystem)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _perceptionImagesSystem = perceptionImagesSystem ?? throw new ArgumentNullException(nameof(perceptionImagesSystem));
      _influenceActionSystem = influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem));
      _verbalSensor = verbalSensor ?? throw new ArgumentNullException(nameof(verbalSensor));
      _adaptiveActionsSystem = adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem)); ;

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

    #region Конвертеры для ToolTip'ов

    /// <summary>
    /// Получает текст подсказки для стиля поведения
    /// </summary>
    public string GetStyleTooltip(string displayBaseStyleID)
    {
      if (string.IsNullOrEmpty(displayBaseStyleID) || !int.TryParse(displayBaseStyleID, out int imageId) || imageId <= 0)
        return "Нет данных о стилях";

      try
      {
        var styleImages = _perceptionImagesSystem.GetAllBehaviorStyleImagesList();
        var styleImage = styleImages.FirstOrDefault(img => img.Id == imageId);

        if (styleImage != null && styleImage.BehaviorStylesList.Any())
        {
          var allStyles = _gomeostas.GetAllBehaviorStyles();

          var styleNames = styleImage.BehaviorStylesList
              .Select(styleId => allStyles.ContainsKey(styleId) ? allStyles[styleId].Name : $"Стиль {styleId}")
              .Where(name => !string.IsNullOrEmpty(name));

          return string.Join(", ", styleNames);
        }
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки стилей: {ex.Message}";
      }

      return "Нет данных о стилях";
    }

    /// <summary>
    /// Получает текст подсказки для триггера
    /// </summary>
    public string GetTriggerTooltip(string displayTriggerStimulusID)
    {
      if (string.IsNullOrEmpty(displayTriggerStimulusID) || !int.TryParse(displayTriggerStimulusID, out int imageId) || imageId <= 0)
        return "Нет данных о триггере";

      try
      {
        var perceptionImages = _perceptionImagesSystem.GetAllPerceptionImagesList();
        var perceptionImage = perceptionImages.FirstOrDefault(img => img.Id == imageId);

        if (perceptionImage != null)
        {
          var tooltipParts = new List<string>();

          // Получаем названия внешних воздействий
          if (perceptionImage.InfluenceActionsList.Any())
          {
            var allInfluences = _influenceActionSystem.GetAllInfluenceActions();

            var influenceNames = perceptionImage.InfluenceActionsList
                .Select(actionId => allInfluences.FirstOrDefault(a => a.Id == actionId)?.Name ?? $"Воздействие {actionId}")
                .Where(name => !string.IsNullOrEmpty(name));

            if (influenceNames.Any())
              tooltipParts.Add($"Воздействия: {string.Join(", ", influenceNames)}");
          }

          // Получаем фразы
          if (perceptionImage.PhraseIdList.Any())
          {
            var phraseNames = perceptionImage.PhraseIdList
                .Select(phraseId => _verbalSensor?.GetPhraseFromPhraseId(phraseId) ?? $"Фраза {phraseId}")
                .Where(phrase => !string.IsNullOrEmpty(phrase));

            if (phraseNames.Any())
              tooltipParts.Add($"Фразы: {string.Join(", ", phraseNames)}");
          }

          return tooltipParts.Any() ? string.Join("\n", tooltipParts) : "Пустой образ восприятия";
        }
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки триггера: {ex.Message}";
      }

      return "Нет данных о триггере";
    }

    #endregion
  }
}