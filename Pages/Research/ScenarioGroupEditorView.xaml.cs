using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels.Research;

namespace AIStudio.Pages.Research
{
  public partial class ScenarioGroupEditorView : UserControl
  {
    public ScenarioGroupEditorView()
    {
      InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(DataContext is ScenarioGroupEditorViewModel vm))
        return;
      if (!vm.TryCancelWithPrompt())
        return;
      if (vm.CloseAction != null)
        vm.CloseAction();
      else
        Window.GetWindow(this)?.Close();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => CloseButton_Click(sender, e);
  }
}
