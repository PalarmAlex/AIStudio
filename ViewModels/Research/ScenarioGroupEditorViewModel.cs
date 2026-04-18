using AIStudio.Common;
using AIStudio.ViewModels;
using ISIDA.Scenarios;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels.Research
{
  public sealed class ScenarioGroupEditorViewModel : INotifyPropertyChanged
  {
    private string _title = "";
    private string _description = "";
    private int _runPulseTimingCoefficient = 1;
    private ScenarioGroupReportFormat _reportFormat = ScenarioGroupReportFormat.Detailed;
    private ScenarioGroupMemberRow _selectedMember;
    private bool _hasUnsavedChanges;

    public ScenarioGroupEditorViewModel(ScenarioGroupDocument doc)
    {
      Document = doc ?? throw new ArgumentNullException(nameof(doc));
      _title = doc.Title ?? "";
      _description = doc.Description ?? "";
      _runPulseTimingCoefficient = doc.RunPulseTimingCoefficient <= 0 ? 1 : doc.RunPulseTimingCoefficient;
      _reportFormat = Enum.IsDefined(typeof(ScenarioGroupReportFormat), doc.ReportFormat)
          ? doc.ReportFormat
          : ScenarioGroupReportFormat.Detailed;

      ScenarioChoices = new ObservableCollection<ScenarioRegistryPickItem>(
          ScenarioStorage.LoadRegistry()
              .OrderBy(h => h.Id)
              .Select(h => new ScenarioRegistryPickItem(h.Id, h.Title ?? "")));

      PreRunStageChoices = new ObservableCollection<EvolutionStageItem>();
      PreRunStageChoices.Add(new EvolutionStageItem { StageNumber = -1, Description = "Не менять стадию" });
      for (int i = 0; i <= 5; i++)
        PreRunStageChoices.Add(new EvolutionStageItem
        {
          StageNumber = i,
          Description = $"{i}: {EvolutionStageItem.GetDescription(i)}"
        });

      Members = new ObservableCollection<ScenarioGroupMemberRow>();
      foreach (var m in doc.Members.OrderBy(x => x.SortOrderInGroup).ThenBy(x => x.ScenarioId))
      {
        var row = m.Clone();
        Members.Add(row);
        WireMember(row);
      }

      Members.CollectionChanged += Members_CollectionChanged;

      PulseTimingCoefficientChoices = new ObservableCollection<int> { 1, 5, 10, 20 };

      ReportFormatChoices = new ObservableCollection<ScenarioGroupReportFormatItem>
      {
        new ScenarioGroupReportFormatItem { Format = ScenarioGroupReportFormat.Detailed, Display = "Подробный" },
        new ScenarioGroupReportFormatItem { Format = ScenarioGroupReportFormat.Compact, Display = "Сокращенный" }
      };

      SaveCommand = new RelayCommand(_ => Save(invokeCloseAfterSuccess: true), _ => Members.Count > 0);
      AddMemberCommand = new RelayCommand(_ => AddMember());
      RemoveMemberCommand = new RelayCommand(_ => RemoveMember(), _ => SelectedMember != null);

      _hasUnsavedChanges = false;
    }

    public ScenarioGroupDocument Document { get; }

    public ObservableCollection<ScenarioGroupMemberRow> Members { get; }

    public ObservableCollection<int> PulseTimingCoefficientChoices { get; }

    public ObservableCollection<ScenarioGroupReportFormatItem> ReportFormatChoices { get; }

    public ObservableCollection<ScenarioRegistryPickItem> ScenarioChoices { get; }

    public ObservableCollection<EvolutionStageItem> PreRunStageChoices { get; }

    public string Title
    {
      get => _title;
      set
      {
        if (_title == value) return;
        _title = value;
        OnPropertyChanged();
        MarkDirty();
      }
    }

    public string Description
    {
      get => _description;
      set
      {
        if (_description == value) return;
        _description = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DescriptionDisplay));
        MarkDirty();
      }
    }

    private const int DescriptionDisplayLimit = 100;

    public string DescriptionDisplay
    {
      get
      {
        if (string.IsNullOrEmpty(_description))
          return "";
        var flat = _description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        return flat.Length <= DescriptionDisplayLimit
            ? flat
            : flat.Substring(0, DescriptionDisplayLimit) + "…";
      }
    }

    public int RunPulseTimingCoefficient
    {
      get => _runPulseTimingCoefficient;
      set
      {
        if (_runPulseTimingCoefficient == value) return;
        _runPulseTimingCoefficient = value;
        OnPropertyChanged();
        MarkDirty();
      }
    }

    public ScenarioGroupReportFormat ReportFormat
    {
      get => _reportFormat;
      set
      {
        if (_reportFormat == value) return;
        _reportFormat = value;
        OnPropertyChanged();
        MarkDirty();
      }
    }

    public ScenarioGroupMemberRow SelectedMember
    {
      get => _selectedMember;
      set { _selectedMember = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    public ICommand SaveCommand { get; }
    public ICommand AddMemberCommand { get; }
    public ICommand RemoveMemberCommand { get; }

    public event EventHandler<bool> RequestClose;

    public Action CloseAction { get; set; }

    private void Members_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      MarkDirty();
      if (e.NewItems != null)
        foreach (ScenarioGroupMemberRow m in e.NewItems)
          WireMember(m);
      if (e.OldItems != null)
        foreach (ScenarioGroupMemberRow m in e.OldItems)
          UnwireMember(m);
    }

    private void WireMember(ScenarioGroupMemberRow m)
    {
      m.PropertyChanged += Member_PropertyChanged;
      RefreshMemberSelectionToolTips(m);
    }

    private void UnwireMember(ScenarioGroupMemberRow m) =>
        m.PropertyChanged -= Member_PropertyChanged;

    private void Member_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      MarkDirty();
      if (sender is ScenarioGroupMemberRow row &&
          (e.PropertyName == nameof(ScenarioGroupMemberRow.ScenarioId) ||
           e.PropertyName == nameof(ScenarioGroupMemberRow.PreRunTargetStage)))
        RefreshMemberSelectionToolTips(row);
    }

    private void RefreshMemberSelectionToolTips(ScenarioGroupMemberRow row)
    {
      var scenario = row.ScenarioId > 0 ? ScenarioChoices.FirstOrDefault(s => s.Id == row.ScenarioId) : null;
      var stage = PreRunStageChoices.FirstOrDefault(s => s.StageNumber == row.PreRunTargetStage);
      string cell;
      string tip;
      if (row.ScenarioId <= 0)
      {
        cell = "(не выбран)";
        tip = "Двойной щелчок — выбрать сценарий из списка.";
      }
      else if (scenario == null)
      {
        cell = row.ScenarioId.ToString(CultureInfo.InvariantCulture);
        tip = "Сценарий не найден в реестре.";
      }
      else
      {
        cell = scenario.DisplayTitle ?? "";
        tip = scenario.FullTitle ?? "";
      }
      row.UpdateMemberPresentation(cell, tip, stage?.Description ?? "");
    }

    /// <summary>
    /// Все id сценариев из таблицы группы (по порядку строк), для предвыделения в окне выбора.
    /// Пустой список — в таблице ни одного выбранного сценария.
    /// </summary>
    public IReadOnlyList<int> GetScenarioIdsForPickerPreselection()
    {
      var list = new List<int>();
      foreach (var m in Members)
      {
        if (m.ScenarioId > 0)
          list.Add(m.ScenarioId);
      }
      return list;
    }

    /// <summary>Первый выбранный id подставляется в строку-якорь, остальные — в новые строки сразу под ней (копия параметров предзапуска с якоря).</summary>
    public void ApplyPickedScenarios(ScenarioGroupMemberRow anchor, IReadOnlyList<int> scenarioIds)
    {
      if (anchor == null || scenarioIds == null || scenarioIds.Count == 0)
        return;
      int idx = Members.IndexOf(anchor);
      if (idx < 0)
        return;

      anchor.ScenarioId = scenarioIds[0];
      for (int i = 1; i < scenarioIds.Count; i++)
      {
        var row = new ScenarioGroupMemberRow
        {
          SortOrderInGroup = anchor.SortOrderInGroup,
          ScenarioId = scenarioIds[i],
          PreRunTargetStage = anchor.PreRunTargetStage,
          PreRunClearAgentData = anchor.PreRunClearAgentData,
          PreRunNormalHomeostasisState = anchor.PreRunNormalHomeostasisState,
          ScenarioObservationMode = anchor.ScenarioObservationMode,
          ScenarioAuthoritativeRecording = anchor.ScenarioAuthoritativeRecording
        };
        Members.Insert(idx + i, row);
      }
      NormalizeMemberSortOrder();
      CommandManager.InvalidateRequerySuggested();
    }

    private void NormalizeMemberSortOrder()
    {
      int n = 1;
      foreach (var m in Members)
        m.SortOrderInGroup = n++;
    }

    private void MarkDirty() => _hasUnsavedChanges = true;

    public bool TryCancelWithPrompt()
    {
      if (!_hasUnsavedChanges)
        return true;
      var r = MessageBox.Show("Сохранить изменения перед закрытием?", "Редактор группы сценариев",
          MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
      if (r == MessageBoxResult.Cancel)
        return false;
      if (r == MessageBoxResult.Yes)
        return Save(invokeCloseAfterSuccess: false);
      return true;
    }

    private void AddMember()
    {
      int nextSort = Members.Count == 0 ? 1 : Members.Max(m => m.SortOrderInGroup) + 1;
      Members.Add(new ScenarioGroupMemberRow
      {
        SortOrderInGroup = nextSort,
        ScenarioId = 0,
        PreRunTargetStage = -1
      });
      CommandManager.InvalidateRequerySuggested();
    }

    private void RemoveMember()
    {
      if (SelectedMember != null && Members.Remove(SelectedMember))
        SelectedMember = Members.FirstOrDefault();
      CommandManager.InvalidateRequerySuggested();
    }

    /// <returns>true если сохранение прошло успешно.</returns>
    public bool Save(bool invokeCloseAfterSuccess)
    {
      if (Members.Count == 0)
      {
        MessageBox.Show("Добавьте хотя бы одну строку сценария.", "Группа", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      foreach (var m in Members)
      {
        if (m.ScenarioId <= 0)
        {
          MessageBox.Show("Укажите сценарий для каждой строки.", "Группа", MessageBoxButton.OK, MessageBoxImage.Warning);
          return false;
        }
        if (m.PreRunTargetStage < -1 || m.PreRunTargetStage > 5)
        {
          MessageBox.Show("Стадия перед запуском должна быть от −1 до 5.", "Группа", MessageBoxButton.OK, MessageBoxImage.Warning);
          return false;
        }
      }

      ScenarioGroupStorage.EnsureFolder();
      int coeff = RunPulseTimingCoefficient;
      if (coeff != 1 && coeff != 5 && coeff != 10 && coeff != 20 && coeff != 50 && coeff != 100)
        coeff = 1;

      var doc = new ScenarioGroupDocument
      {
        Id = Document.Id,
        Title = Title?.Trim() ?? "",
        Description = Description?.Trim() ?? "",
        RunPulseTimingCoefficient = coeff,
        ReportFormat = ReportFormat,
        Members = Members.Select(m => m.Clone()).ToList()
      };

      if (doc.Id <= 0)
        doc.Id = ScenarioGroupStorage.NextGroupId();

      var reg = ScenarioGroupStorage.LoadGroupRegistry();
      var existing = reg.FirstOrDefault(h => h.Id == doc.Id);
      if (existing != null)
        reg.Remove(existing);
      reg.Add(new ScenarioGroupHeader
      {
        Id = doc.Id,
        Title = doc.Title,
        Description = doc.Description
      });
      var (okReg, errReg) = ScenarioGroupStorage.SaveGroupRegistry(reg);
      if (!okReg)
      {
        MessageBox.Show(errReg, "Ошибка реестра", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      Document.Id = doc.Id;
      var (okG, errG) = ScenarioGroupStorage.SaveGroup(doc);
      if (!okG)
      {
        MessageBox.Show(errG, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      _hasUnsavedChanges = false;
      MessageBox.Show("Группа сохранена.", "Группа", MessageBoxButton.OK, MessageBoxImage.Information);
      if (invokeCloseAfterSuccess)
        RequestClose?.Invoke(this, true);
      return true;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
}
