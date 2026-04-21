using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AIStudio.Common;
using ISIDA.Gomeostas;
using ISIDA.Research;

namespace AIStudio.ViewModels.Research
{
  public sealed class HomeostasisHarnessViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private ResearchHarnessPipeMethodInfo _selectedMethod;
    private string _pipeLinesText = "";
    private string _statusMessage =
        "Сценарии хранятся в подпапке Scenarios каталога прогонов (по harness_id). Выберите имя в списке — текст подгрузится из файла.";
    private string _lastReportPath;
    private string _selectedSavedScenario;
    private string _scenarioSaveName = "";
    private bool _suspendScenarioSelectionReaction;

    public HomeostasisHarnessViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      foreach (var m in ResearchHarnessPipeMethodInfo.All)
        Methods.Add(m);

      _selectedMethod = Methods.FirstOrDefault();
      RefreshSavedScenarioNamesAndLoadInitial();

      LoadSampleCommand = new RelayCommand(_ => LoadSampleForSelectedMethod());
      RunCommand = new RelayCommand(_ => Run(), _ => SelectedMethod != null && !string.IsNullOrWhiteSpace(PipeLinesText));
      OpenRunsRootCommand = new RelayCommand(_ => OpenRunsRoot());
      OpenLastReportCommand = new RelayCommand(_ => OpenLastReport(), _ => !string.IsNullOrWhiteSpace(_lastReportPath) && File.Exists(_lastReportPath));
      SaveScenarioCommand = new RelayCommand(_ => SaveScenario(), _ => SelectedMethod != null && !string.IsNullOrWhiteSpace(ScenarioSaveName));
      RefreshScenarioListCommand = new RelayCommand(_ => RefreshSavedScenarioNamesOnly());
      OpenScenariosFolderCommand = new RelayCommand(_ => OpenScenariosFolder(), _ => SelectedMethod != null);
      AutoGenerateScenarioCommand = new RelayCommand(_ => AutoGenerateScenario(), _ => SelectedMethod != null);
    }

    public ObservableCollection<ResearchHarnessPipeMethodInfo> Methods { get; } =
        new ObservableCollection<ResearchHarnessPipeMethodInfo>();

    public ObservableCollection<string> SavedScenarioNames { get; } = new ObservableCollection<string>();

    public ResearchHarnessPipeMethodInfo SelectedMethod
    {
      get => _selectedMethod;
      set
      {
        if (_selectedMethod == value) return;
        _selectedMethod = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(MethodCardText));
        OnPropertyChanged(nameof(PipeFormatHint));
        OnPropertyChanged(nameof(JsonSampleText));
        RefreshSavedScenarioNamesAndLoadInitial();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public string MethodCardText => SelectedMethod?.CardDescription ?? "";

    public string PipeFormatHint => SelectedMethod?.PipeFormatLine ?? "";

    /// <summary>Пример JSON с тем же <c>harness_id</c>, что у выбранного pipe-метода (для CLI / внешнего прогона).</summary>
    public string JsonSampleText =>
        SelectedMethod == null ? "" : ResearchHarnessSampleInputs.GetJson(SelectedMethod.HarnessId);

    /// <summary>Выбранное имя файла сценария из списка (без .txt); при смене — загрузка в поле строк.</summary>
    public string SelectedSavedScenario
    {
      get => _selectedSavedScenario;
      set
      {
        if (_selectedSavedScenario == value) return;
        _selectedSavedScenario = value;
        OnPropertyChanged();
        if (_suspendScenarioSelectionReaction)
          return;
        if (string.IsNullOrWhiteSpace(value))
          return;
        ScenarioSaveName = value;
        OnPropertyChanged(nameof(ScenarioSaveName));
        LoadScenarioFromDisk(value);
      }
    }

    /// <summary>Имя файла сценария для сохранения (без пути; расширение .txt добавится само).</summary>
    public string ScenarioSaveName
    {
      get => _scenarioSaveName;
      set
      {
        if (_scenarioSaveName == value) return;
        _scenarioSaveName = value;
        OnPropertyChanged();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public string PipeLinesText
    {
      get => _pipeLinesText;
      set
      {
        if (_pipeLinesText == value) return;
        _pipeLinesText = value;
        OnPropertyChanged();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public string StatusMessage
    {
      get => _statusMessage;
      private set
      {
        _statusMessage = value;
        OnPropertyChanged();
      }
    }

    public ICommand LoadSampleCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand OpenRunsRootCommand { get; }
    public ICommand OpenLastReportCommand { get; }
    public ICommand SaveScenarioCommand { get; }
    public ICommand RefreshScenarioListCommand { get; }
    public ICommand OpenScenariosFolderCommand { get; }
    public ICommand AutoGenerateScenarioCommand { get; }

    private void RefreshSavedScenarioNamesOnly()
    {
      if (SelectedMethod == null) return;
      _suspendScenarioSelectionReaction = true;
      try
      {
        SavedScenarioNames.Clear();
        foreach (var n in ResearchHarnessScenarioStore.ListScenarioNames(SelectedMethod.HarnessId))
          SavedScenarioNames.Add(n);
      }
      finally
      {
        _suspendScenarioSelectionReaction = false;
      }

      StatusMessage = $"Список сценариев обновлён ({SavedScenarioNames.Count} файлов).";
    }

    private void RefreshSavedScenarioNamesAndLoadInitial()
    {
      if (SelectedMethod == null)
      {
        SavedScenarioNames.Clear();
        return;
      }

      _suspendScenarioSelectionReaction = true;
      try
      {
        SavedScenarioNames.Clear();
        foreach (var n in ResearchHarnessScenarioStore.ListScenarioNames(SelectedMethod.HarnessId))
          SavedScenarioNames.Add(n);
        _selectedSavedScenario = null;
        OnPropertyChanged(nameof(SelectedSavedScenario));
      }
      finally
      {
        _suspendScenarioSelectionReaction = false;
      }

      if (SavedScenarioNames.Count > 0)
      {
        var first = SavedScenarioNames[0];
        _suspendScenarioSelectionReaction = true;
        try
        {
          _selectedSavedScenario = first;
          OnPropertyChanged(nameof(SelectedSavedScenario));
        }
        finally
        {
          _suspendScenarioSelectionReaction = false;
        }

        ScenarioSaveName = first;
        OnPropertyChanged(nameof(ScenarioSaveName));
        LoadScenarioFromDisk(first);
        StatusMessage = $"Загружен сценарий «{first}». Каталог: {ResearchHarnessScenarioStore.GetScenarioDirectory(SelectedMethod.HarnessId)}";
      }
      else
      {
        ScenarioSaveName = "";
        OnPropertyChanged(nameof(ScenarioSaveName));
        PipeLinesText = SelectedMethod.DefaultSampleText;
        StatusMessage = "Нет сохранённых сценариев для этого метода — подставлен встроенный пример. Сохраните под своим именем.";
      }
    }

    private void LoadScenarioFromDisk(string name)
    {
      if (SelectedMethod == null || string.IsNullOrWhiteSpace(name)) return;
      try
      {
        PipeLinesText = ResearchHarnessScenarioStore.Load(SelectedMethod.HarnessId, name);
        StatusMessage = $"Загружен файл «{name}.txt».";
      }
      catch (Exception ex)
      {
        StatusMessage = "Не удалось загрузить сценарий: " + ex.Message;
        MessageBox.Show(ex.Message, "Сценарий", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void SaveScenario()
    {
      if (SelectedMethod == null) return;
      var baseName = ResearchHarnessScenarioStore.NormalizeScenarioName(ScenarioSaveName);
      try
      {
        ResearchHarnessScenarioStore.ValidateScenarioName(baseName);
      }
      catch (ArgumentException ex)
      {
        MessageBox.Show(ex.Message, "Сохранение сценария", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      if (ResearchHarnessScenarioStore.Exists(SelectedMethod.HarnessId, baseName))
      {
        var r = MessageBox.Show(
            $"Файл «{baseName}.txt» уже есть. Перезаписать?",
            "Сохранение сценария",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes)
          return;
      }

      try
      {
        ResearchHarnessScenarioStore.Save(SelectedMethod.HarnessId, baseName, PipeLinesText ?? "");
        RefreshSavedScenarioNamesOnly();
        _suspendScenarioSelectionReaction = true;
        try
        {
          _selectedSavedScenario = baseName;
          OnPropertyChanged(nameof(SelectedSavedScenario));
        }
        finally
        {
          _suspendScenarioSelectionReaction = false;
        }

        ScenarioSaveName = baseName;
        OnPropertyChanged(nameof(ScenarioSaveName));
        StatusMessage = $"Сценарий сохранён: {ResearchHarnessScenarioStore.GetScenarioFilePath(SelectedMethod.HarnessId, baseName)}";
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Сохранение сценария", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void OpenScenariosFolder()
    {
      if (SelectedMethod == null) return;
      try
      {
        var dir = ResearchHarnessScenarioStore.GetScenarioDirectory(SelectedMethod.HarnessId);
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Каталог сценариев", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void LoadSampleForSelectedMethod()
    {
      if (SelectedMethod == null) return;
      PipeLinesText = SelectedMethod.DefaultSampleText;
      StatusMessage = "Подставлен встроенный пример строк для выбранного метода (не из файла).";
    }

    private void Run()
    {
      if (SelectedMethod == null)
      {
        MessageBox.Show("Выберите метод прогона.", "Прогон гомеостаза", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      var parse = ResearchHarnessPipeRunner.Parse(SelectedMethod, PipeLinesText);
      if (!parse.Success)
      {
        var msg = string.Join("\r\n", parse.BlockingErrors);
        StatusMessage =
            parse.BlockingErrors.Count == 1
                ? "Ошибка разбора сценария. Подробности — в открывшемся окне."
                : $"Ошибка разбора сценария ({parse.BlockingErrors.Count} замечаний). Подробности — в открывшемся окне.";
        ResearchHarnessScrollMessageWindow.Show("Ошибка разбора сценария", msg);
        return;
      }

      if (parse.Warnings.Count > 0)
      {
        var w = string.Join("\n", parse.Warnings);
        var r = MessageBox.Show(
            "Есть предупреждения по числам (округление и т.п.):\n\n" + w + "\n\nПродолжить прогон?",
            "Прогон гомеостаза",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (r != MessageBoxResult.Yes)
        {
          StatusMessage = "Прогон отменён из-за предупреждений.";
          return;
        }
      }

      try
      {
        Directory.CreateDirectory(ResearchHarnessPaths.RootFolder);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outDir = ResearchHarnessPaths.RunFolder(stamp);
        Directory.CreateDirectory(outDir);

        var run = ResearchHarnessPipeRunner.Execute(
            _gomeostas.Calculator,
            SelectedMethod,
            parse.Rows,
            outDir,
            PipeLinesText);

        if (!run.Success)
        {
          StatusMessage = run.ErrorMessage ?? "Ошибка выполнения.";
          MessageBox.Show(StatusMessage, "Прогон гомеостаза", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        _lastReportPath = run.ReportHtmlPath;
        StatusMessage =
            $"Готово. Строк: {run.RowCount}, расхождений (NO): {run.MismatchCount}, время: {run.ElapsedMs} мс.\nКаталог: {outDir}\nОткройте отчёт для просмотра таблицы.";
        CommandManager.InvalidateRequerySuggested();

        var open = MessageBox.Show(
            "Прогон завершён. Открыть HTML-отчёт?",
            "Прогон гомеостаза",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (open == MessageBoxResult.Yes)
          OpenLastReport();
      }
      catch (Exception ex)
      {
        StatusMessage = ex.Message;
        MessageBox.Show(ex.Message, "Прогон гомеостаза", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void OpenRunsRoot()
    {
      try
      {
        Directory.CreateDirectory(ResearchHarnessPaths.RootFolder);
        Process.Start(new ProcessStartInfo
        {
          FileName = ResearchHarnessPaths.RootFolder,
          UseShellExecute = true
        });
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Каталог", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void OpenLastReport()
    {
      try
      {
        if (string.IsNullOrWhiteSpace(_lastReportPath) || !File.Exists(_lastReportPath))
          return;
        Process.Start(new ProcessStartInfo { FileName = _lastReportPath, UseShellExecute = true });
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Отчёт", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void AutoGenerateScenario()
    {
      if (SelectedMethod == null) return;
      var calc = _gomeostas.Calculator;
      var block = ResearchHarnessPipeRunner.BuildAutoScenarioText(SelectedMethod, calc);

      if (!string.IsNullOrWhiteSpace(PipeLinesText))
      {
        var choice = MessageBox.Show(
            "В поле «Строки прогона» уже есть текст.\n\n" +
            "Да — заменить его целиком результатом автогенерации.\n" +
            "Нет — добавить автогенерацию в конец существующего текста.\n" +
            "Отмена — не менять поле.",
            "Автогенерация сценария",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        if (choice == MessageBoxResult.Cancel)
        {
          StatusMessage = "Автогенерация отменена: поле не изменено.";
          return;
        }

        if (choice == MessageBoxResult.Yes)
          PipeLinesText = block;
        else
          PipeLinesText = PipeLinesText.TrimEnd() + "\r\n\r\n" + block;
      }
      else
      {
        PipeLinesText = block;
      }

      var lines = block.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
      int dataLines = lines.Count(l =>
      {
        var t = l.Trim();
        return t.Length > 0 && !t.StartsWith("#", StringComparison.Ordinal);
      });
      StatusMessage =
          $"Автогенерация: {dataLines} строк данных. Сохранение в файл — только кнопкой «Сохранить сценарий».";
      CommandManager.InvalidateRequerySuggested();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
