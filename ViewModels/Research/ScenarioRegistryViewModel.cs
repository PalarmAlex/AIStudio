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
    private readonly OperatorScenarioEngine _scenarioEngine;
    private readonly Action<ScenarioEditorViewModel> _openEditorEmbedded;
    private readonly Func<ScenarioDocument, string, bool> _tryStartScenario;

    private ScenarioHeader _selected;
    private readonly List<ScenarioHeader> _registryAll = new List<ScenarioHeader>();
    private string _filterIdText = "";
    private string _filterTitleText = "";
    private string _filterGroupText = "";

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

    public string FilterGroupText
    {
      get => _filterGroupText;
      set { if (_filterGroupText == value) return; _filterGroupText = value; OnPropertyChanged(); }
    }

    public ScenarioRegistryViewModel(
        InfluenceActionSystem influenceActions,
        OperatorScenarioRunner runner,
        OperatorScenarioEngine scenarioEngine,
        Action<ScenarioEditorViewModel> openEditorEmbedded = null,
        Func<ScenarioDocument, string, bool> tryStartScenario = null)
    {
      _influenceActions = influenceActions ?? throw new ArgumentNullException(nameof(influenceActions));
      _runner = runner ?? throw new ArgumentNullException(nameof(runner));
      _scenarioEngine = scenarioEngine ?? throw new ArgumentNullException(nameof(scenarioEngine));
      _openEditorEmbedded = openEditorEmbedded;
      _tryStartScenario = tryStartScenario ?? throw new ArgumentNullException(nameof(tryStartScenario));

      Items = new ObservableCollection<ScenarioHeader>();
      RefreshCommand = new RelayCommand(_ => Refresh());
      NewCommand = new RelayCommand(_ => OpenEditor(null, true), _ => !_runner.IsRunning);
      EditCommand = new RelayCommand(_ => OpenEditor(Selected, false), _ => Selected != null && !_runner.IsRunning);
      LaunchCommand = new RelayCommand(_ => Launch(), _ => Selected != null && !_runner.IsRunning);
      DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null && !_runner.IsRunning);
      DuplicateCommand = new RelayCommand(_ => Duplicate(), _ => Selected != null && !_runner.IsRunning);
      ImportCommand = new RelayCommand(_ => Import(), _ => !_runner.IsRunning);
      StopScenarioCommand = new RelayCommand(_ => _runner.StopUser(), _ => _runner.IsRunning);
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
    public ICommand DeleteCommand { get; }
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
      var groupF = (FilterGroupText ?? "").Trim();
      IEnumerable<ScenarioHeader> q = _registryAll;
      if (idF.Length > 0)
        q = q.Where(h => h.Id.ToString(CultureInfo.InvariantCulture).IndexOf(idF, StringComparison.OrdinalIgnoreCase) >= 0);
      if (titleF.Length > 0)
        q = q.Where(h => (h.Title ?? "").IndexOf(titleF, StringComparison.OrdinalIgnoreCase) >= 0);
      if (groupF.Length > 0)
        q = q.Where(h => h.GroupNumber.ToString(CultureInfo.InvariantCulture).IndexOf(groupF, StringComparison.OrdinalIgnoreCase) >= 0);
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
      FilterGroupText = "";
      OnPropertyChanged(nameof(FilterIdText));
      OnPropertyChanged(nameof(FilterTitleText));
      OnPropertyChanged(nameof(FilterGroupText));
      ApplyFilters();
    }

    private void OpenEditor(ScenarioHeader header, bool isNew)
    {
      if (_runner.IsRunning)
      {
        MessageBox.Show("Дождитесь завершения сценария.", "Сценарий", MessageBoxButton.OK, MessageBoxImage.Information);
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

      OpenEditorWithViewModel(CreateEditorViewModel(doc, isNew));
    }

    private ScenarioEditorViewModel CreateEditorViewModel(ScenarioDocument doc, bool isNew)
    {
      return new ScenarioEditorViewModel(
          _influenceActions,
          _scenarioEngine,
          doc,
          isNew,
          _tryStartScenario,
          () => _runner.IsRunning);
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

      _tryStartScenario(doc, AppConfig.ScenarioReportsFolderPath);
    }

    private void Delete()
    {
      if (Selected == null)
        return;
      if (MessageBox.Show($"Удалить сценарий «{Selected.Title}» (ID={Selected.Id})?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;

      ScenarioStorage.DeleteScenarioFiles(Selected.Id);
      var reg = ScenarioStorage.LoadRegistry().Where(h => h.Id != Selected.Id).ToList();
      var (ok, msg) = ScenarioStorage.SaveRegistry(reg);
      if (!ok)
        MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      Refresh();
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
          GroupNumber = doc.Header.GroupNumber,
          SortOrderInGroup = doc.Header.SortOrderInGroup
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
        OpenEditorWithViewModel(CreateEditorViewModel(doc, true));
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
      foreach (var line in lines)
      {
        var t = line?.Trim();
        if (string.IsNullOrEmpty(t) || t.StartsWith("#"))
        {
          if (t != null && t.StartsWith("# SCENARIO_META|", StringComparison.Ordinal))
          {
            var meta = t.Substring("# SCENARIO_META|".Length).Split('|');
            if (meta.Length >= 3)
            {
              doc.Header.Title = ScenarioStorage.Unescape(meta[0]);
              doc.Header.Description = ScenarioStorage.Unescape(meta[1]);
              doc.Header.DateText = ScenarioStorage.Unescape(meta[2]);
            }
            if (meta.Length >= 6)
              doc.Header.InitialHomeostasisValues = "";
            else if (meta.Length >= 5)
              doc.Header.InitialHomeostasisValues = ScenarioStorage.Unescape(meta[4]);
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
