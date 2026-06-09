using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Pages.SymbiontEnv
{
  public partial class EnvironmentRecipeEditorView : UserControl
  {
    public EnvironmentRecipeEditorView()
    {
      InitializeComponent();
      ShowTab(RecipeEditorTab.General);
    }

    private EnvironmentRecipeEditorViewModel Vm => DataContext as EnvironmentRecipeEditorViewModel;

    private void BackButton_Click(object sender, RoutedEventArgs e) => Vm?.CloseAction?.Invoke();

    private void PickRecommendedTriggers_Click(object sender, RoutedEventArgs e) =>
        Vm?.PickRecommendedTriggers(Window.GetWindow(this));

    private void PickRecipeId_Click(object sender, RoutedEventArgs e) =>
        Vm?.PickRecipeId(Window.GetWindow(this));

    private void PickAdaptiveAction_Click(object sender, RoutedEventArgs e) =>
        Vm?.PickAdaptiveAction(Window.GetWindow(this));

    private void TabGeneral_Click(object sender, RoutedEventArgs e) => ShowTab(RecipeEditorTab.General);

    private void TabSteps_Click(object sender, RoutedEventArgs e) => ShowTab(RecipeEditorTab.Steps);

    private void TabLinks_Click(object sender, RoutedEventArgs e) => ShowTab(RecipeEditorTab.Links);

    private void ShowTab(RecipeEditorTab tab)
    {
      if (Vm != null)
        Vm.SelectedTab = tab;
      GeneralPanel.Visibility = tab == RecipeEditorTab.General ? Visibility.Visible : Visibility.Collapsed;
      StepsPanel.Visibility = tab == RecipeEditorTab.Steps ? Visibility.Visible : Visibility.Collapsed;
      LinksPanel.Visibility = tab == RecipeEditorTab.Links ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StepsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
      e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    private void ModelField_TextChanged(object sender, TextChangedEventArgs e) => Vm?.OnModelFieldChanged();

    private void ModelField_Changed(object sender, RoutedEventArgs e) => Vm?.OnModelFieldChanged();
  }
}
