using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio;
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

      // Навигация через главное окно
      var mainWindow = Application.Current.MainWindow;
      if (mainWindow?.DataContext is MainViewModel mainVm)
      {
        if (!string.IsNullOrWhiteSpace(row.RecipeId))
        {
          // Открыть рецепт
          mainVm.NavigateToRecipe(row.RecipeId);
        }
        else if (!string.IsNullOrWhiteSpace(row.TriggerId))
        {
          // Открыть триггер
          mainVm.NavigateToTrigger(row.TriggerId);
        }
      }
    }
  }
}
