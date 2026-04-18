using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.Common;
using AIStudio.ViewModels.Research;
using AIStudio.Windows;

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

    private void ScenarioCell_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left || e.ClickCount != 2)
        return;
      var fe = sender as FrameworkElement;
      if (fe == null)
        return;
      var row = fe.DataContext as ScenarioGroupMemberRow;
      if (row == null)
        return;
      var vm = GroupEditorRoot.DataContext as ScenarioGroupEditorViewModel;
      if (vm == null)
        return;
      e.Handled = true;
      var owner = Window.GetWindow(this);
      var dlg = new ScenarioGroupScenarioPickerWindow
      {
        Owner = owner,
        InitialSelectedScenarioIds = vm.GetScenarioIdsForPickerPreselection()
      };
      if (dlg.ShowDialog() != true || dlg.SelectedScenarioIds == null || dlg.SelectedScenarioIds.Count == 0)
        return;
      vm.ApplyPickedScenarios(row, dlg.SelectedScenarioIds);
    }
  }
}
