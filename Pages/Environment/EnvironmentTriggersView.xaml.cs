using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Pages.SymbiontEnv
{
  public partial class EnvironmentTriggersView : UserControl
  {
    public EnvironmentTriggersView()
    {
      InitializeComponent();
    }

    private EnvironmentTriggersViewModel Vm => DataContext as EnvironmentTriggersViewModel;

    private void HomeostasisDeltas_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!Vm.IsEditingEnabled)
      {
        MessageBox.Show(
            Vm.PulseWarningMessage,
            "Редактирование недоступно",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;
        return;
      }

      Vm.EditHomeostasisDeltas(Window.GetWindow(this));
      e.Handled = true;
    }
  }
}
