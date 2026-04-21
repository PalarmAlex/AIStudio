using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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
    private string _selectedHarnessId = HomeostasisHarnessIds.HasCriticalParameterChanges;
    private string _inputJsonText;
    private string _statusMessage = "Укажите JSON и нажмите «Выполнить прогон». Результаты: …/Data/ResearchHarness/Runs/&lt;дата&gt;_&lt;время&gt;/";
    private string _lastRunFolder;
    private string _lastReportPath;

    public HomeostasisHarnessViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      foreach (var id in HomeostasisHarnessIds.All)
        HarnessIds.Add(id);
      _inputJsonText = ResearchHarnessSampleInputs.GetJson(HomeostasisHarnessIds.HasCriticalParameterChanges);

      LoadTemplateCommand = new RelayCommand(_ => LoadTemplate());
      RunCommand = new RelayCommand(_ => Run(), _ => !string.IsNullOrWhiteSpace(InputJsonText));
      OpenRunsRootCommand = new RelayCommand(_ => OpenRunsRoot());
      OpenLastReportCommand = new RelayCommand(_ => OpenLastReport(), _ => !string.IsNullOrWhiteSpace(_lastReportPath) && File.Exists(_lastReportPath));
    }

    public ObservableCollection<string> HarnessIds { get; } = new ObservableCollection<string>();

    public string SelectedHarnessId
    {
      get => _selectedHarnessId;
      set
      {
        if (_selectedHarnessId == value) return;
        _selectedHarnessId = value;
        OnPropertyChanged();
      }
    }

    public string InputJsonText
    {
      get => _inputJsonText;
      set
      {
        if (_inputJsonText == value) return;
        _inputJsonText = value;
        OnPropertyChanged();
      }
    }

    public string StatusMessage
    {
      get => _statusMessage;
      private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public ICommand LoadTemplateCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand OpenRunsRootCommand { get; }
    public ICommand OpenLastReportCommand { get; }

    private void LoadTemplate()
    {
      var id = string.IsNullOrWhiteSpace(SelectedHarnessId) ? HomeostasisHarnessIds.HasCriticalParameterChanges : SelectedHarnessId;
      InputJsonText = ResearchHarnessSampleInputs.GetJson(id);
      StatusMessage = "Подставлен шаблон для «" + id + "». При необходимости отредактируйте JSON.";
    }

    private void Run()
    {
      try
      {
        Directory.CreateDirectory(ResearchHarnessPaths.RootFolder);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outDir = ResearchHarnessPaths.RunFolder(stamp);
        Directory.CreateDirectory(outDir);
        var inputPath = Path.Combine(outDir, "input.json");
        File.WriteAllText(inputPath, InputJsonText ?? "", Encoding.UTF8);

        var result = HomeostasisHarnessRunner.Run(_gomeostas.Calculator, inputPath, outDir);
        if (!result.Success)
        {
          StatusMessage = "Ошибка: " + (result.ErrorMessage ?? "неизвестно");
          MessageBox.Show(StatusMessage, "Прогон гомеостаза", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        _lastRunFolder = outDir;
        var manifestPath = Path.Combine(outDir, "manifest.json");
        var jsonlPath = Path.Combine(outDir, "results.jsonl");
        _lastReportPath = Path.Combine(outDir, "report.html");
        ResearchHarnessReportHtmlBuilder.WriteReport(manifestPath, jsonlPath, _lastReportPath);

        StatusMessage = $"Готово. Строк: {result.Manifest.RowCount}, ошибок по кейсам: {result.Manifest.ErrorsCount}, время: {result.Manifest.ElapsedMs} мс.\nКаталог: {outDir}";
        CommandManager.InvalidateRequerySuggested();
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

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
