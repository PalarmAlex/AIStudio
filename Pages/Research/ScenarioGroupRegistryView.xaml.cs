using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.Common;
using AIStudio.ViewModels.Research;

namespace AIStudio.Pages.Research
{
  public partial class ScenarioGroupRegistryView : UserControl
  {
    public ScenarioGroupRegistryView()
    {
      InitializeComponent();
    }

    private void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ScenarioGroupRegistryViewModel vm)
        vm.EditCommand.Execute(null);
    }

    private void DataGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete)
        return;
      var dg = sender as DataGrid;
      var vm = DataContext as ScenarioGroupRegistryViewModel;
      if (dg == null || vm == null)
        return;
      var headers = new List<ScenarioGroupHeader>();
      foreach (var item in dg.SelectedItems)
      {
        if (item is ScenarioGroupHeader h)
          headers.Add(h);
      }
      if (headers.Count == 0)
        return;
      if (vm.TryDeleteSelected(headers))
        e.Handled = true;
    }
  }
}
