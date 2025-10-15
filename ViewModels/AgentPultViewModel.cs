using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class AgentPultViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GomeostasSystem _gomeostas;
    private readonly SensorySystem _sensorySystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private ObservableCollection<InfluenceActionItem> _influenceActions;
    private ObservableCollection<InfluenceActionItem> _column1Actions;
    private ObservableCollection<InfluenceActionItem> _column2Actions;
    private AntagonistManager _antagonistManager;
    private bool _isAgentDead;
    private bool _authoritativeMode;
    private string _messageText;
    private string _agentResponse;

    public bool IsEditingEnabled => !IsAgentDead;
    public bool IsAgentDead
    {
      get => _isAgentDead;
      set
      {
        if (_isAgentDead != value)
        {
          _isAgentDead = value;
          OnPropertyChanged();
          OnPropertyChanged(nameof(IsEditingEnabled));
        }
      }
    }

    public bool AuthoritativeMode
    {
      get => _authoritativeMode;
      set
      {
        if (_authoritativeMode != value)
        {
          _authoritativeMode = value;
          OnPropertyChanged();
        }
      }
    }

    public string MessageText
    {
      get => _messageText;
      set
      {
        if (_messageText != value)
        {
          _messageText = value;
          OnPropertyChanged();
        }
      }
    }

    public string AgentResponse
    {
      get => _agentResponse;
      set
      {
        if (_agentResponse != value)
        {
          _agentResponse = value;
          OnPropertyChanged();
        }
      }
    }

    private ICommand _applyInfluenceCommand;
    public ICommand ApplyInfluenceCommand => _applyInfluenceCommand ??
        (_applyInfluenceCommand = new RelayCommand(ApplyInfluenceActions, _ => IsEditingEnabled));

    public AgentPultViewModel()
    {
      _gomeostas = GomeostasSystem.Instance;
      _sensorySystem = SensorySystem.Instance;
      _influenceActionSystem = InfluenceActionSystem.Instance;
      _influenceActions = new ObservableCollection<InfluenceActionItem>();
      _column1Actions = new ObservableCollection<InfluenceActionItem>();
      _column2Actions = new ObservableCollection<InfluenceActionItem>();
      _messageText = "Привет";

      LoadInfluenceActions();
      UpdateAgentState();
    }

    private void UpdateAgentState()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        if (agentInfo != null)
        {
          IsAgentDead = agentInfo.IsDead;
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Ошибка получения состояния агента: {ex.Message}");
      }
    }

    public ObservableCollection<InfluenceActionItem> Column1Actions
    {
      get => _column1Actions;
      set
      {
        _column1Actions = value;
        OnPropertyChanged();
      }
    }

    public ObservableCollection<InfluenceActionItem> Column2Actions
    {
      get => _column2Actions;
      set
      {
        _column2Actions = value;
        OnPropertyChanged();
      }
    }

    public void LoadInfluenceActions()
    {
      _influenceActions.Clear();

      var column1 = new ObservableCollection<InfluenceActionItem>();
      var column2 = new ObservableCollection<InfluenceActionItem>();

      try
      {
        var allActions = _influenceActionSystem.GetAllInfluenceActions();

        int index = 0;
        foreach (var action in allActions)
        {
          var item = new InfluenceActionItem
          {
            Id = action.Id,
            Name = action.Name,
            Description = action.Description,
            IsSelected = false,
            AntagonistIds = new List<int>(action.AntagonistInfluences ?? new List<int>())
          };

          _influenceActions.Add(item);

          // Распределяем по столбцам
          if (index % 2 == 0)
            column1.Add(item);
          else
            column2.Add(item);

          index++;
        }

        // Инициализируем менеджер антагонистов
        _antagonistManager = new AntagonistManager(_influenceActions.Cast<AntagonistItem>().ToList());

        Column1Actions = column1;
        Column2Actions = column2;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки воздействий: {ex.Message}");
      }
    }

    public List<int> GetSelectedInfluenceActions()
    {
      return _influenceActions
          .Where(a => a.IsSelected)
          .Select(a => a.Id)
          .ToList();
    }

    public void ApplyInfluenceActions(object parameter)
    {
      if (IsAgentDead)
      {
        MessageBox.Show("Невозможно применить воздействие к мертвому агенту",
            "Агент мертв",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      var selectedActions = GetSelectedInfluenceActions();
      if (selectedActions.Count == 0 && string.IsNullOrWhiteSpace(MessageText))
      {
        MessageBox.Show("Не выбрано ни одного воздействия и не введено сообщение",
            "Внимание",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      try
      {
        List<int> phraseIds = new List<int>();

        // Обрабатываем текстовое сообщение, если оно есть
        if (!string.IsNullOrWhiteSpace(MessageText))
        {
          // Распознаем текст и получаем ID фраз
          phraseIds = _sensorySystem.VerbalChannel.RecognizeText(
              MessageText,
              AuthoritativeMode
          );

          // Формируем ответ агента
          if (phraseIds.Any())
          {
            var recognizedPhrases = phraseIds.Select(id =>
                _sensorySystem.VerbalChannel.GetPhraseFromPhraseId(id));
            AgentResponse = $"Распознанные фразы: {string.Join("; ", recognizedPhrases)}";
          }
          else
          {
            AgentResponse = "Фразы не распознаны (добавлены в песочницу для обучения)";
          }
        }

        // Применяем воздействия, если есть выбранные действия
        if (selectedActions.Any())
        {
          var (success, errorMessage, imageId) = _influenceActionSystem.ApplyMultipleInfluenceActions(
              selectedActions,
              phraseIds
          );

          if (!success)
          {
            if (errorMessage.Contains("Агент мертв"))
            {
              IsAgentDead = true;
              MessageBox.Show("Агент умер во время применения воздействий",
                  "Агент мертв",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
              return;
            }

            MessageBox.Show($"Не удалось применить воздействия: {errorMessage}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
          }

          // Добавляем информацию о созданном образе в ответ
          if (imageId > 0)
            AgentResponse += $"\nСоздан образ восприятия ID: {imageId}";

          // Обновляем состояние агента после воздействий
          UpdateAgentState();

          MessageBox.Show("Воздействия успешно применены",
              "Успех",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
        else
        {
          // Если только текстовое сообщение без действий
          MessageBox.Show("Сообщение обработано",
              "Успех",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при применении воздействий: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        System.Diagnostics.Debug.WriteLine($"Ошибка ApplyInfluenceActions: {ex.Message}");
      }
    }

    public void Dispose()
    {
      _antagonistManager?.Dispose();
    }
  }

  public class InfluenceActionItem : AntagonistItem
  {
    // доп свойства
  }
}