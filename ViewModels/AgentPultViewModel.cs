using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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
    private string _recognitionDisplayText;

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
          if(_messageText != "")
            UpdateRecognitionDisplay();
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

    public string RecognitionDisplayText
    {
      get => _recognitionDisplayText;
      set
      {
        if (_recognitionDisplayText != value)
        {
          _recognitionDisplayText = value;
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
      _recognitionDisplayText = "";
      MessageText = "Привет";

      LoadInfluenceActions();
      UpdateAgentState();
      UpdateRecognitionDisplay();
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

    /// <summary>
    /// Обновляет отображение распознанного текста с заменой нераспознанных слов на xxxxx
    /// </summary>
    private void UpdateRecognitionDisplay()
    {
      if (string.IsNullOrWhiteSpace(MessageText))
      {
        RecognitionDisplayText = "";
        return;
      }

      try
      {
        // Получаем все слова из дерева для проверки распознавания
        var allWords = _sensorySystem.VerbalChannel.GetAllWords();
        var recognizedWords = allWords.Values.Select(w => w.ToLower()).ToHashSet();

        // Разбиваем текст на части (слова, пробелы, знаки препинания)
        var parts = Regex.Split(MessageText, @"(\s+|[^\w\s])")
            .Where(part => !string.IsNullOrEmpty(part))
            .ToList();

        var resultParts = new List<string>();

        foreach (var part in parts)
        {
          // Проверяем, является ли часть словом (содержит буквы)
          if (Regex.IsMatch(part, @"\p{L}"))
          {
            // Это слово - проверяем распознавание
            if (recognizedWords.Contains(part.ToLower()))
            {
              // Распознанное слово - оставляем как есть
              resultParts.Add(part);
            }
            else
            {
              // Не распознанное слово - заменяем на xxxxx
              resultParts.Add("xxxxx");
            }
          }
          else
          {
            // Это пробелы или знаки препинания - оставляем как есть
            resultParts.Add(part);
          }
        }

        RecognitionDisplayText = string.Join("", resultParts);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка при обновлении отображения распознавания: {ex.Message}");
        // В случае ошибки показываем исходный текст
        RecognitionDisplayText = MessageText;
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
        MessageText = ""; // Очищаем только поле ввода
        return;
      }

      var selectedActions = GetSelectedInfluenceActions();
      if (selectedActions.Count == 0 && string.IsNullOrWhiteSpace(MessageText))
      {
        MessageBox.Show("Не выбрано ни одного воздействия и не введено сообщение",
            "Внимание",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        MessageText = ""; // Очищаем только поле ввода
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

          UpdateRecognitionDisplay(); // до очистки поля ввода!
          MessageText = "";
        }else
          UpdateRecognitionDisplay(); // чтобы очистило текст распознавания

        // Применяем воздействия, если есть выбранные действия или фраза
        if (selectedActions.Any() || phraseIds.Any())
        {
          var (success, errorMessage, imageId, imageGenRefId) = _influenceActionSystem.ApplyMultipleInfluenceActions(
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
          // Обновляем состояние агента после воздействий
          UpdateAgentState();
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при применении воздействий: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Debug.WriteLine($"Ошибка ApplyInfluenceActions: {ex.Message}");
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