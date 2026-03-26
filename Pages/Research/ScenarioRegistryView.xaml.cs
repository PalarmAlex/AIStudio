using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.ViewModels.Research;

namespace AIStudio.Pages.Research
{
  public partial class ScenarioRegistryView : UserControl
  {
    public ScenarioRegistryView()
    {
      InitializeComponent();
    }

    private void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ScenarioRegistryViewModel vm)
        vm.EditCommand.Execute(null);
    }
  }
}
