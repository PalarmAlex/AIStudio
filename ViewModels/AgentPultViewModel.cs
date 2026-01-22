using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ISIDA.Psychic.Automatism;

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
    private readonly ReflexesActivator _reflexesActivator;
    private ObservableCollection<InfluenceActionItem> _influenceActions;
    private ObservableCollection<InfluenceActionItem> _column1Actions;
    private ObservableCollection<InfluenceActionItem> _column2Actions;
    private AntagonistManager _antagonistManager;
    private DispatcherTimer _chainStatusTimer;

    private bool _isAgentDead;
    private bool _authoritativeMode;
    private string _messageText;
    private string _recognitionDisplayText;

    private int _selectedToneId = 0;
    private int _selectedMoodId = 0;
    private Dictionary<int, string> _toneList;
    private Dictionary<int, string> _moodList;

    // Свойства для управления цепочкой
    private bool _chainStepSuccess = true;
    private System.Windows.Visibility _chainControlVisibility = System.Windows.Visibility.Collapsed;
    private bool _isChainActive = false;

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
          if (_messageText != "")
            UpdateRecognitionDisplay();
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

    private int _activeChainId = 0;

    public int ActiveChainId
    {
      get => _activeChainId;
      set
      {
        if (_activeChainId != value)
        {
          _activeChainId = value;
          OnPropertyChanged();
          OnPropertyChanged(nameof(ChainActiveStatusWithId));
        }
      }
    }

    public string ChainActiveStatusWithId
    {
      get
      {
        if (_isChainActive && _activeChainId > 0)
          return $"Цепочка активна (ID: {_activeChainId})";
        return "Цепочка не активна";
      }
    }

    #region Свойства для тона и настроения

    /// <summary>
    /// Список доступных тонов
    /// </summary>
    public Dictionary<int, string> ToneList
    {
      get => _toneList;
      set
      {
        _toneList = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Список доступных настроений
    /// </summary>
    public Dictionary<int, string> MoodList
    {
      get => _moodList;
      set
      {
        _moodList = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Выбранный ID тона
    /// </summary>
    public int SelectedToneId
    {
      get => _selectedToneId;
      set
      {
        if (_selectedToneId != value)
        {
          _selectedToneId = value;
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Выбранный ID настроения
    /// </summary>
    public int SelectedMoodId
    {
      get => _selectedMoodId;
      set
      {
        if (_selectedMoodId != value)
        {
          _selectedMoodId = value;
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Текстовое описание выбранного тона
    /// </summary>
    public string SelectedToneText
    {
      get => ActionsImagesSystem.GetToneText(SelectedToneId);
    }

    /// <summary>
    /// Текстовое описание выбранного настроения
    /// </summary>
    public string SelectedMoodText
    {
      get => ActionsImagesSystem.GetMoodText(SelectedMoodId);
    }

    #endregion

    #region Свойства для управления цепочкой

    /// <summary>
    /// Результат выполнения звена цепочки (успех)
    /// </summary>
    public bool ChainStepSuccess
    {
      get => _chainStepSuccess;
      set
      {
        if (_chainStepSuccess != value)
        {
          _chainStepSuccess = value;
          OnPropertyChanged();

          // Если выбрано "Успех", сбрасываем неудачу
          if (value)
            ChainStepFailure = false;

          UpdateChainStepResult();
        }
      }
    }

    /// <summary>
    /// Результат выполнения звена цепочки (неудача)
    /// </summary>
    public bool ChainStepFailure
    {
      get => !_chainStepSuccess;
      set
      {
        if (ChainStepFailure != value)
        {
          // Если выбрано "Неудача", устанавливаем успех в false
          ChainStepSuccess = !value;
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Видимость элементов управления цепочкой
    /// </summary>
    public System.Windows.Visibility ChainControlVisibility
    {
      get => _chainControlVisibility;
      set
      {
        if (_chainControlVisibility != value)
        {
          _chainControlVisibility = value;
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Статус активности цепочки
    /// </summary>
    public string ChainActiveStatus
    {
      get => ChainActiveStatusWithId;
    }

    /// <summary>
    /// Цвет индикатора активности цепочки
    /// </summary>
    public Brush ChainActiveIndicatorColor
    {
      get => _isChainActive ? Brushes.Green : Brushes.Gray;
    }

    /// <summary>
    /// Фон панели состояния цепочки
    /// </summary>
    public Brush ChainActiveBackground
    {
      get => _isChainActive ? Brushes.LightGreen : Brushes.LightGray;
    }

    /// <summary>
    /// Цвет текста статуса цепочки
    /// </summary>
    public Brush ChainActiveTextColor
    {
      get => _isChainActive ? Brushes.DarkGreen : Brushes.DarkGray;
    }

    #endregion

    #region Команды

    private ICommand _applyInfluenceCommand;
    public ICommand ApplyInfluenceCommand => _applyInfluenceCommand ??
        (_applyInfluenceCommand = new RelayCommand(ApplyInfluenceActions, _ => IsEditingEnabled));

    #endregion

    public AgentPultViewModel()
    {
      _gomeostas = GomeostasSystem.Instance;
      _sensorySystem = SensorySystem.Instance;
      _influenceActionSystem = InfluenceActionSystem.Instance;
      _reflexesActivator = ReflexesActivator.Instance;
      _influenceActions = new ObservableCollection<InfluenceActionItem>();
      _column1Actions = new ObservableCollection<InfluenceActionItem>();
      _column2Actions = new ObservableCollection<InfluenceActionItem>();
      _recognitionDisplayText = "";
      MessageText = "";

      LoadInfluenceActions();
      UpdateAgentState();
      UpdateRecognitionDisplay();

      InitializeToneAndMoodLists();
      InitializeChainStatusPolling();
    }

    /// <summary>
    /// Инициализирует списки тона и настроения
    /// </summary>
    private void InitializeToneAndMoodLists()
    {
      try
      {
        ToneList = ActionsImagesSystem.GetToneList();
        MoodList = ActionsImagesSystem.GetMoodList();

        SelectedToneId = 0;
        SelectedMoodId = 0;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка инициализации списков тона и настроения: {ex.Message}");
        ToneList = new Dictionary<int, string> { { 0, "Нормальный" } };
        MoodList = new Dictionary<int, string> { { 0, "Нормальное" } };
      }
    }

    /// <summary>
    /// Инициализирует периодическую проверку активности цепочки
    /// </summary>
    private void InitializeChainStatusPolling()
    {
      _chainStatusTimer = new DispatcherTimer();
      _chainStatusTimer.Interval = TimeSpan.FromMilliseconds(500);
      _chainStatusTimer.Tick += CheckChainStatus;
      _chainStatusTimer.Start();
    }

    /// <summary>
    /// Проверяет статус цепочки и обновляет UI
    /// </summary>
    private void CheckChainStatus(object sender, EventArgs e)
    {
      try
      {
        bool wasActive = _isChainActive;
        bool isChainActive = _reflexesActivator.IsChainActive;
        int newChainId = isChainActive ? _reflexesActivator.GetActiveChainId() : 0;

        if (wasActive != isChainActive || ActiveChainId != newChainId)
        {
          _isChainActive = isChainActive;
          ActiveChainId = newChainId;
          ChainControlVisibility = _isChainActive ?
              System.Windows.Visibility.Visible :
              System.Windows.Visibility.Collapsed;

          OnPropertyChanged(nameof(ChainActiveStatusWithId));
          OnPropertyChanged(nameof(ChainActiveStatus));
          OnPropertyChanged(nameof(ChainActiveIndicatorColor));
          OnPropertyChanged(nameof(ChainActiveBackground));
          OnPropertyChanged(nameof(ChainActiveTextColor));

          if (_isChainActive)
            ChainStepSuccess = true;
          else
            ChainControlVisibility = System.Windows.Visibility.Collapsed;
        }

        if (_isChainActive)
          UpdateChainStepResult();
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка при проверке статуса цепочки: {ex.Message}");
      }
    }

    /// <summary>
    /// Обновляет результат выполнения звена в активаторе рефлексов
    /// </summary>
    private void UpdateChainStepResult()
    {
      try
      {
        if (_isChainActive)
          _reflexesActivator.SetChainStepResult(_chainStepSuccess);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка при обновлении результата звена: {ex.Message}");
      }
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
        Debug.WriteLine($"Ошибка получения состояния агента: {ex.Message}");
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
            // Проверяем существование слова в дереве
            if (_sensorySystem.VerbalChannel.WordExists(part))
              resultParts.Add(part);
            else
              resultParts.Add("xxxxx");
          }
          else
            // Это пробелы или знаки препинания - оставляем как есть
            resultParts.Add(part);
        }

        RecognitionDisplayText = string.Join("", resultParts);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка при обновлении отображения распознавания: {ex.Message}");
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
        Debug.WriteLine($"Ошибка загрузки воздействий: {ex.Message}");
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
        }
        else
          UpdateRecognitionDisplay(); // чтобы очистило текст распознавания

        // Применяем воздействия, если есть выбранные действия или фраза
        if (selectedActions.Any() || phraseIds.Any())
        {
          var (success, errorMessage) = _influenceActionSystem.ApplyMultipleInfluenceActions(
              selectedActions,
              phraseIds,
              AuthoritativeMode,
              SelectedToneId,
              SelectedMoodId
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

      // Останавливаем таймер
      if (_chainStatusTimer != null)
      {
        _chainStatusTimer.Stop();
        _chainStatusTimer.Tick -= CheckChainStatus;
        _chainStatusTimer = null;
      }
    }
  }

  public class InfluenceActionItem : AntagonistItem
  {
    // доп свойства
  }
}