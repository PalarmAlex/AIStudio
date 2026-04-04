using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.ViewModels.Research;

namespace AIStudio.Pages.Research
{
  public partial class ScenarioGroupRegistryView : UserControl
  {
    public ScenarioGroupRegistryView()
    {
      InitializeComponent();
    }

    private void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ScenarioGroupRegistryViewModel vm)
        vm.EditCommand.Execute(null);
    }
  }
}
