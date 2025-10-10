using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AIStudio.Common;
using ISIDA.Gomeostas;
using System.Windows.Controls;
using System.Globalization;
using ISIDA.Common;

namespace AIStudio.ViewModels
{
  public class SystemParametersViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private ObservableCollection<GomeostasSystem.ParameterData> _systemParameters;
    private bool _disposed = false;
    private int _currentAgentStage;
    private string _currentAgentName;
    private string _currentAgentDescription;

    public GomeostasSystem Gomeostas => _gomeostas;
    public bool IsStageZero => _currentAgentStage == 0;
    public Brush WarningMessageColor =>
        !IsStageZero ? Brushes.Red :
        Brushes.Gray;
    public bool IsEditingEnabled => IsStageZero && !GlobalTimer.IsPulsationRunning;
    public string PulseWarningMessage =>
        !IsStageZero
            ? "[КРИТИЧНО] Редактирование параметров доступно только в стадии 0"
            : GlobalTimer.IsPulsationRunning
                ? "Редактирование параметров доступно только при выключенной пульсации"
                : string.Empty;
    public event PropertyChangedEventHandler PropertyChanged;
    public ObservableCollection<GomeostasSystem.ParameterData> SystemParameters
    {
      get => _systemParameters;
      set
      {
        _systemParameters = value;
        OnPropertyChanged(nameof(SystemParameters));
        OnPropertyChanged(nameof(CurrentAgentTitle));
        OnPropertyChanged(nameof(CurrentAgentDescription));
      }
    }
    public string CurrentAgentTitle => $"Параметры гомеостаза Агента: {_currentAgentName ?? "Не определен"}";
    public string CurrentAgentDescription => _currentAgentDescription ?? "Нет описания";
    public ICommand SaveCommand { get; }
    public ICommand RemoveAllCommand { get; }
    public ICommand SelectAgentCommand { get; }

    public SystemParametersViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas;
      SystemParameters = new ObservableCollection<GomeostasSystem.ParameterData>();
      SaveCommand = new RelayCommand(_ => SaveParameters());
      RemoveAllCommand = new RelayCommand(_ => RemoveAllParameters());
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
    }

    /// <summary>
    /// Получает все стили поведения агента
    /// </summary>
    public ReadOnlyDictionary<int, GomeostasSystem.BehaviorStyle> GetAllBehaviorStyles()
    {
      return _gomeostas.GetAllBehaviorStyles();
    }

    public List<GomeostasSystem.ParameterData> GetAllParameters()
    {
      return _gomeostas.GetAllParameters();
    }

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
      });
    }

    public void RefreshState()
    {
      OnPulsationStateChanged();
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_disposed) return;

      if (disposing)
      {
        // Отписываемся от пульсаций
        //PulseMediator.Unsubscribe(PulseSubscriberId);
      }

      _disposed = true;
    }

    public void RefreshParameters()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
        _currentAgentDescription = agentInfo.Description;
        _currentAgentName = agentInfo.Name;

        OnPropertyChanged(nameof(IsStageZero));
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(CurrentAgentDescription));

        var parameters = _gomeostas.GetAllParameters()?
            .OrderBy(p => p.Id)
            .ToList();

        SystemParameters = new ObservableCollection<GomeostasSystem.ParameterData>(parameters ?? new List<GomeostasSystem.ParameterData>());
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки параметров: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void SaveParameters()
    {
      try
      {
        if (!_gomeostas.ValidateParameterIds(SystemParameters, out string erroMsg))
        {
          MessageBox.Show($"Ошибка валидации параметров:\n{erroMsg}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return;
        }

        // Сохраняем новые параметры
        foreach (var param in SystemParameters)
        {
          if (param.Id == 0) // Новый параметр
          {
            var (paramId, warnings) = _gomeostas.AddParameter(
                param.Name,
                param.Description,
                param.Value,
                param.Weight,
                param.NormaWell,
                param.Speed,
                param.RequiresExternalResources,
                param.IsVital,
                param.CriticalMinValue,
                param.CriticalMaxValue);

            if (warnings.Length > 0)
            {
              MessageBox.Show(string.Join("\n", warnings),
                  "Предупреждения при создании параметра",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }

            // После создания параметра обновляем его влияния
            var createdParam = _gomeostas.GetAllParameters()
                .First(p => p.Id == paramId);

            createdParam.BadStateInfluence = new Dictionary<int, float>(param.BadStateInfluence);
            createdParam.WellStateInfluence = new Dictionary<int, float>(param.WellStateInfluence);
            createdParam.StyleActivations = new Dictionary<int, List<int>>(param.StyleActivations);

            _gomeostas.UpdateParameter(createdParam);
          }
          else
            _gomeostas.UpdateParameter(param);
        }

        // Сохраняем с обработкой результата
        var (success, error) = _gomeostas.SaveAgentParameters(false);

        if (success)
        {
          RefreshParameters();
          MessageBox.Show("Параметры успешно сохранены",
              "Сохранение",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
        else
        {
          MessageBox.Show($"Не удалось сохранить параметры:\n{error}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Критическая ошибка при сохранении:\n{ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void RemoveParameters(IEnumerable<GomeostasSystem.ParameterData> parameters)
    {
      try
      {
        var paramsList = parameters.ToList();
        foreach (var param in paramsList)
        {
          // Удаляем из локальной коллекции
          if (SystemParameters.Contains(param))
          {
            SystemParameters.Remove(param);
          }

          // Удаляем из системы гомеостаза (если параметр уже сохранен)
          if (param.Id > 0)
          {
            try
            {
              _gomeostas.RemoveParameter(param.Id);

              var (success, error) = _gomeostas.SaveAgentParameters();
              if (!success)
              {
                MessageBox.Show($"Не удалось сохранить параметры:\n{error}",
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
              }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("системным"))
            {
              // Отлавливаем исключение о системном параметре
              MessageBox.Show($"Не удалось удалить параметр '{param.Name}':\n{ex.Message}",
                  "Системный параметр",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);

              // Возвращаем параметр обратно в коллекцию
              if (!SystemParameters.Contains(param))
              {
                SystemParameters.Add(param);
              }
              continue; // Переходим к следующему параметру
            }
            catch (Exception ex)
            {
              MessageBox.Show($"Ошибка при удалении параметра '{param.Name}':\n{ex.Message}",
                  "Ошибка удаления",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
              continue;
            }
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Общая ошибка при удалении параметров: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public void RemoveAllParameters()
    {
      var result = MessageBox.Show(
          $"Вы действительно хотите удалить ВСЕ параметры гомеостаза агента? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          // Удаляем все параметры из системы
          var allParam = _gomeostas.GetAllParameters().ToList();

          foreach (var param in allParam)
          {
            _gomeostas.RemoveParameter(param.Id);
          }

          // Очищаем коллекцию представления
          SystemParameters.Clear();

          var (success, error) = _gomeostas.SaveAgentParameters(false); // все удалено - не надо валидаций 
          if (success)
          {
            MessageBox.Show("Все параметры гомеостаза агента успешно удалены",
                "Удаление завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось удалить параметры гомеостаза агента:\n{error}",
                "Ошибка сохранения после удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления параметров гомеостаза агента: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }
  }
}