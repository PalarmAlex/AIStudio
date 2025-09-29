using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AIStudio.Common
{
  public abstract class AntagonistItem : INotifyPropertyChanged
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<int> AntagonistIds { get; set; } = new List<int>();

    private bool _isSelected;
    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        if (_isSelected != value)
        {
          _isSelected = value;
          OnPropertyChanged();
          OnSelectionChanged?.Invoke(this);
        }
      }
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
      get => _isEnabled;
      set
      {
        if (_isEnabled != value)
        {
          _isEnabled = value;
          OnPropertyChanged();
        }
      }
    }

    public event Action<AntagonistItem> OnSelectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
