using AIStudio;
using AIStudio.Common;
using AIStudio.ViewModels;

using ISIDA.Actions;
using ISIDA.Gomeostas;
using ISIDA.Scenarios;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels.Research
{
  /// <summary>Строка редактора: один параметр гомеостаза, значение 0…100 для ползунка.</summary>
  public sealed class HomeostasisParamRow : INotifyPropertyChanged
  {
    private int _sliderValue;

    public HomeostasisParamRow(int paramId, string labelText, int sliderValue)
    {
      ParamId = paramId;
      LabelText = labelText ?? "";
      _sliderValue = sliderValue;
    }

    public int ParamId { get; }

    public string LabelText { get; }

    public int SliderValue
    {
      get => _sliderValue;
      set
      {
        int v = value;
        if (v < 0) v = 0;
        if (v > 100) v = 100;
        if (_sliderValue == v) return;
        _sliderValue = v;
        OnPropertyChanged(nameof(SliderValue));
        OnPropertyChanged(nameof(ValueDisplay));
      }
    }

    public string ValueDisplay => SliderValue.ToString(CultureInfo.InvariantCulture);

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }

  public sealed class ScenarioEditorViewModel : INotifyPropertyChanged
  {
    private static readonly SolidColorBrush PreviewBrushStateBad = CreateFrozenBrush(211, 47, 47);
    private static readonly SolidColorBrush PreviewBrushStateNormal = CreateFrozenBrush(204, 204, 0);
    private static readonly SolidColorBrush PreviewBrushStateWell = CreateFrozenBrush(46, 125, 50);

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
      var br = new SolidColorBrush(Color.FromRgb(r, g, b));
      br.Freeze();
      return br;
    }

    private readonly InfluenceActionSystem _influenceActions;
    private readonly OperatorScenarioEngine _scenarioEngine;
    private readonly bool _isNew;

    private string _title = "";
    private string _description = "";
    private string _dateText = "";
    private ScenarioLineRow _selectedLine;
    private string _previewStateValue = "";
    private Brush _previewStateValueBrush = Brushes.Black;
    private string _previewStylesValue = "";

    public ScenarioEditorViewModel(
        InfluenceActionSystem influenceActions,
        OperatorScenarioEngine scenarioEngine,
        ScenarioDocument doc,
        bool isNew)
    {
      _influenceActions = influenceActions ?? throw new ArgumentNullException(nameof(influenceActions));
      _scenarioEngine = scenarioEngine ?? throw new ArgumentNullException(nameof(scenarioEngine));
      _isNew = isNew;
      Document = doc ?? throw new ArgumentNullException(nameof(doc));

      _title = doc.Header.Title ?? "";
      _description = doc.Header.Description ?? "";
      _dateText = string.IsNullOrWhiteSpace(doc.Header.DateText)
          ? DateTime.Now.ToString("yyyy-MM-dd")
          : doc.Header.DateText;

      var saved = ScenarioHomeostasisValuesFormat.Parse(doc.Header.InitialHomeostasisValues ?? "");

      HomeostasisRows = new ObservableCollection<HomeostasisParamRow>();
      if (GomeostasSystem.IsInitialized)
      {
        foreach (var p in GomeostasSystem.Instance.GetAllParameters())
        {
          float raw = saved.TryGetValue(p.Id, out var sv)
              ? sv
              : GomeostasSystem.GetDefaultInitialValueForScenarioParameter(p);
          int iv = (int)Math.Round(raw);
          if (iv < 0) iv = 0;
          if (iv > 100) iv = 100;
          string label = $"{p.Name} ({p.NormaWell})";
          HomeostasisRows.Add(new HomeostasisParamRow(p.Id, label, iv));
        }
      }

      Lines = new ObservableCollection<ScenarioLineRow>();
      foreach (var l in doc.Lines.OrderBy(x => x.StepIndex > 0 ? x.StepIndex : int.MaxValue).ThenBy(x => x.PulseWithinScenario))
      {
        var row = l.Clone();
        Lines.Add(row);
        row.RefreshActionNames(_influenceActions);
      }

      _scenarioEngine.NormalizeSchedule(Lines);
      foreach (var l in Lines)
        l.RefreshActionNames(_influenceActions);

      AddLineCommand = new RelayCommand(_ => AddLine());
      RemoveLineCommand = new RelayCommand(_ => RemoveLine(), _ => SelectedLine != null);
      SaveCommand = new RelayCommand(_ => Save(requestCloseAfterSuccess: true));

      HasUnsavedChanges = false;

      RefreshHomeostasisPreview();
    }

    public ScenarioDocument Document { get; }
    public ObservableCollection<ScenarioLineRow> Lines { get; }
    public ObservableCollection<HomeostasisParamRow> HomeostasisRows { get; }

    public InfluenceActionSystem InfluenceActions => _influenceActions;

    public string Title
    {
      get => _title;
      set
      {
        if (_title == value) return;
        _title = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
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
        HasUnsavedChanges = true;
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
        HasUnsavedChanges = true;
      }
    }

    /// <summary>Текст состояния без подписи «Состояние:» (Плохо / Норма / Хорошо или пояснение).</summary>
    public string PreviewStateValue
    {
      get => _previewStateValue;
      private set
      {
        if (_previewStateValue == value) return;
        _previewStateValue = value ?? "";
        OnPropertyChanged(nameof(PreviewStateValue));
      }
    }

    /// <summary>Цвет только для значения состояния (Плохо — красный, Норма — жёлтый, Хорошо — зелёный).</summary>
    public Brush PreviewStateValueBrush
    {
      get => _previewStateValueBrush;
      private set
      {
        if (_previewStateValueBrush == value) return;
        _previewStateValueBrush = value ?? Brushes.Black;
        OnPropertyChanged(nameof(PreviewStateValueBrush));
      }
    }

    /// <summary>Текст комбинации стилей без подписи «Активные стили:» (подпись в XAML чёрная, значение — серое).</summary>
    public string PreviewStylesValue
    {
      get => _previewStylesValue;
      private set
      {
        if (_previewStylesValue == value) return;
        _previewStylesValue = value ?? "";
        OnPropertyChanged(nameof(PreviewStylesValue));
      }
    }

    public ScenarioLineRow SelectedLine
    {
      get => _selectedLine;
      set
      {
        _selectedLine = value;
        OnPropertyChanged();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveCommand { get; }

    public event EventHandler<bool> RequestClose;

    public bool HasUnsavedChanges { get; private set; }

    /// <summary>Вызывать после отпускания ползунка / потери фокуса / клавиш — пересчёт превью и отметка несохранённого.</summary>
    public void OnHomeostasisSliderCommitted()
    {
      HasUnsavedChanges = true;
      RefreshHomeostasisPreview();
    }

    public void RefreshHomeostasisPreview()
    {
      if (!GomeostasSystem.IsInitialized || HomeostasisRows.Count == 0)
      {
        if (!GomeostasSystem.IsInitialized)
        {
          PreviewStateValue = "Гомеостаз не инициализирован";
          PreviewStateValueBrush = Brushes.Black;
          PreviewStylesValue = "—";
        }
        else
        {
          PreviewStateValue = "—";
          PreviewStateValueBrush = Brushes.Black;
          PreviewStylesValue = "—";
        }
        return;
      }

      var dict = HomeostasisRows.ToDictionary(r => r.ParamId, r => (float)r.SliderValue);
      var (st, styles) = GomeostasSystem.Instance.PreviewScenarioHomeostasisForEditor(dict);
      PreviewStateValue = st;
      PreviewStateValueBrush = BrushForHomeostasisStateText(st);
      PreviewStylesValue = styles ?? "";
    }

    private static Brush BrushForHomeostasisStateText(string st)
    {
      if (string.IsNullOrEmpty(st) || st == "—")
        return Brushes.Black;
      if (st == "Плохо")
        return PreviewBrushStateBad;
      if (st == "Норма")
        return PreviewBrushStateNormal;
      if (st == "Хорошо")
        return PreviewBrushStateWell;
      return Brushes.Black;
    }

    public void RecomputePulseSchedule()
    {
      _scenarioEngine.NormalizeSchedule(Lines);
      HasUnsavedChanges = true;
    }

    public bool TryCancelWithPrompt()
    {
      if (!HasUnsavedChanges)
        return true;
      var r = MessageBox.Show("Сохранить изменения перед закрытием?", "Редактор сценария",
          MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
      if (r == MessageBoxResult.Cancel)
        return false;
      if (r == MessageBoxResult.Yes)
        return Save(requestCloseAfterSuccess: false);
      return true;
    }

    private void AddLine()
    {
      int nextStep = Lines.Count == 0 ? 1 : Lines.Max(l => l.StepIndex) + 1;
      var row = new ScenarioLineRow
      {
        StepIndex = nextStep,
        Kind = ScenarioLineKind.Pult,
        ToneId = 0,
        MoodId = 0
      };
      Lines.Add(row);
      row.RefreshActionNames(_influenceActions);
      _scenarioEngine.NormalizeSchedule(Lines);
      SelectedLine = row;
      HasUnsavedChanges = true;
    }

    public void MarkDirty()
    {
      HasUnsavedChanges = true;
    }

    private void RemoveLine()
    {
      if (SelectedLine == null) return;
      Lines.Remove(SelectedLine);
      _scenarioEngine.NormalizeSchedule(Lines);
      SelectedLine = Lines.FirstOrDefault();
      HasUnsavedChanges = true;
    }

    public bool Save(bool requestCloseAfterSuccess = true)
    {
      var doc = BuildDocument();
      var err = OperatorScenarioValidator.ValidateDocument(doc, _influenceActions);
      if (err != null)
      {
        MessageBox.Show(err, "Проверка сценария", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      ScenarioStorage.EnsureFolder();
      if (_isNew)
        doc.Header.Id = ScenarioStorage.NextScenarioId();

      doc.Header.Title = Title?.Trim() ?? "";
      doc.Header.Description = Description?.Trim() ?? "";
      doc.Header.DateText = DateText?.Trim() ?? "";

      var (okLines, errLines) = ScenarioStorage.SaveScenarioLines(doc);
      if (!okLines)
      {
        MessageBox.Show(errLines, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      var reg = ScenarioStorage.LoadRegistry();
      var existing = reg.FirstOrDefault(h => h.Id == doc.Header.Id);
      if (existing != null)
        reg.Remove(existing);
      reg.Add(new ScenarioHeader
      {
        Id = doc.Header.Id,
        Title = doc.Header.Title,
        Description = doc.Header.Description,
        DateText = doc.Header.DateText
      });

      var (okReg, errReg) = ScenarioStorage.SaveRegistry(reg);
      if (!okReg)
      {
        MessageBox.Show(errReg, "Ошибка сохранения реестра", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      HasUnsavedChanges = false;
      if (requestCloseAfterSuccess)
        RequestClose?.Invoke(this, true);
      return true;
    }

    public ScenarioDocument BuildDocument()
    {
      _scenarioEngine.NormalizeSchedule(Lines);
      var dict = HomeostasisRows.ToDictionary(r => r.ParamId, r => (float)r.SliderValue);
      var doc = new ScenarioDocument
      {
        Header = new ScenarioHeader
        {
          Id = Document.Header.Id,
          Title = Title?.Trim() ?? "",
          Description = Description?.Trim() ?? "",
          DateText = DateText?.Trim() ?? "",
          InitialHomeostasisValues = ScenarioHomeostasisValuesFormat.Serialize(dict)
        },
        Lines = Lines.Select(l => l.Clone()).ToList()
      };
      return doc;
    }

    public void MarkSaved()
    {
      HasUnsavedChanges = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
