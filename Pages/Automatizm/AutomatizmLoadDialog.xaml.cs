using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic.Automatism;
using AIStudio.ViewModels;

namespace AIStudio.Dialogs
{
  public partial class AutomatizmLoadDialog : Window
  {
    public AutomatizmLoadDialogViewModel ViewModel { get; private set; }

    public AutomatizmLoadDialog(
        string bootDataFolder,
        AutomatizmFileLoader automatizmFileLoader = null)
    {
      InitializeComponent();

      // Создаем ViewModel
      ViewModel = new AutomatizmLoadDialogViewModel(bootDataFolder, automatizmFileLoader ?? AutomatizmFileLoader.Instance);

      // Устанавливаем DataContext
      DataContext = ViewModel;

      // Устанавливаем CloseAction
      ViewModel.CloseAction = (result) =>
      {
        DialogResult = result;
        Close();
      };
      ViewModel.SwitchToPromptTabAction = () =>
      {
        if (MainTabControl != null && MainTabControl.Items.Count > 1)
          MainTabControl.SelectedIndex = 1;
      };
    }

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
      ViewModel.LoadCsvContent();
      ViewModel.LoadPromptContent();
      ViewModel.LoadInsertTextContent();
    }
  }

  public class AutomatizmLoadDialogViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private readonly string _bootDataFolder;
    private readonly AutomatizmFileLoader _automatizmFileLoader;

    public Action<bool> CloseAction { get; set; }
    public Action SwitchToPromptTabAction { get; set; }

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

    private const string ChainsListFileName = "automatizm_chains_list.txt";
    private const string PromptFileName = "prompt_automatizm_chains.txt";
    private const string PromptInsertFileName = "automatizm_prompt_insert.txt";

    private static readonly string DefaultInsertTextTemplate = @"Сгенерируй строки автоматизмов (цепочек диалога) в формате (строка — одна запись, поля разделены только символом |):
Состояние|Комбинации стилей|Фразы через дефис с пробелами|Тон|Настроение

Правила:
- Состояние: строго одно из — Плохо, Норма, Хорошо
- Комбинации стилей: имена через + (например: Поиск+Игра)
- Фразы: цепочка фраз, разделённых « - » или «;» (например: привет - как дела - все ок)
- Тон: строго одно из списка, только кириллица — Вялый, Нормальный, Повышенный
- Настроение: строго одно из списка, только кириллица — Нормальное, Хорошее, Плохое, Игривое, Учитель, Агрессивное, Защитное, Протест

Примеры:
Норма|Поиск+Игра|привет - как дела - все ок|Нормальный|Хорошее
Хорошо|Расслабление|здравствуй - отлично|Нормальный|Нормальное

