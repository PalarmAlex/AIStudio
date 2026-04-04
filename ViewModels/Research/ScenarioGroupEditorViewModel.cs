using AIStudio.Common;
using AIStudio.ViewModels;
using ISIDA.Scenarios;
using System;
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
    private string _dateText = "";
    private int _runPulseTimingCoefficient = 1;
    private ScenarioGroupMemberRow _selectedMember;
    private bool _hasUnsavedChanges;

    public ScenarioGroupEditorViewModel(ScenarioGroupDocument doc)
    {
      Document = doc ?? throw new ArgumentNullException(nameof(doc));
      _title = doc.Title ?? "";
      _description = doc.Description ?? "";
      _dateText = string.IsNullOrWhiteSpace(doc.DateText)
          ? DateTime.Now.ToString("yyyy-MM-dd")
          : doc.DateText;
      _runPulseTimingCoefficient = doc.RunPulseTimingCoefficient <= 0 ? 1 : doc.RunPulseTimingCoefficient;

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

      SaveCommand = new RelayCommand(_ => Save(invokeCloseAfterSuccess: true), _ => Members.Count > 0);
      AddMemberCommand = new RelayCommand(_ => AddMember());
      RemoveMemberCommand = new RelayCommand(_ => RemoveMember(), _ => SelectedMember != null);

      _hasUnsavedChanges = false;
    }

    public ScenarioGroupDocument Document { get; }

    public ObservableCollection<ScenarioGroupMemberRow> Members { get; }

    public ObservableCollection<int> PulseTimingCoefficientChoices { get; }

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

    public string DateText
    {
      get => _dateText;
      set
      {
        if (_dateText == value) return;
        _dateText = value;
        OnPropertyChanged();
        MarkDirty();
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
      var scenario = ScenarioChoices.FirstOrDefault(s => s.Id == row.ScenarioId);
      var stage = PreRunStageChoices.FirstOrDefault(s => s.StageNumber == row.PreRunTargetStage);
      row.UpdateSelectionToolTips(scenario?.FullTitle ?? "", stage?.Description ?? "");
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
      var defaultScenarioId = ScenarioChoices.FirstOrDefault()?.Id ?? 1;
      Members.Add(new ScenarioGroupMemberRow
      {
        SortOrderInGroup = nextSort,
        ScenarioId = defaultScenarioId,
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
        DateText = DateText?.Trim() ?? "",
        RunPulseTimingCoefficient = coeff,
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
        Description = doc.Description,
        DateText = doc.DateText
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
