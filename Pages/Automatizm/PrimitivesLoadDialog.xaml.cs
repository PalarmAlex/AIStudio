using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AIStudio.ViewModels;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic.Automatism;

namespace AIStudio.Dialogs
{
  public partial class PrimitivesLoadDialog : Window
  {
    public PrimitivesLoadDialogViewModel ViewModel { get; }

    public PrimitivesLoadDialog(string bootDataFolder, Stage2PrimitivesLoader loader)
    {
      InitializeComponent();
      ViewModel = new PrimitivesLoadDialogViewModel(bootDataFolder ?? throw new ArgumentNullException(nameof(bootDataFolder)), loader ?? throw new ArgumentNullException(nameof(loader)));
      DataContext = ViewModel;
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

  public class PrimitivesLoadDialogViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private const string PrimitivesListFileName = "primitives_generate_list.txt";
    private const string PromptFileName = "prompt_primitives_generate.txt";
    private const string PromptInsertFileName = "primitives_prompt_insert.txt";

    private static readonly string DefaultInsertTextTemplate = @"Сгенерируй строки базовых примитивов в формате (строка — одна запись, поля разделены только символом |):
Состояние|Стили|Трехсложный паттерн|Тон|Настроение

Правила:
- Состояние: строго одно из — Плохо, Норма, Хорошо
- Стили: комбинации стилей через + (например: Расслабление+Игра)
- Трехсложный паттерн: три слога или коротких слова через пробел или дефис, без символа | (например: со ба ка или тик-так; при пробелах вербальная часть — без пробелов)
- Тон: строго одно из списка, только кириллица, без замены букв на латинские — Вялый, Нормальный, Повышенный
- Настроение: строго одно из списка, только кириллица, без замены букв на латинские — Нормальное, Хорошее, Плохое, Игривое, Учитель, Агрессивное, Защитное, Протест

Примеры (копируй написание Тон и Настроение точно, кириллицей):
Норма|Расслабление+Игра|со ба ка|Нормальный|Хорошее
Хорошо|Расслабление|ма ма мы|Нормальный|Нормальное

Сгенерируй примерно 10–15 строк на каждое состояние.";

    private readonly string _bootDataFolder;
    private readonly Stage2PrimitivesLoader _loader;

    public Action<bool> CloseAction { get; set; }
    public Action SwitchToPromptTabAction { get; set; }

    private bool _isBusy;
    public bool IsBusy
    {
      get => _isBusy;
      set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
    }

    public RelayCommand SaveCsvCommand { get; }
    public RelayCommand ValidateCsvCommand { get; }
    public RelayCommand SavePromptCommand { get; }
    public RelayCommand SaveInsertTextCommand { get; }
    public RelayCommand CreatePromptCommand { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CancelCommand { get; }

    public PrimitivesLoadDialogViewModel(string bootDataFolder, Stage2PrimitivesLoader loader)
    {
      _bootDataFolder = bootDataFolder;
      _loader = loader;

      SaveCsvCommand = new RelayCommand(ExecuteSaveCsv, CanExecuteSaveCsv);
      ValidateCsvCommand = new RelayCommand(ExecuteValidateCsv);
      SavePromptCommand = new RelayCommand(ExecuteSavePrompt, CanExecuteSavePrompt);
      SaveInsertTextCommand = new RelayCommand(ExecuteSaveInsertText, CanExecuteSaveInsertText);
      CreatePromptCommand = new RelayCommand(ExecuteCreatePrompt);
      ApplyCommand = new RelayCommand(ExecuteApply, _ => CanApply);
      CancelCommand = new RelayCommand(_ => CloseAction?.Invoke(false));
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
        ApplyCommand?.RaiseCanExecuteChanged();
        SaveCsvCommand?.RaiseCanExecuteChanged();
      }
    }

    private string _promptFilePath;
    public string PromptFilePath => _promptFilePath ?? (_promptFilePath = Path.Combine(_bootDataFolder, PromptFileName));

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

    public bool IsPromptEditingEnabled => !string.IsNullOrEmpty(PromptFilePath);

    public bool CanApply => !string.IsNullOrWhiteSpace(CsvContent);

    public void LoadCsvContent()
    {
      try
      {
        string path = Path.Combine(_bootDataFolder, PrimitivesListFileName);
        CsvContent = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
      }
      catch (Exception ex)
      {
        CsvContent = "# Ошибка загрузки: " + ex.Message;
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

    private bool CanExecuteSaveCsv(object _) => !string.IsNullOrWhiteSpace(CsvContent);

    private bool CanExecuteSaveInsertText(object _) => !string.IsNullOrWhiteSpace(PromptInsertText);

    private void ExecuteSaveInsertText(object _)
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

    private void ExecuteCreatePrompt(object _)
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

    private void ExecuteSaveCsv(object _)
    {
      try
      {
        Directory.CreateDirectory(_bootDataFolder);
        string path = Path.Combine(_bootDataFolder, PrimitivesListFileName);
        File.WriteAllText(path, CsvContent, Encoding.UTF8);
        MessageBox.Show("Файл успешно сохранён.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteValidateCsv(object _)
    {
      if (string.IsNullOrWhiteSpace(CsvContent))
      {
        MessageBox.Show("Текст пуст.", "Проверка формата", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      var lines = CsvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      int valid = 0, invalid = 0;
      foreach (var line in lines)
      {
        var t = line.Trim();
        if (string.IsNullOrWhiteSpace(t) || t.StartsWith("#")) continue;
        var parts = t.Split('|');
        if (parts.Length >= 5) valid++; else invalid++;
      }
      string msg = valid > 0
          ? $"Корректных строк: {valid}. Некорректных: {invalid}."
          : "Нет корректных строк. Ожидается формат: Состояние|Стили|Трехсложный паттерн|Тон|Настроение";
      MessageBox.Show(msg, "Проверка формата", MessageBoxButton.OK, invalid > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private bool CanExecuteSavePrompt(object _) => !string.IsNullOrWhiteSpace(PromptContent);

    private void ExecuteSavePrompt(object _)
    {
      try
      {
        Directory.CreateDirectory(_bootDataFolder);
        File.WriteAllText(PromptFilePath, PromptContent, Encoding.UTF8);
        MessageBox.Show("Промпт сохранён.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка сохранения промпта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ExecuteApply(object _)
    {
      if (string.IsNullOrWhiteSpace(CsvContent))
      {
        MessageBox.Show("Введите текст примитивов во вкладке «Текст примитивов».", "Нет данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      if (AppGlobalState.EvolutionStage != 2)
      {
        MessageBox.Show("Создание базовых примитивов по шаблону разрешено только на стадии 2.", "Неверная стадия", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      IsBusy = true;
      try
      {
        var result = await System.Threading.Tasks.Task.Run(() => _loader.LoadFromContent(CsvContent.Trim())).ConfigureAwait(true);
        MessageBox.Show(result.ToSummaryString(), "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        CloseAction?.Invoke(true);
      }
      catch (InvalidOperationException ex)
      {
        IsBusy = false;
        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
      catch (ArgumentException ex)
      {
        IsBusy = false;
        MessageBox.Show(ex.Message, "Ошибка формата", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
      catch (Exception ex)
      {
        IsBusy = false;
        MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
