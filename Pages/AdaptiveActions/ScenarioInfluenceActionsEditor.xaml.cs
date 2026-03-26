using ISIDA.Actions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class ScenarioInfluenceActionsEditor : Window
  {
    public List<int> SelectedActionIds { get; private set; }

    private readonly List<int> _initialIds;

    public ScenarioInfluenceActionsEditor(string title,
        IEnumerable<InfluenceActionSystem.GomeostasisInfluenceAction> allActions,
        IList<int> currentIds)
    {
      InitializeComponent();
      Title = title;
      if (HeaderTitle != null)
        HeaderTitle.Text = title;
      var cur = currentIds ?? new List<int>();
      _initialIds = cur.OrderBy(x => x).ToList();
      SelectedActionIds = new List<int>(cur);

      var items = allActions
          .OrderBy(a => a.Id)
          .Select(a => new InfluenceActionSelectionItem
          {
            Id = a.Id,
            Name = a.Name ?? "",
            IsSelected = cur.Contains(a.Id)
          })
          .ToList();

      ActionsList.ItemsSource = items;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      SelectedActionIds = ((IEnumerable<InfluenceActionSelectionItem>)ActionsList.ItemsSource)
          .Where(x => x.IsSelected)
          .Select(x => x.Id)
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
        Close();
        e.Handled = true;
      }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      if (DialogResult == true)
        return;
      if (!HasSelectionChanged())
        return;
      var r = MessageBox.Show(
          "Закрыть без сохранения выбранных воздействий?",
          Title,
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);
      if (r != MessageBoxResult.Yes)
        e.Cancel = true;
    }

    private bool HasSelectionChanged()
    {
      var cur = ((IEnumerable<InfluenceActionSelectionItem>)ActionsList.ItemsSource)
          .Where(x => x.IsSelected)
          .Select(x => x.Id)
          .OrderBy(x => x)
          .ToList();
      return !cur.SequenceEqual(_initialIds);
    }
  }

  internal sealed class InfluenceActionSelectionItem
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsSelected { get; set; }
  }
}
