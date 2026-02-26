using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AIStudio.ViewModels;
using ISIDA.Reflexes;

namespace AIStudio.Dialogs
{
  public partial class ReflexLoadDialog : Window
  {
    public ReflexLoadDialogViewModel ViewModel { get; }

    public ReflexLoadDialog(string bootDataFolder, GeneticReflexFileLoader loader)
    {
      InitializeComponent();
      ViewModel = new ReflexLoadDialogViewModel(bootDataFolder, loader ?? GeneticReflexFileLoader.Instance);
      DataContext = ViewModel;
      ViewModel.CloseAction = (result) =>
      {
        DialogResult = result;
        Close();
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
    }
  }

  public class ReflexLoadDialogViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly string _bootDataFolder;
    private readonly GeneticReflexFileLoader _loader;

    public Action<bool> CloseAction { get; set; }

    private bool _isBusy;
    public bool IsBusy
    {
      get => _isBusy;
      set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
    }

    public RelayCommand SaveCsvCommand { get; }
    public RelayCommand ValidateCsvCommand { get; }
    public RelayCommand SavePromptCommand { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ReflexLoadDialogViewModel(string bootDataFolder, GeneticReflexFileLoader loader)
    {
      _bootDataFolder = bootDataFolder ?? throw new ArgumentNullException(nameof(bootDataFolder));
      _loader = loader ?? throw new ArgumentNullException(nameof(loader));

      SaveCsvCommand = new RelayCommand(ExecuteSaveCsv, CanExecuteSaveCsv);
      ValidateCsvCommand = new RelayCommand(ExecuteValidateCsv);
      SavePromptCommand = new RelayCommand(ExecuteSavePrompt, CanExecuteSavePrompt);
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
    public string PromptFilePath => _promptFilePath ?? (_promptFilePath = Path.Combine(_bootDataFolder, "prompt_reflex_generate.txt"));

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

    private bool CanExecuteSaveCsv(object _) => !string.IsNullOrWhiteSpace(CsvContent);

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
        if (parts.Length >= 4) valid++; else invalid++;
      }
      string msg = valid > 0
          ? $"Корректных строк: {valid}. Некорректных: {invalid}."
          : $"Нет корректных строк. Ожидается формат: Состояние|Стили|Триггер|Действие|Цепочка";
      MessageBox.Show(msg, "Проверка формата", MessageBoxButton.OK, invalid > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private bool CanExecuteSavePrompt(object _) => !string.IsNullOrWhiteSpace(PromptContent) && File.Exists(PromptFilePath);

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
      IsBusy = true;
      try
      {
        int count = await System.Threading.Tasks.Task.Run(() => _loader.LoadFromContent(CsvContent.Trim())).ConfigureAwait(true);
        MessageBox.Show($"Создано рефлексов/цепочек по строкам: {count}.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        CloseAction?.Invoke(true);
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
