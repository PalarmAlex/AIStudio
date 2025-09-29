using ISIDA.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AIStudio.Pages
{
  public partial class AntagonistInfluenceEditor : Window
  {
    public class InfluenceItem
    {
      public int Id { get; set; }
      public string Name { get; set; }
      public bool IsSelected { get; set; }
      public string DisplayText => $"{Name} (ID: {Id})";
    }

    public IEnumerable<int> SelectedInfluenceIds =>
    _influenceItems.Where(x => x.IsSelected).Select(x => x.Id);

    private readonly List<InfluenceItem> _influenceItems;

    public AntagonistInfluenceEditor(string title,
        IEnumerable<InfluenceActionSystem.GomeostasisInfluenceAction> availableInfluences,
        IEnumerable<int> selectedStyleIds)
    {
      InitializeComponent();

      Title = title;

      var selectedIds = new HashSet<int>(selectedStyleIds);
      _influenceItems = availableInfluences
          .OrderBy(s => s.Id)
          .Select(s => new InfluenceItem
          {
            Id = s.Id,
            Name = s.Name,
            IsSelected = selectedIds.Contains(s.Id)
          })
          .ToList();

      InfluenceListBox.ItemsSource = _influenceItems;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close(); // Закрываем окно при нажатии Esc
        e.Handled = true;
      }
    }
  }
}
