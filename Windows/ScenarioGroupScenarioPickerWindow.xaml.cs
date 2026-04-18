using AIStudio.ViewModels.Research;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Windows
{
  public partial class ScenarioGroupScenarioPickerWindow : Window
  {
    public ScenarioGroupScenarioPickerWindow()
    {
      InitializeComponent();
      DataContext = new ScenarioGroupScenarioPickerViewModel();
    }

    /// <summary>Идентификаторы выбранных сценариев в порядке выделения в списке (после нажатия «Выбрать» или двойного щелчка).</summary>
    public IReadOnlyList<int> SelectedScenarioIds { get; private set; }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
      var ids = CollectSelectedIdsInDisplayOrder();
      if (ids.Count == 0)
      {
        MessageBox.Show("Выделите в списке хотя бы один сценарий.", "Выбор", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      SelectedScenarioIds = ids;
      DialogResult = true;
    }

    private void PickerGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var row = PickerGrid.SelectedItem as ScenarioGroupScenarioPickerRow;
      if (row == null)
        return;
      SelectedScenarioIds = new List<int> { row.Id };
      DialogResult = true;
      e.Handled = true;
    }

    /// <summary>Сохраняет порядок строк таблицы (сверху вниз), а не порядок выделения мышью.</summary>
    private List<int> CollectSelectedIdsInDisplayOrder()
    {
      var selected = new HashSet<int>(
          PickerGrid.SelectedItems.Cast<ScenarioGroupScenarioPickerRow>().Select(r => r.Id));
      if (selected.Count == 0)
        return new List<int>();
      return PickerGrid.Items.Cast<ScenarioGroupScenarioPickerRow>()
          .Where(r => selected.Contains(r.Id))
          .Select(r => r.Id)
          .ToList();
    }
  }
}
