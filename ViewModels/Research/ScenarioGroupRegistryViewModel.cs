using AIStudio;
using AIStudio.Common;
using AIStudio.ViewModels;
using AIStudio.Windows;
using ISIDA.Scenarios;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AIStudio.ViewModels.Research
{
  public sealed class ScenarioGroupRegistryViewModel : INotifyPropertyChanged
  {
    private readonly OperatorScenarioRunner _runner;
    private readonly Func<ScenarioGroupDocument, string, bool> _tryStartGroup;
    private readonly Action<ScenarioGroupEditorViewModel> _openEditorEmbedded;
    private readonly Func<string> _getLaunchPrecheckError;
    private readonly Func<bool> _isScenarioRunSessionBusy;
    private readonly Action _requestStopScenarioSession;
    private readonly Func<bool> _canStopScenarioSession;

    private ScenarioGroupHeader _selected;
    private readonly List<ScenarioGroupHeader> _registryAll = new List<ScenarioGroupHeader>();
    private string _filterIdText = "";
    private string _filterTitleText = "";
    private string _reportOutputFolder = "";

    public ScenarioGroupRegistryViewModel(
        OperatorScenarioRunner runner,
        Func<ScenarioGroupDocument, string, bool> tryStartGroup,
        Action<ScenarioGroupEditorViewModel> openEditorEmbedded = null,
        Func<string> getLaunchPrecheckError = null,
        Func<bool> isScenarioRunSessionBusy = null,
        Action requestStopScenarioSession = null,
        Func<bool> canStopScenarioSession = null)
    {
      _runner = runner ?? throw new ArgumentNullException(nameof(runner));
      _tryStartGroup = tryStartGroup ?? throw new ArgumentNullException(nameof(tryStartGroup));
      _openEditorEmbedded = openEditorEmbedded;
      _getLaunchPrecheckError = getLaunchPrecheckError;
      _isScenarioRunSessionBusy = isScenarioRunSessionBusy ?? (() => runner.IsRunning);
      _requestStopScenarioSession = requestStopScenarioSession ?? (() => _runner.StopUser());
      _canStopScenarioSession = canStopScenarioSession ?? (() => _runner.IsRunning);

      _reportOutputFolder = AppConfig.ScenarioReportsFolderPath ?? "";

      Items = new ObservableCollection<ScenarioGroupHeader>();
      RefreshCommand = new RelayCommand(_ => Refresh());
      NewCommand = new RelayCommand(_ => NewGroup(), _ => !_isScenarioRunSessionBusy());
      EditCommand = new RelayCommand(_ => Edit(), _ => Selected != null && !_isScenarioRunSessionBusy());
      LaunchCommand = new RelayCommand(_ => Launch(), _ => Selected != null && !_isScenarioRunSessionBusy());
      DuplicateCommand = new RelayCommand(_ => Duplicate(), _ => Selected != null && !_isScenarioRunSessionBusy());
      ImportCommand = new RelayCommand(_ => Import(), _ => !_isScenarioRunSessionBusy());
      StopScenarioCommand = new RelayCommand(_ => _requestStopScenarioSession(), _ => _canStopScenarioSession());
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      BrowseReportFolderCommand = new RelayCommand(_ => BrowseReportFolder());

      _runner.RunningStateChanged += () =>
      {
        Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal,
            new Action(() => CommandManager.InvalidateRequerySuggested()));
      };

      Refresh();
    }

    public string FilterIdText
    {
      get => _filterIdText;
      set { if (_filterIdText == value) return; _filterIdText = value; OnPropertyChanged(); }
    }

    public string FilterTitleText
    {
      get => _filterTitleText;
      set { if (_filterTitleText == value) return; _filterTitleText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ScenarioGroupHeader> Items { get; }

    public ScenarioGroupHeader Selected
    {
      get => _selected;
      set
      {
        _selected = value;
        OnPropertyChanged();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand LaunchCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand StopScenarioCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand BrowseReportFolderCommand { get; }

    public string ReportOutputFolder
    {
      get => _reportOutputFolder;
      set
      {
        if (_reportOutputFolder == value) return;
        _reportOutputFolder = value ?? "";
        OnPropertyChanged();
      }
    }

    private void BrowseReportFolder()
    {
      var dialog = new VistaFolderBrowserDialog
      {
        Description = "Каталог для сохранения HTML-отчёта группы",
        UseDescriptionForTitle = true,
        SelectedPath = Directory.Exists(ReportOutputFolder) ? ReportOutputFolder : ""
      };
      if (dialog.ShowDialog() == true)
        ReportOutputFolder = dialog.SelectedPath;
    }

    public void Refresh()
    {
      _registryAll.Clear();
      foreach (var h in ScenarioGroupStorage.LoadGroupRegistry())
        _registryAll.Add(h);
      ApplyFilters();
    }

    private void ApplyFilters()
    {
      var idF = (FilterIdText ?? "").Trim();
      var titleF = (FilterTitleText ?? "").Trim();
      IEnumerable<ScenarioGroupHeader> q = _registryAll;
      if (idF.Length > 0)
        q = q.Where(h => h.Id.ToString(CultureInfo.InvariantCulture).IndexOf(idF, StringComparison.OrdinalIgnoreCase) >= 0);
      if (titleF.Length > 0)
        q = q.Where(h => (h.Title ?? "").IndexOf(titleF, StringComparison.OrdinalIgnoreCase) >= 0);
      var prevId = Selected?.Id;
      Items.Clear();
      foreach (var h in q.OrderBy(x => x.Id))
        Items.Add(h);
      Selected = prevId.HasValue ? Items.FirstOrDefault(x => x.Id == prevId.Value) : Items.FirstOrDefault();
    }

    private void ResetFilters()
    {
      FilterIdText = "";
      FilterTitleText = "";
      OnPropertyChanged(nameof(FilterIdText));
      OnPropertyChanged(nameof(FilterTitleText));
      ApplyFilters();
    }

    private void NewGroup()
    {
      if (_isScenarioRunSessionBusy())
      {
        MessageBox.Show("Дождитесь завершения сценария или подготовки к запуску.", "Группа", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      ScenarioGroupStorage.EnsureFolder();
      var doc = new ScenarioGroupDocument
      {
        Id = ScenarioGroupStorage.NextGroupId(),
        Title = "Новая группа",
        DateText = DateTime.Now.ToString("yyyy-MM-dd"),
        RunPulseTimingCoefficient = 1
      };
      OpenEditor(doc);
    }

    public void Edit()
    {
      if (Selected == null)
        return;
      try
      {
        var doc = ScenarioGroupStorage.LoadGroup(Selected.Id);
        doc.Title = Selected.Title;
        doc.Description = Selected.Description;
        doc.DateText = Selected.DateText;
        OpenEditor(doc);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не удалось загрузить группу: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void OpenEditor(ScenarioGroupDocument doc)
    {
      var vm = new ScenarioGroupEditorViewModel(doc);
      if (_openEditorEmbedded != null)
      {
        _openEditorEmbedded(vm);
        return;
      }
      var w = new ScenarioGroupEditorWindow { DataContext = vm };
      if (Application.Current?.MainWindow is Window owner)
        w.Owner = owner;
      vm.RequestClose += (_, saved) =>
      {
        w.DialogResult = saved;
        w.Close();
      };
      w.ShowDialog();
      Refresh();
    }

    private void Launch()
    {
      if (Selected == null)
        return;
      try
      {
        var groupDoc = ScenarioGroupStorage.LoadGroup(Selected.Id);
        groupDoc.Title = Selected.Title;
        groupDoc.Description = Selected.Description;
        groupDoc.DateText = Selected.DateText;

        if (groupDoc.Members == null || groupDoc.Members.Count == 0)
        {
          MessageBox.Show("В группе нет сценариев для запуска.", "Группа", MessageBoxButton.OK, MessageBoxImage.Information);
          return;
        }

        if (_getLaunchPrecheckError != null)
        {
          var preErr = _getLaunchPrecheckError();
          if (!string.IsNullOrEmpty(preErr))
          {
            MessageBox.Show(preErr, "Группа сценариев", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
          }
        }

        var ordered = groupDoc.Members.OrderBy(m => m.SortOrderInGroup).ThenBy(m => m.ScenarioId).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("Будут запущены сценарии в таком порядке (коэфф. пульсации группы: ")
            .Append(groupDoc.RunPulseTimingCoefficient.ToString(CultureInfo.InvariantCulture)).AppendLine("):");
        foreach (var m in ordered)
        {
          sb.Append("  ID ").Append(m.ScenarioId.ToString(CultureInfo.InvariantCulture))
              .Append(", стадия ").Append(ScenarioGroupDocument.FormatPreRunStageShort(m.PreRunTargetStage))
              .Append(", очистка ").Append(m.PreRunClearAgentData ? "да" : "нет")
              .Append(", норма ").Append(m.PreRunNormalHomeostasisState ? "да" : "нет")
              .Append(", набл. ").Append(m.ScenarioObservationMode ? "да" : "нет")
              .Append(", авт.зап. ").AppendLine(m.ScenarioAuthoritativeRecording ? "да" : "нет");
        }
        sb.AppendLine().AppendLine("Продолжить?");

        if (MessageBox.Show(sb.ToString(), "Подтверждение группового запуска",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
          return;

        var folder = string.IsNullOrWhiteSpace(ReportOutputFolder)
            ? AppConfig.ScenarioReportsFolderPath
            : ReportOutputFolder.Trim();
        _tryStartGroup(groupDoc, folder);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Запуск группы", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void Duplicate()
    {
      if (Selected == null)
        return;
      try
      {
        var doc = ScenarioGroupStorage.LoadGroup(Selected.Id);
        doc.Id = ScenarioGroupStorage.NextGroupId();
        doc.Title = (Selected.Title ?? "Группа") + "_copy1";

        var reg = ScenarioGroupStorage.LoadGroupRegistry();
        reg.Add(new ScenarioGroupHeader
        {
          Id = doc.Id,
          Title = doc.Title,
          Description = doc.Description,
          DateText = doc.DateText
        });
        var (okReg, errReg) = ScenarioGroupStorage.SaveGroupRegistry(reg);
        if (!okReg)
        {
          MessageBox.Show(errReg, "Ошибка реестра", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }
        var (okG, errG) = ScenarioGroupStorage.SaveGroup(doc);
        if (!okG)
          MessageBox.Show(errG, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        Refresh();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Копирование", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void Import()
    {
      var dlg = new OpenFileDialog { Filter = "Группа сценариев (*.dat;*.txt)|*.dat;*.txt|Все файлы|*.*" };
      if (dlg.ShowDialog() != true)
        return;
      try
      {
        var lines = File.ReadAllLines(dlg.FileName, Encoding.UTF8);
        var doc = ScenarioGroupStorage.ParseGroupFromLines(lines, ScenarioGroupStorage.NextGroupId());
        if (doc.Members.Count == 0 && string.IsNullOrWhiteSpace(doc.Title))
        {
          MessageBox.Show("Не удалось разобрать файл группы.", "Импорт", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }
        doc.Id = ScenarioGroupStorage.NextGroupId();
        if (string.IsNullOrWhiteSpace(doc.Title))
          doc.Title = "Импорт группы";
        OpenEditor(doc);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Импорт", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
}
