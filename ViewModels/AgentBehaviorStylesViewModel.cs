using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ISIDA.Gomeostas;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.ViewModels
{
  public class AgentBehaviorStylesViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private ObservableCollection<BehaviorStyle> _allBehaviorStyles;
    private ObservableCollection<BehaviorStyle> _activeStyles;

    public ObservableCollection<BehaviorStyle> AllBehaviorStyles
    {
      get => _allBehaviorStyles;
      set
      {
        _allBehaviorStyles = value;
        OnPropertyChanged(nameof(AllBehaviorStyles));
      }
    }

    public ObservableCollection<BehaviorStyle> ActiveStyles
    {
      get => _activeStyles;
      set
      {
        _activeStyles = value;
        OnPropertyChanged(nameof(ActiveStyles));
      }
    }

    public AgentBehaviorStylesViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas;
      LoadBehaviorStyles();
    }

    public void LoadBehaviorStyles()
    {
      var agentInfo = _gomeostas.GetAgentState();

      // Инициализируем коллекции, если они null
      if (_allBehaviorStyles == null)
      {
        _allBehaviorStyles = new ObservableCollection<BehaviorStyle>();
        OnPropertyChanged(nameof(AllBehaviorStyles));
      }
      else
        _allBehaviorStyles.Clear();

      if (_activeStyles == null)
      {
        _activeStyles = new ObservableCollection<BehaviorStyle>();
        OnPropertyChanged(nameof(ActiveStyles));
      }
      else
        _activeStyles.Clear();

      // Заполняем коллекции данными
      if (agentInfo?.AllBehaviorStyles?.Values != null)
      {
        foreach (var style in agentInfo.AllBehaviorStyles.Values)
        {
          _allBehaviorStyles.Add(style);
        }
      }

      if (agentInfo?.ActiveStyles != null)
      {
        foreach (var style in agentInfo.ActiveStyles)
        {
          _activeStyles.Add(style);
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}