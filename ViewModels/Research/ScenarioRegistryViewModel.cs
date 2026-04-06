using AIStudio;
using AIStudio.Common;
using AIStudio.Pages.Research;
using AIStudio.Windows;
using ISIDA.Actions;
using ISIDA.Scenarios;
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
using Microsoft.Win32;

namespace AIStudio.ViewModels.Research
{
  public sealed class ScenarioRegistryViewModel : INotifyPropertyChanged
  {
    private readonly InfluenceActionSystem _influenceActions;
    private readonly OperatorScenarioRunner _runner;
    private readonly Action<ScenarioEditorViewModel> _openEditorEmbedded;
    private readonly Func<ScenarioDocument, string, ScenarioEditorViewModel, bool> _tryStartScenario;
    private readonly Func<bool> _isScenarioRunSessionBusy;
    private readonly Action _requestStopScenarioSession;
    private readonly Func<bool> _canStopScenarioSession;

    private ScenarioHeader _selected;
    private readonly List<ScenarioHeader> _registryAll = new List<ScenarioHeader>();
    private string _filterIdText = "";
    private string _filterTitleText = "";
    private string _filterStageText = "";

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

    public string FilterStageText
    {
      get => _filterStageText;
      set { if (_filterStageText == value) return; _filterStageText = value; OnPropertyChanged(); }
    }

    public ScenarioRegistryViewModel(
        InfluenceActionSystem influenceActions,
        OperatorScenarioRunner runner,
        Action<ScenarioEditorViewModel> openEditorEmbedded = null,
        Func<ScenarioDocument, string, ScenarioEditorViewModel, bool> tryStartScenario = null,
        Func<bool> isScenarioRunSessionBusy = null,
        Action requestStopScenarioSession = null,
        Func<bool> canStopScenarioSession = null)
    {
      _influenceActions = influenceActions ?? throw new ArgumentNullException(nameof(influenceActions));
      _runner = runner ?? throw new ArgumentNullException(nameof(runner));
      _openEditorEmbedded = openEditorEmbedded;
      _tryStartScenario = tryStartScenario ?? throw new ArgumentNullException(nameof(tryStartScenario));
      _isScenarioRunSessionBusy = isScenarioRunSessionBusy ?? (() => runner.IsRunning);
      _requestStopScenarioSession = requestStopScenarioSession ?? (() => _runner.StopUser());
      _canStopScenarioSession = canStopScenarioSession ?? (() => _runner.IsRunning);

      Items = new ObservableCollection<ScenarioHeader>();
      RefreshCommand = new RelayCommand(_ => Refresh());
      NewCommand = new RelayCommand(_ => OpenEditor(null, true), _ => !_isScenarioRunSessionBusy());
      EditCommand = new RelayCommand(_ => OpenEditor(Selected, false), _ => Selected != null && !_isScenarioRunSessionBusy());
      LaunchCommand = new RelayCommand(_ => Launch(), _ => Selected != null && !_isScenarioRunSessionBusy());
      DuplicateCommand = new RelayCommand(_ => Duplicate(), _ => Selected != null && !_isScenarioRunSessionBusy());
      ImportCommand = new RelayCommand(_ => Import(), _ => !_isScenarioRunSessionBusy());
      StopScenarioCommand = new RelayCommand(_ => _requestStopScenarioSession(), _ => _canStopScenarioSession());
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());

      _runner.RunningStateChanged += () =>
      {
        Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal,
            new Action(() => CommandManager.InvalidateRequerySuggested()));
      };

