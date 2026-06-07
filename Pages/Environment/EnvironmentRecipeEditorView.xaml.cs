using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Pages.SymbiontEnv
{
  /// <summary>
  /// Детальный редактор рецепта среды.
  /// </summary>
  public partial class EnvironmentRecipeEditorView : UserControl
  {
    /// <summary>
    /// Создаёт представление.
    /// </summary>
    public EnvironmentRecipeEditorView()
    {
      InitializeComponent();
    }

    private EnvironmentRecipeEditorViewModel Vm => DataContext as EnvironmentRecipeEditorViewModel;

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
      Vm?.CloseAction?.Invoke();
    }

    private void PickRecommendedTriggers_Click(object sender, RoutedEventArgs e)
    {
      Vm?.PickRecommendedTriggers(Window.GetWindow(this));
    }

    private void PickRecipeId_Click(object sender, RoutedEventArgs e)
    {
      Vm?.PickRecipeId(Window.GetWindow(this));
    }

    private void PickAdaptiveAction_Click(object sender, RoutedEventArgs e)
    {
      Vm?.PickAdaptiveAction(Window.GetWindow(this));
    }

    private void StepsGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      if (Vm == null)
        return;
      EnvironmentRecipeStepRow row = Vm.CreateNewStep();
      e.NewItem = row;
      Vm.SelectedStep = row;
    }

    private void StepTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (Vm == null || e.AddedItems.Count == 0)
        return;
      Vm.SyncSelectedStepSchema();
    }

    private void InsertPlaceholder_Click(object sender, RoutedEventArgs e)
    {
      if (sender is FrameworkElement element && element.Tag is EnvironmentRecipeStepParameterField field)
        Vm?.InsertTemplatePlaceholder(Window.GetWindow(this), field);
    }

    private void PickPropertyName_Click(object sender, RoutedEventArgs e)
    {
      if (sender is FrameworkElement element && element.Tag is EnvironmentRecipeStepParameterField field)
        Vm?.PickPropertyName(Window.GetWindow(this), field);
    }
  }
}
