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
using ISIDA.Reflexes;

namespace AIStudio.Dialogs
{
  public partial class ConditionedReflexLoadDialog : Window
  {
    public ConditionedReflexLoadDialogViewModel ViewModel { get; }

    public ConditionedReflexLoadDialog(string bootDataFolder)
    {
      InitializeComponent();
      var loader = new ConditionedReflexFileLoader(bootDataFolder ?? throw new ArgumentNullException(nameof(bootDataFolder)));
      ViewModel = new ConditionedReflexLoadDialogViewModel(bootDataFolder, loader);
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

  public class ConditionedReflexLoadDialogViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly string _bootDataFolder;
    private readonly ConditionedReflexFileLoader _loader;

    public Action<bool> CloseAction { get; set; }
    public Action SwitchToPromptTabAction { get; set; }

    private const string InsertTextFileName = "conditioned_reflex_prompt_insert.txt";
    private static readonly string DefaultInsertTextTemplate = @"Сгенерируй строки условных рефлексов в формате (строго одна строка — одна запись, поля разделены только одним символом |, внутри ячеек символ | не используй):
Состояние|Стили|Триггер безусловного рефлекса|Новый триггер условного рефлекса|Тон|Настроение

Правила:
- Разделитель полей — только вертикальная черта |. В тексте ячеек (состояние, стили, триггер, фраза, тон, настроение) символ | запрещён.
- Состояние: строго одно из трёх — Плохо, Норма, Хорошо
- Стили: комбинации из [stileCombination], через + (например: Расслабление+Игра)
- Триггер безусловного рефлекса: строго одно воздействие из списка [InfluenceActionList]
- Новый триггер: короткая фраза с пульта без символа | (поощрение, команда и т.п.)
- Тон: строго одно из списка — Вялый, Нормальный, Повышенный (без изменений и синонимов)
- Настроение: строго одно из списка — Нормальное, Хорошее, Плохое, Игривое, Учитель, Агрессивное, Защитное, Протест (без изменений и синонимов)

Примеры (копируй формат точно):
Норма|Расслабление+Игра|Поощрить|молодец|Нормальный|Хорошее
Хорошо|Расслабление|Погладить|хороший мальчик|Нормальный|Игривое

Сгенерируй примерно [ReflexGenLinesStage1PerState] строк на каждое состояние.";

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

    public ConditionedReflexLoadDialogViewModel(string bootDataFolder, ConditionedReflexFileLoader loader)
    {
      _bootDataFolder = bootDataFolder ?? throw new ArgumentNullException(nameof(bootDataFolder));
      _loader = loader ?? throw new ArgumentNullException(nameof(loader));

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
    public string PromptFilePath => _promptFilePath ?? (_promptFilePath = _loader.GetPromptFilePath());

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
        string path = _loader.GetGenerateListFilePath();
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
        string path = _loader.GetPromptFilePath();
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
        string path = Path.Combine(_bootDataFolder, InsertTextFileName);
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
        string path = Path.Combine(_bootDataFolder, InsertTextFileName);
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
        File.WriteAllText(_loader.GetGenerateListFilePath(), CsvContent, Encoding.UTF8);
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
        // Допускается 4 поля (без тона и настроения) или 6 полей (с тоном и настроением)
        if (parts.Length >= 4) valid++; else invalid++;
      }
      string msg = valid > 0
          ? $"Корректных строк: {valid}. Некорректных: {invalid}."
          : "Нет корректных строк. Ожидается формат: Состояние|Стили|Триггер безусловного рефлекса|Новый триггер [|Тон|Настроение]";
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
        MessageBox.Show("Введите текст рефлексов во вкладке «Текст рефлексов».", "Нет данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      if (AppGlobalState.EvolutionStage != 1)
      {
        MessageBox.Show("Генерация условных рефлексов по шаблону разрешена только в стадии 1.", "Неверная стадия", MessageBoxButton.OK, MessageBoxImage.Warning);
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