      Refresh();
    }

    public ObservableCollection<ScenarioHeader> Items { get; }

    public ScenarioHeader Selected
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

    public void Refresh()
    {
      _registryAll.Clear();
      foreach (var h in ScenarioStorage.LoadRegistry())
        _registryAll.Add(h);
      ApplyFilters();
    }

    private void ApplyFilters()
    {
      var idF = (FilterIdText ?? "").Trim();
      var titleF = (FilterTitleText ?? "").Trim();
      var stageF = (FilterStageText ?? "").Trim();
      IEnumerable<ScenarioHeader> q = _registryAll;
      if (idF.Length > 0)
        q = q.Where(h => h.Id.ToString(CultureInfo.InvariantCulture).IndexOf(idF, StringComparison.OrdinalIgnoreCase) >= 0);
      if (titleF.Length > 0)
        q = q.Where(h => (h.Title ?? "").IndexOf(titleF, StringComparison.OrdinalIgnoreCase) >= 0);
      if (stageF.Length > 0)
      {
        if (int.TryParse(stageF, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stageNum))
          q = q.Where(h => h.PreRunTargetStage == stageNum);
        else
          q = q.Where(h => (h.PreRunStageNumberDisplay ?? "").IndexOf(stageF, StringComparison.OrdinalIgnoreCase) >= 0
              || h.PreRunTargetStage.ToString(CultureInfo.InvariantCulture).IndexOf(stageF, StringComparison.OrdinalIgnoreCase) >= 0);
      }
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
      FilterStageText = "";
      OnPropertyChanged(nameof(FilterIdText));
      OnPropertyChanged(nameof(FilterTitleText));
      OnPropertyChanged(nameof(FilterStageText));
      ApplyFilters();
    }

    private void OpenEditor(ScenarioHeader header, bool isNew)
    {
      if (_isScenarioRunSessionBusy())
      {
        MessageBox.Show("Дождитесь завершения сценария или подготовки к запуску.", "Сценарий", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      ScenarioDocument doc;
      if (isNew)
      {
        doc = new ScenarioDocument
        {
          Header = new ScenarioHeader { Id = 0, Title = "Новый сценарий", DateText = DateTime.Now.ToString("yyyy-MM-dd") }
        };
      }
      else
      {
        try
        {
          doc = ScenarioStorage.LoadScenario(header.Id);
          doc.Header.Title = header.Title;
          doc.Header.Description = header.Description;
          doc.Header.DateText = header.DateText;
          ScenarioPulseSchedule.EnsureSequentialStepIndices(doc.Lines);
        }
        catch (Exception ex)
        {
          MessageBox.Show("Не удалось загрузить сценарий: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }
      }

      OpenEditorWithViewModel(CreateEditorViewModel(doc));
    }

    private ScenarioEditorViewModel CreateEditorViewModel(ScenarioDocument doc)
    {
      return new ScenarioEditorViewModel(
          _influenceActions,
          doc,
          _tryStartScenario);
    }

    private void OpenEditorWithViewModel(ScenarioEditorViewModel vm)
    {
      if (_openEditorEmbedded != null)
      {
        _openEditorEmbedded(vm);
        return;
      }

      var w = new ScenarioEditorWindow { DataContext = vm };
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

      ScenarioDocument doc;
      try
      {
        doc = ScenarioStorage.LoadScenario(Selected.Id);
        doc.Header.Title = Selected.Title;
        doc.Header.Description = Selected.Description;
        doc.Header.DateText = Selected.DateText;
        ScenarioPulseSchedule.EnsureSequentialStepIndices(doc.Lines);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не удалось загрузить сценарий: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      _tryStartScenario(doc, AppConfig.ScenarioReportsFolderPath, null);
    }

    public bool TryDeleteSelected(IReadOnlyList<ScenarioHeader> headers)
    {
      if (headers == null || headers.Count == 0)
        return false;
      if (_isScenarioRunSessionBusy())
      {
        MessageBox.Show("Дождитесь завершения сценария или подготовки к запуску.", "Сценарий", MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
      }

      var ids = new HashSet<int>(headers.Select(h => h.Id));
      string confirm = ids.Count == 1
          ? $"Удалить сценарий «{headers[0].Title}» (ID={headers[0].Id})?"
          : $"Удалить выбранные сценарии ({ids.Count} шт.)?";
      if (MessageBox.Show(confirm, "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        return true;

      foreach (var id in ids)
        ScenarioStorage.DeleteScenarioFiles(id);
      var reg = ScenarioStorage.LoadRegistry().Where(h => !ids.Contains(h.Id)).ToList();
      var (ok, msg) = ScenarioStorage.SaveRegistry(reg);
      if (!ok)
        MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      Refresh();
      return true;
    }

    private void Duplicate()
    {
      if (Selected == null)
        return;
      try
      {
        var doc = ScenarioStorage.LoadScenario(Selected.Id);
        ScenarioPulseSchedule.EnsureSequentialStepIndices(doc.Lines);
        doc.Header.Id = ScenarioStorage.NextScenarioId();
        doc.Header.Title = (Selected.Title ?? "Сценарий") + "_copy1";

        var (okLines, errLines) = ScenarioStorage.SaveScenarioLines(doc);
        if (!okLines)
        {
          MessageBox.Show(errLines, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        var reg = ScenarioStorage.LoadRegistry();
        reg.Add(new ScenarioHeader
        {
          Id = doc.Header.Id,
          Title = doc.Header.Title,
          Description = doc.Header.Description,
          DateText = doc.Header.DateText,
          PreRunTargetStage = doc.Header.PreRunTargetStage
        });
        var (ok, msg) = ScenarioStorage.SaveRegistry(reg);
        if (!ok)
          MessageBox.Show(msg, "Ошибка реестра", MessageBoxButton.OK, MessageBoxImage.Error);
        Refresh();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Копирование", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void Import()
    {
      var dlg = new OpenFileDialog { Filter = "Сценарий (*.dat;*.txt)|*.dat;*.txt|Все файлы|*.*" };
      if (dlg.ShowDialog() != true)
        return;
      try
      {
        var lines = File.ReadAllLines(dlg.FileName, Encoding.UTF8);
        var doc = ParseImported(lines);
        if (doc == null)
        {
          MessageBox.Show("Не удалось разобрать файл.", "Импорт", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }
        ScenarioPulseSchedule.EnsureSequentialStepIndices(doc.Lines);
        var verr = OperatorScenarioValidator.ValidateDocument(doc, _influenceActions);
        if (verr != null)
        {
          MessageBox.Show(verr, "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }
        OpenEditorWithViewModel(CreateEditorViewModel(doc));
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Импорт", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private static ScenarioDocument ParseImported(string[] lines)
    {
      var doc = new ScenarioDocument
      {
        Header = new ScenarioHeader { Title = "Импорт", DateText = DateTime.Now.ToString("yyyy-MM-dd") }
      };
      int formatVersion = 4;
      foreach (var line in lines)
      {
        var t = line?.Trim();
        if (t != null && t.StartsWith("# SCENARIO_LINES_FORMAT|", StringComparison.Ordinal))
        {
          var fv = t.Substring("# SCENARIO_LINES_FORMAT|".Length).Trim();
          int.TryParse(fv, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedV);
          if (parsedV > 0)
            formatVersion = parsedV;
        }
      }

      foreach (var line in lines)
      {
        var t = line?.Trim();
        if (string.IsNullOrEmpty(t) || t.StartsWith("#"))
        {
          if (t != null && t.StartsWith("# SCENARIO_META|", StringComparison.Ordinal))
          {
            var meta = t.Substring("# SCENARIO_META|".Length).Split('|');
            ScenarioStorage.ApplyScenarioMeta(doc.Header, meta, formatVersion);
          }
          continue;
        }
        if (ScenarioStorage.TryParseScenarioLine(t, out var row))
          doc.Lines.Add(row);
      }
      return doc.Lines.Count > 0 ? doc : null;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