Сгенерируй примерно 10–15 строк на каждое состояние.";

    // Команды
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SaveCsvCommand { get; }
    public RelayCommand ValidateCsvCommand { get; }
    public RelayCommand SavePromptCommand { get; }
    public RelayCommand SaveInsertTextCommand { get; }
    public RelayCommand CreatePromptCommand { get; }

    public AutomatizmLoadDialogViewModel(string bootDataFolder, AutomatizmFileLoader automatizmFileLoader)
    {
      _bootDataFolder = bootDataFolder;
      _automatizmFileLoader = automatizmFileLoader;

      CancelCommand = new RelayCommand(ExecuteCancel);
      ApplyCommand = new RelayCommand(ExecuteApply, CanExecuteApply);
      SaveCsvCommand = new RelayCommand(ExecuteSaveCsv, CanExecuteSaveCsv);
      ValidateCsvCommand = new RelayCommand(ExecuteValidateCsv);
      SavePromptCommand = new RelayCommand(ExecuteSavePrompt, CanExecuteSavePrompt);
      SaveInsertTextCommand = new RelayCommand(ExecuteSaveInsertText, CanExecuteSaveInsertText);
      CreatePromptCommand = new RelayCommand(ExecuteCreatePrompt);

      LoadCsvContent();
      LoadPromptContent();
      LoadInsertTextContent();
    }

    #region CSV Properties

    private string _filePath;
    public string FilePath
    {
      get
      {
        if (string.IsNullOrEmpty(_filePath))
          _filePath = Path.Combine(_bootDataFolder, ChainsListFileName);
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

    public bool IsEditingEnabled => true;

    #endregion

    #region Prompt Properties

    private string _promptFilePath;
    public string PromptFilePath => _promptFilePath ?? (_promptFilePath = Path.Combine(_bootDataFolder, PromptFileName));

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

    private string _promptInsertText;
    public string PromptInsertText
    {
      get => _promptInsertText;
      set
      {
        _promptInsertText = value;
        OnPropertyChanged(nameof(PromptInsertText));
        SaveInsertTextCommand?.RaiseCanExecuteChanged();
      }
    }

    public bool IsPromptEditingEnabled => !string.IsNullOrEmpty(PromptFilePath);

    #endregion

    public bool CanApply => !string.IsNullOrWhiteSpace(CsvContent);

    #region Command CanExecute

    private bool CanExecuteApply(object parameter)
    {
      return CanApply;
    }

    private bool CanExecuteSaveCsv(object parameter)
    {
      return !string.IsNullOrWhiteSpace(CsvContent);
    }

    private bool CanExecuteSavePrompt(object parameter)
    {
      return !string.IsNullOrWhiteSpace(PromptContent);
    }

    private bool CanExecuteSaveInsertText(object parameter)
    {
      return !string.IsNullOrWhiteSpace(PromptInsertText);
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

        // Формат: Состояние|Комбинации стилей|фраза1 - фраза2|Тон|Настроение
        var parts = trimmedLine.Split('|');
        if (parts.Length >= 5)
        {
          var phrasePart = parts[2].Trim();
          if (phrasePart.Contains(";") || phrasePart.Contains(" - "))
            return true;
        }
      }

      return false;
    }

    #endregion

    #region Load Methods

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
        string path = Path.Combine(_bootDataFolder, PromptFileName);
        PromptContent = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
      }
      catch (Exception ex)
      {
        PromptContent = "# Ошибка загрузки промпта: " + ex.Message;
      }
    }

    public void LoadInsertTextContent()
    {
      try
      {
        string path = Path.Combine(_bootDataFolder, PromptInsertFileName);
        PromptInsertText = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : DefaultInsertTextTemplate;
      }
      catch (Exception ex)
      {
        PromptInsertText = DefaultInsertTextTemplate + "\n\n# Ошибка загрузки: " + ex.Message;
      }
    }

    #endregion

    #region Command Executions

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
              "В файле не обнаружено строк формата Состояние|Комбинации стилей|Фразы|Тон|Настроение.\n\nВсё равно сохранить?",
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
            "Текст пуст.",
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

        var parts = trimmedLine.Split('|');
        if (parts.Length >= 5)
        {
          var phrasePart = parts[2].Trim();
          bool hasSemicolon = phrasePart.Contains(";");
          bool hasDashSeparator = phrasePart.Contains(" - ");
          var phraseParts = hasSemicolon
              ? phrasePart.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
              : phrasePart.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
          if ((hasSemicolon || hasDashSeparator) && phraseParts.Length >= 2 && phraseParts.All(p => !string.IsNullOrWhiteSpace(p)))
            validLines++;
          else
          {
            invalidLines++;
            if (invalidExamples.Count < 3)
              invalidExamples.Add(trimmedLine.Length > 50 ? trimmedLine.Substring(0, 47) + "..." : trimmedLine);
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
        message = $"✅ Текст корректен.\n\n" +
                  $"📊 Статистика:\n" +
                  $"• Валидных строк: {validLines}\n" +
                  $"• Строк с комментариями: {commentLines}\n\n" +
                  $"Формат: Состояние|Комбинации стилей|Фразы|Тон|Настроение";
        icon = MessageBoxImage.Information;
      }
      else if (validLines == 0 && invalidLines > 0)
      {
        message = $"❌ Нет корректных строк.\n\n" +
                  $"Ожидается формат: Состояние|Комбинации стилей|Фразы|Тон|Настроение\n" +
                  $"Пример: Норма|Поиск+Игра|привет - как дела - все ок|Нормальный|Хорошее";
        if (invalidExamples.Any())
          message += $"\n\nПримеры: {string.Join("\n", invalidExamples)}";
        icon = MessageBoxImage.Warning;
      }
      else
      {
        message = $"⚠️ Корректных строк: {validLines}, некорректных: {invalidLines}.";
        if (invalidExamples.Any())
          message += $"\nПримеры ошибок:\n{string.Join("\n", invalidExamples)}";
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

    private void ExecuteSaveInsertText(object parameter)
    {
      try
      {
        Directory.CreateDirectory(_bootDataFolder);
        string path = Path.Combine(_bootDataFolder, PromptInsertFileName);
        File.WriteAllText(path, PromptInsertText, Encoding.UTF8);
        MessageBox.Show("Текст сохранён.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteCreatePrompt(object parameter)
    {
      try
      {
        var gomeostas = GomeostasSystem.Instance;
        if (gomeostas == null)
        {
          MessageBox.Show("Система гомеостаза не инициализирована.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }
        gomeostas.UpdateAgentPropertiesPromptContent();
        string part1 = AppGlobalState.AgentPropertiesPromptContent ?? string.Empty;
        string insertTemplate = PromptInsertText ?? string.Empty;
        string part2 = gomeostas.ReplacePromptTemplatePlaceholders(insertTemplate);
        string fullPrompt = string.IsNullOrWhiteSpace(part1)
          ? part2.Trim()
          : (part1.TrimEnd() + "\r\n\r\n" + part2.Trim()).Trim();
        PromptContent = fullPrompt;
        SwitchToPromptTabAction?.Invoke();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка создания промпта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ExecuteApply(object parameter)
    {
      var content = CsvContent?.Trim();
      if (string.IsNullOrEmpty(content))
      {
        MessageBox.Show("Введите текст автоматизмов во вкладке «Текст автоматизмов».", "Нет данных",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      IsBusy = true;
      try
      {
        var count = await Task.Run(() => _automatizmFileLoader.LoadFromContent(content));
        MessageBox.Show($"Создано автоматизмов по цепочкам: {count}.", "Готово",
            MessageBoxButton.OK, MessageBoxImage.Information);
        CloseAction?.Invoke(true);
      }
      catch (ArgumentException ex)
      {
        IsBusy = false;
        MessageBox.Show(ex.Message, "Ошибка валидации",
            MessageBoxButton.OK, MessageBoxImage.Warning);
      }
      catch (Exception ex)
      {
        IsBusy = false;
        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteCancel(object parameter)
    {
      CloseAction?.Invoke(false);
    }

    #endregion
  }

}