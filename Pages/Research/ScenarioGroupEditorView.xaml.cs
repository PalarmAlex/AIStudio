using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private void MembersDataGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete)
        return;
      if (Keyboard.FocusedElement is TextBox)
        return;
      if (!(DataContext is ScenarioGroupEditorViewModel vm))
        return;
      if (!vm.RemoveMemberCommand.CanExecute(null))
        return;
      vm.RemoveMemberCommand.Execute(null);
      e.Handled = true;
    }
  }
}
