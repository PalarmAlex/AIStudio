using AIStudio.Common;
using AIStudio.ViewModels;
using ISIDA.Scenarios;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AIStudio.ViewModels.Research
{
  /// <summary>Строка списка выбора сценария для группы: ID, название, подпись стадии как в редакторе сценария.</summary>
  public sealed class ScenarioGroupScenarioPickerRow
  {
    public ScenarioGroupScenarioPickerRow(int id, string title, string stageCaption)
    {
      Id = id;
      Title = title ?? "";
      StageCaption = stageCaption ?? "";
    }

    public int Id { get; }
    public string Title { get; }
    public string StageCaption { get; }
  }

  /// <summary>Фильтруемый список сценариев для выбора в составе группы.</summary>
  public sealed class ScenarioGroupScenarioPickerViewModel : INotifyPropertyChanged
  {
    private readonly List<ScenarioHeader> _registryAll = new List<ScenarioHeader>();
    private string _filterIdText = "";
    private string _filterTitleText = "";
    private string _filterStageText = "";

    public ScenarioGroupScenarioPickerViewModel()
    {
      Items = new ObservableCollection<ScenarioGroupScenarioPickerRow>();
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      Reload();
    }

    public ObservableCollection<ScenarioGroupScenarioPickerRow> Items { get; }

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

    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public void Reload()
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
          q = q.Where(h => (StageCaptionFor(h) ?? "").IndexOf(stageF, StringComparison.OrdinalIgnoreCase) >= 0
              || h.PreRunTargetStage.ToString(CultureInfo.InvariantCulture).IndexOf(stageF, StringComparison.OrdinalIgnoreCase) >= 0
              || (h.PreRunStageNumberDisplay ?? "").IndexOf(stageF, StringComparison.OrdinalIgnoreCase) >= 0);
      }

      Items.Clear();
      foreach (var h in q.OrderBy(x => x.Id))
        Items.Add(new ScenarioGroupScenarioPickerRow(h.Id, h.Title, StageCaptionFor(h)));
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

    /// <summary>Подпись стадии перед запуском в том же стиле, что пункты комбобокса в <see cref="ScenarioEditorView"/>.</summary>
    public static string StageCaptionFor(ScenarioHeader h)
    {
      int s = h.PreRunTargetStage;
      if (s == -1)
        return "Не менять стадию";
      if (s >= 0 && s <= 5)
        return $"{s}: {EvolutionStageItem.GetDescription(s)}";
      return s.ToString(CultureInfo.InvariantCulture);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
}
