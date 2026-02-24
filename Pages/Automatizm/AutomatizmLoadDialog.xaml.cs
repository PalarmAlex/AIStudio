using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ISIDA.Gomeostas;
using ISIDA.Common;
using ISIDA.Psychic.Automatism;
using AIStudio.ViewModels;

namespace AIStudio.Dialogs
{
  public partial class AutomatizmLoadDialog : Window
  {
    public AutomatizmLoadDialogViewModel ViewModel { get; private set; }

    public AutomatizmLoadDialog(
        GomeostasSystem gomeostasSystem,
        string bootDataFolder,
        AutomatizmFileLoader automatizmFileLoader = null)
    {
      InitializeComponent();

      // Создаем ViewModel
      ViewModel = new AutomatizmLoadDialogViewModel(gomeostasSystem, bootDataFolder, automatizmFileLoader ?? AutomatizmFileLoader.Instance);

      // Устанавливаем DataContext
      DataContext = ViewModel;

      // Устанавливаем CloseAction
      ViewModel.CloseAction = (result, baseState, styleIds) =>
      {
        DialogResult = result;
        SelectedBaseState = baseState;
        SelectedStyleIds = styleIds;
        Close();
      };
    }

    public int? SelectedBaseState { get; private set; }
    public List<int> SelectedStyleIds { get; private set; }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      // Автоматически загружаем содержимое CSV и промптов при загрузке окна
      ViewModel.LoadCsvContent();
      ViewModel.LoadPromptContent();
      ViewModel.LoadPrompt1Content();
    }
  }

  public class AutomatizmLoadDialogViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private readonly GomeostasSystem _gomeostasSystem;
    private readonly string _bootDataFolder;
    private readonly AutomatizmFileLoader _automatizmFileLoader;

    public Action<bool, int?, List<int>> CloseAction { get; set; }

    private bool _isBusy;
    public bool IsBusy
    {
      get => _isBusy;
      set
      {
        _isBusy = value;
        OnPropertyChanged(nameof(IsBusy));
      }
    }

    // Команды
    public RelayCommand LoadStylesCommand { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SaveCsvCommand { get; }
    public RelayCommand ValidateCsvCommand { get; }
    public RelayCommand SavePromptCommand { get; }
    public RelayCommand SavePrompt1Command { get; }

    public AutomatizmLoadDialogViewModel(GomeostasSystem gomeostasSystem, string bootDataFolder, AutomatizmFileLoader automatizmFileLoader)
    {
      _gomeostasSystem = gomeostasSystem;
      _bootDataFolder = bootDataFolder;
      _automatizmFileLoader = automatizmFileLoader;

      // Инициализация команд
      LoadStylesCommand = new RelayCommand(ExecuteGenerateStyles);
      CancelCommand = new RelayCommand(ExecuteCancel);
      ApplyCommand = new RelayCommand(ExecuteApply, CanExecuteApply);
      SaveCsvCommand = new RelayCommand(ExecuteSaveCsv, CanExecuteSaveCsv);
      ValidateCsvCommand = new RelayCommand(ExecuteValidateCsv);
      SavePromptCommand = new RelayCommand(ExecuteSavePrompt, CanExecuteSavePrompt);
      SavePrompt1Command = new RelayCommand(ExecuteSavePrompt1, CanExecuteSavePrompt1);

      // Базовые состояния
      BaseStates = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(-1, "Плохо"),
                new KeyValuePair<int, string>(0, "Норма"),
                new KeyValuePair<int, string>(1, "Хорошо")
            };

      SelectedBaseState = null; // По умолчанию ничего не выбрано

      // Загружаем комбинации стилей
      LoadStyleCombinations();

      // Загружаем содержимое CSV и промптов
      LoadCsvContent();
      LoadPromptContent();
      LoadPrompt1Content();
    }

    #region CSV Properties

    private string _filePath;
    public string FilePath
    {
      get
      {
        if (string.IsNullOrEmpty(_filePath))
        {
          _filePath = Path.Combine(_bootDataFolder, "automatizm_generate_list.csv");
        }
        return _filePath;
      }
    }

    private string _csvContent;
    public string CsvContent
    {
      get => _csvContent;
      set
      {
        _csvContent = value;
        OnPropertyChanged(nameof(CsvContent));
        OnPropertyChanged(nameof(CanApply));
        SaveCsvCommand?.RaiseCanExecuteChanged();
      }
    }

    public bool IsEditingEnabled => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

    #endregion

    #region Prompt Properties

    private string _promptFilePath1;
    public string PromptFilePath1
    {
      get
      {
        if (string.IsNullOrEmpty(_promptFilePath1))
        {
          _promptFilePath1 = Path.Combine(_bootDataFolder, "prompt_automatizm_generate_list_1.txt");
        }
        return _promptFilePath1;
      }
    }

    private string _promptContent1;
    public string PromptContent1
    {
      get => _promptContent1;
      set
      {
        _promptContent1 = value;
        OnPropertyChanged(nameof(PromptContent1));
        SavePrompt1Command?.RaiseCanExecuteChanged();
      }
    }

    public bool IsPrompt1EditingEnabled => !string.IsNullOrEmpty(PromptFilePath1) && File.Exists(PromptFilePath1);

    private string _promptFilePath;
    public string PromptFilePath
    {
      get
      {
        if (string.IsNullOrEmpty(_promptFilePath))
        {
          _promptFilePath = Path.Combine(_bootDataFolder, "prompt_automatizm_generate_list_2.txt");
        }
        return _promptFilePath;
      }
    }

    private string _promptContent;
    public string PromptContent
    {
      get => _promptContent;
      set
      {
        _promptContent = value;
        OnPropertyChanged(nameof(PromptContent));
        SavePromptCommand?.RaiseCanExecuteChanged();
      }
    }

    public bool IsPromptEditingEnabled => !string.IsNullOrEmpty(PromptFilePath) && File.Exists(PromptFilePath);

    #endregion

    #region Selection Properties

    public List<KeyValuePair<int, string>> BaseStates { get; }

    private int? _selectedBaseState;
    public int? SelectedBaseState
    {
      get => _selectedBaseState;
      set
      {
        _selectedBaseState = value;
        OnPropertyChanged(nameof(SelectedBaseState));
        OnPropertyChanged(nameof(SelectedBaseStateDisplay));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(ShowSelectionWarning));
        ApplyCommand?.RaiseCanExecuteChanged();
      }
    }

    public string SelectedBaseStateDisplay
    {
      get
      {
        if (!SelectedBaseState.HasValue) return "Не выбрано";
        if (SelectedBaseState.Value == -1) return "Плохо";
        if (SelectedBaseState.Value == 0) return "Норма";
        if (SelectedBaseState.Value == 1) return "Хорошо";
        return SelectedBaseState.Value.ToString();
      }
    }

    private List<StyleCombinationItem> _styleCombinations = new List<StyleCombinationItem>();
    public List<StyleCombinationItem> StyleCombinations
    {
      get => _styleCombinations;
      set
      {
        _styleCombinations = value;
        OnPropertyChanged(nameof(StyleCombinations));
      }
    }

    private List<int> _selectedStyleIds;
    public List<int> SelectedStyleIds
    {
      get => _selectedStyleIds;
      set
      {
        _selectedStyleIds = value;
        OnPropertyChanged(nameof(SelectedStyleIds));
        OnPropertyChanged(nameof(SelectedStylesDisplay));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(ShowSelectionWarning));
        OnPropertyChanged(nameof(SelectedStyleValidationMessage));
        OnPropertyChanged(nameof(SelectedStyleValidationColor));
        OnPropertyChanged(nameof(ShowStyleValidation));
        ApplyCommand?.RaiseCanExecuteChanged();
      }
    }

    public string SelectedStylesDisplay
    {
      get
      {
        if (SelectedStyleIds == null || SelectedStyleIds.Count == 0)
          return "Не выбрано";

        var selectedItem = StyleCombinations?.FirstOrDefault(x =>
            x.StyleIds != null && AreListsEqual(x.StyleIds, SelectedStyleIds));

        return selectedItem != null
            ? selectedItem.DisplayName
            : $"ID: {string.Join(", ", SelectedStyleIds)}";
      }
    }

    public string SelectedStyleValidationMessage
    {
      get
      {
        if (SelectedStyleIds == null || SelectedStyleIds.Count == 0)
          return "⚠️ Необходимо выбрать комбинацию стилей";
        return string.Empty;
      }
    }

    public Brush SelectedStyleValidationColor => Brushes.OrangeRed;

    public bool ShowStyleValidation => SelectedStyleIds == null || SelectedStyleIds.Count == 0;

    private string _stylesStatusText;
    public string StylesStatusText
    {
      get => _stylesStatusText;
      set
      {
        _stylesStatusText = value;
        OnPropertyChanged(nameof(StylesStatusText));
      }
    }

    public bool CanApply =>
        SelectedBaseState.HasValue &&
        SelectedStyleIds != null &&
        SelectedStyleIds.Count > 0 &&
        File.Exists(FilePath) &&
        !string.IsNullOrWhiteSpace(CsvContent) &&
        HasValidSeparators();

    public bool ShowSelectionWarning =>
        !SelectedBaseState.HasValue ||
        SelectedStyleIds == null ||
        SelectedStyleIds.Count == 0;

    #endregion

    #region Command CanExecute

    private bool CanExecuteApply(object parameter)
    {
      return CanApply;
    }

    private bool CanExecuteSaveCsv(object parameter)
    {
      return !string.IsNullOrWhiteSpace(CsvContent) && File.Exists(FilePath);
    }

    private bool CanExecuteSavePrompt(object parameter)
    {
      return !string.IsNullOrWhiteSpace(PromptContent) && File.Exists(PromptFilePath);
    }

    private bool CanExecuteSavePrompt1(object parameter)
    {
      return !string.IsNullOrWhiteSpace(PromptContent1) && File.Exists(PromptFilePath1);
    }

    #endregion

    #region Helper Methods

    private bool HasValidSeparators()
    {
      if (string.IsNullOrWhiteSpace(CsvContent))
        return false;

      var lines = CsvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

      foreach (var line in lines)
      {
        var trimmedLine = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
          continue;

        // Проверяем наличие хотя бы одного допустимого разделителя
        if (trimmedLine.Contains(";") || trimmedLine.Contains(" - "))
        {
          return true;
        }
      }

      return false;
    }

    private bool AreListsEqual(List<int> list1, List<int> list2)
    {
      if (list1 == null && list2 == null) return true;
      if (list1 == null || list2 == null) return false;
      if (list1.Count != list2.Count) return false;

      var sorted1 = list1.OrderBy(x => x).ToList();
      var sorted2 = list2.OrderBy(x => x).ToList();

      for (int i = 0; i < sorted1.Count; i++)
      {
        if (sorted1[i] != sorted2[i]) return false;
      }
      return true;
    }

    #endregion

    #region Load Methods

    private void LoadStyleCombinations()
    {
      try
      {
        List<List<GomeostasSystem.BehaviorStyle>> combinations;
        if (_gomeostasSystem != null)
        {
          combinations = _gomeostasSystem.LoadStyleCombinations();
        }
        else
        {
          combinations = new List<List<GomeostasSystem.BehaviorStyle>>();
        }

        var items = new List<StyleCombinationItem>();

        // Добавляем пустой элемент для возможности сброса выбора
        items.Add(new StyleCombinationItem
        {
          DisplayName = "[Не выбрано]",
          StyleIds = new List<int>()
        });

        foreach (var combo in combinations.OrderBy(c => c.Count))
        {
          var styleIds = combo.Select(s => s.Id).OrderBy(id => id).ToList();
          var styleNames = combo.Select(s => s.Name).ToList();

          items.Add(new StyleCombinationItem
          {
            DisplayName = $"[{combo.Count}]: {string.Join(" + ", styleNames)}",
            StyleIds = styleIds
          });
        }

        StyleCombinations = items;
        StylesStatusText = $"Загружено комбинаций: {combinations.Count}";

        // Сбрасываем выбор
        SelectedStyleIds = new List<int>();
      }
      catch (Exception ex)
      {
        StylesStatusText = $"Ошибка загрузки: {ex.Message}";
        StyleCombinations = new List<StyleCombinationItem>();
        SelectedStyleIds = new List<int>();
      }
    }

    public void LoadCsvContent()
    {
      try
      {
        if (File.Exists(FilePath))
        {
          CsvContent = File.ReadAllText(FilePath, Encoding.UTF8);
        }
        else
        {
          CsvContent = string.Empty;
        }
      }
      catch (Exception ex)
      {
        CsvContent = $"# Ошибка загрузки файла: {ex.Message}";
      }
    }

    public void LoadPromptContent()
    {
      try
      {
        if (File.Exists(PromptFilePath))
        {
          PromptContent = File.ReadAllText(PromptFilePath, Encoding.UTF8);
        }
        else
        {
          PromptContent = string.Empty;
        }
      }
      catch (Exception ex)
      {
        PromptContent = $"# Ошибка загрузки промпта: {ex.Message}";
      }
    }

    public void LoadPrompt1Content()
    {
      try
      {
        if (File.Exists(PromptFilePath1))
        {
          PromptContent1 = File.ReadAllText(PromptFilePath1, Encoding.UTF8);
        }
        else
        {
          PromptContent1 = string.Empty;
        }
      }
      catch (Exception ex)
      {
        PromptContent1 = $"# Ошибка загрузки промпта: {ex.Message}";
      }
    }

    #endregion

    #region Command Executions

    private void ExecuteGenerateStyles(object parameter)
    {
      try
      {
        List<List<GomeostasSystem.BehaviorStyle>> combinations;
        if (_gomeostasSystem != null)
        {
          combinations = _gomeostasSystem.GenerateStyleCombinations(true);
        }
        else
        {
          combinations = new List<List<GomeostasSystem.BehaviorStyle>>();
        }

        var items = new List<StyleCombinationItem>
                {
                    new StyleCombinationItem
                    {
                        DisplayName = "[Не выбрано]",
                        StyleIds = new List<int>()
                    }
                };

        foreach (var combo in combinations.OrderBy(c => c.Count))
        {
          var styleIds = combo.Select(s => s.Id).OrderBy(id => id).ToList();
          var styleNames = combo.Select(s => s.Name).ToList();

          items.Add(new StyleCombinationItem
          {
            DisplayName = $"[{combo.Count}]: {string.Join(" + ", styleNames)}",
            StyleIds = styleIds
          });
        }

        StyleCombinations = items;
        StylesStatusText = $"Сгенерировано комбинаций: {combinations.Count}";

        // Сбрасываем выбор, так как список изменился
        SelectedStyleIds = new List<int>();

        MessageBox.Show($"Сгенерировано {combinations.Count} комбинаций стилей",
            "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteSaveCsv(object parameter)
    {
      ExecuteSaveCsvInternal(suppressSuccessMessage: false);
    }

    private void ExecuteSaveCsvInternal(bool suppressSuccessMessage)
    {
      try
      {
        // Проверяем наличие разделителей, но не блокируем сохранение
        if (!HasValidSeparators() && !string.IsNullOrWhiteSpace(CsvContent))
        {
          var result = MessageBox.Show(
              "В файле не обнаружено корректных разделителей (; или -).\n" +
              "Возможно, файл имеет неправильный формат.\n\n" +
              "Всё равно сохранить?",
              "Предупреждение",
              MessageBoxButton.YesNo,
              MessageBoxImage.Warning);

          if (result != MessageBoxResult.Yes)
            return;
        }

        // Создаем директорию, если её нет
        Directory.CreateDirectory(_bootDataFolder);

        File.WriteAllText(FilePath, CsvContent, Encoding.UTF8);
        if (!suppressSuccessMessage)
        {
          MessageBox.Show("Файл успешно сохранен", "Сохранение",
              MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Обновляем состояние команд
        ApplyCommand?.RaiseCanExecuteChanged();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteValidateCsv(object parameter)
    {
      if (string.IsNullOrWhiteSpace(CsvContent))
      {
        MessageBox.Show(
            "Файл пуст.",
            "Проверка формата",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      var lines = CsvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      int validLines = 0;
      int invalidLines = 0;
      int commentLines = 0;
      var invalidExamples = new List<string>();

      foreach (var line in lines)
      {
        var trimmedLine = line.Trim();

        if (string.IsNullOrWhiteSpace(trimmedLine))
          continue;

        if (trimmedLine.StartsWith("#"))
        {
          commentLines++;
          continue;
        }

        bool hasSemicolon = trimmedLine.Contains(";");
        bool hasDashSeparator = trimmedLine.Contains(" - ");

        if (hasSemicolon || hasDashSeparator)
        {
          // Проверяем, что есть хотя бы две части
          if (hasSemicolon)
          {
            var parts = trimmedLine.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts.All(p => !string.IsNullOrWhiteSpace(p)))
            {
              validLines++;
            }
            else
            {
              invalidLines++;
              if (invalidExamples.Count < 3)
                invalidExamples.Add(trimmedLine.Length > 50 ? trimmedLine.Substring(0, 47) + "..." : trimmedLine);
            }
          }
          else // hasDashSeparator
          {
            var parts = trimmedLine.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts.All(p => !string.IsNullOrWhiteSpace(p)))
            {
              validLines++;
            }
            else
            {
              invalidLines++;
              if (invalidExamples.Count < 3)
                invalidExamples.Add(trimmedLine.Length > 50 ? trimmedLine.Substring(0, 47) + "..." : trimmedLine);
            }
          }
        }
        else
        {
          invalidLines++;
          if (invalidExamples.Count < 3)
            invalidExamples.Add(trimmedLine.Length > 50 ? trimmedLine.Substring(0, 47) + "..." : trimmedLine);
        }
      }

      string message;
      MessageBoxImage icon;

      if (invalidLines == 0 && validLines > 0)
      {
        message = $"✅ Файл корректен.\n\n" +
                  $"📊 Статистика:\n" +
                  $"• Валидных строк: {validLines}\n" +
                  $"• Строк с комментариями: {commentLines}\n" +
                  $"• Всего строк: {lines.Length}\n\n" +
                  $"Формат строк: фразы, разделенные ';' или ' - '";
        icon = MessageBoxImage.Information;
      }
      else if (validLines == 0 && invalidLines > 0)
      {
        message = $"❌ Файл содержит только некорректные строки.\n\n" +
                  $"📊 Статистика:\n" +
                  $"• Некорректных строк: {invalidLines}\n" +
                  $"• Строк с комментариями: {commentLines}\n\n" +
                  $"⚠️ Примеры ошибок:\n" +
                  $"{string.Join("\n", invalidExamples)}\n\n" +
                  $"✅ Правильный формат:\n" +
                  $"• фраза1;фраза2;фраза3\n" +
                  $"• фраза1 - фраза2 - фраза3";
        icon = MessageBoxImage.Warning;
      }
      else
      {
        message = $"⚠️ Файл содержит смешанные данные.\n\n" +
                  $"📊 Статистика:\n" +
                  $"• Корректных строк: {validLines}\n" +
                  $"• Некорректных строк: {invalidLines}\n" +
                  $"• Строк с комментариями: {commentLines}\n\n";

        if (invalidExamples.Any())
        {
          message += $"❌ Примеры ошибок:\n" +
                    $"{string.Join("\n", invalidExamples)}\n\n";
        }

        message += $"✅ Правильный формат:\n" +
                  $"• фраза1;фраза2;фраза3\n" +
                  $"• фраза1 - фраза2 - фраза3";
        icon = MessageBoxImage.Warning;
      }

      MessageBox.Show(message, "Проверка формата", MessageBoxButton.OK, icon);
    }

    private void ExecuteSavePrompt(object parameter)
    {
      try
      {
        // Создаем директорию, если её нет
        Directory.CreateDirectory(_bootDataFolder);

        File.WriteAllText(PromptFilePath, PromptContent, Encoding.UTF8);
        MessageBox.Show("Промпт успешно сохранен", "Сохранение",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка сохранения промпта: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteSavePrompt1(object parameter)
    {
      try
      {
        Directory.CreateDirectory(_bootDataFolder);

        File.WriteAllText(PromptFilePath1, PromptContent1, Encoding.UTF8);
        MessageBox.Show("Промпт успешно сохранен", "Сохранение",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка сохранения промпта: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ExecuteApply(object parameter)
    {
      // Автосохранение данных вкладки «Редактирование CSV» в файл перед применением (в т.ч. если файла ещё нет)
      if (!string.IsNullOrWhiteSpace(CsvContent) && !string.IsNullOrWhiteSpace(FilePath))
      {
        ExecuteSaveCsvInternal(suppressSuccessMessage: true);
      }

      IsBusy = true;
      try
      {
        var baseState = SelectedBaseState.Value;
        var styleIds = SelectedStyleIds ?? new List<int>();
        await Task.Run(() => _automatizmFileLoader.LoadFromFile(baseState, styleIds));
        CloseAction?.Invoke(true, baseState, styleIds);
      }
      catch (Exception ex)
      {
        IsBusy = false;
        MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteCancel(object parameter)
    {
      CloseAction?.Invoke(false, null, null);
    }

    #endregion
  }

  public class StyleCombinationItem
  {
    public string DisplayName { get; set; }
    public List<int> StyleIds { get; set; }
  }
}