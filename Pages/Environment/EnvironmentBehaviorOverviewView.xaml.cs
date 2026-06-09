using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Pages.SymbiontEnv
{
  public partial class EnvironmentBehaviorOverviewView : UserControl
  {
    public EnvironmentBehaviorOverviewView()
    {
      InitializeComponent();
    }

    private void ChainsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!(DataContext is EnvironmentBehaviorOverviewViewModel vm))
        return;
      if (!(sender is DataGrid grid) || !(grid.SelectedItem is EnvironmentBehaviorChainRow row))
        return;
      if (!string.IsNullOrWhiteSpace(row.RecipeId))
        vm.OpenRecipeCommand.Execute(row);
      else if (!string.IsNullOrWhiteSpace(row.TriggerId))
        vm.OpenTriggerCommand.Execute(row);
    }
  }
}
