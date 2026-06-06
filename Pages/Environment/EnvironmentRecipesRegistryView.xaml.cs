using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Pages.SymbiontEnv
{
  /// <summary>
  /// Реестр рецептов среды.
  /// </summary>
  public partial class EnvironmentRecipesRegistryView : UserControl
  {
    /// <summary>
    /// Создаёт представление.
    /// </summary>
    public EnvironmentRecipesRegistryView()
    {
      InitializeComponent();
    }

    private void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is EnvironmentRecipesRegistryViewModel vm)
        vm.EditCommand.Execute(null);
    }

    private void DataGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete)
        return;
      var dg = sender as DataGrid;
      var vm = DataContext as EnvironmentRecipesRegistryViewModel;
      if (dg == null || vm == null)
        return;
      var items = new List<EnvironmentRecipeListItem>();
      foreach (object item in dg.SelectedItems)
      {
        if (item is EnvironmentRecipeListItem row)
          items.Add(row);
      }
      if (items.Count == 0)
        return;
      if (vm.TryDeleteSelected(items))
        e.Handled = true;
    }
  }
}
