using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.ViewModels.Research;
using ISIDA.Scenarios;

namespace AIStudio.Pages.Research
{
  public partial class ScenarioRegistryView : UserControl
  {
    public ScenarioRegistryView()
    {
      InitializeComponent();
    }

    private void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ScenarioRegistryViewModel vm)
        vm.EditCommand.Execute(null);
    }

    private void DataGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete)
        return;
      var dg = sender as DataGrid;
      var vm = DataContext as ScenarioRegistryViewModel;
      if (dg == null || vm == null)
        return;
      var headers = new List<ScenarioHeader>();
      foreach (var item in dg.SelectedItems)
      {
        if (item is ScenarioHeader h)
          headers.Add(h);
      }
      if (headers.Count == 0)
        return;
      if (vm.TryDeleteSelected(headers))
        e.Handled = true;
    }
  }
}
