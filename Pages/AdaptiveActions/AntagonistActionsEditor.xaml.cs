using ISIDA.Actions;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class AntagonistActionsEditor : Window
  {
    public List<int> SelectedActionIds { get; private set; }

    public AntagonistActionsEditor(string title,
        IEnumerable<AdaptiveActionsSystem.AdaptiveAction> allActions,
        List<int> currentAntagonists)
    {
      InitializeComponent();
      Title = title;
      SelectedActionIds = new List<int>(currentAntagonists);

      var items = allActions.Select(a => new ActionSelection
      {
        Id = a.Id,
        Name = a.Name,
        IsSelected = currentAntagonists.Contains(a.Id)
      }).ToList();

      ActionsList.ItemsSource = items;
    }
    private void OK_Click(object sender, RoutedEventArgs e)
    {
      SelectedActionIds = ((IEnumerable<dynamic>)ActionsList.ItemsSource)
          .Where(item => item.IsSelected)
          .Select(item => (int)item.Id)
          .ToList();

      DialogResult = true;
      Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
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
