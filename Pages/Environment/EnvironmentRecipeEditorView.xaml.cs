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

    private void PickAdaptiveAction_Click(object sender, RoutedEventArgs e)
    {
      Vm?.PickAdaptiveAction(Window.GetWindow(this));
    }
  }
}
