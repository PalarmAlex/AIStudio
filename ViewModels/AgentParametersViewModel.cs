using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using AIStudio.Common;
using ISIDA.Gomeostas;

namespace AIStudio.ViewModels
{
  public class AgentParametersViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;

    public ObservableCollection<GomeostasSystem.ParameterData> Parameters { get; }
        = new ObservableCollection<GomeostasSystem.ParameterData>();

    public AgentParametersViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas;
      LoadParameters();
    }

    private void LoadParameters()
    {
      var parameters = _gomeostas.GetAllParameters();

      Parameters.Clear();
      foreach (var param in parameters)
      {
        Parameters.Add(param);
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}