using System.Windows.Controls;
using AIStudio.ViewModels.Understanding;

namespace AIStudio.Pages.Understanding
{
  public partial class MentalEpisodicTreeView : UserControl
  {
    public MentalEpisodicTreeView()
    {
      InitializeComponent();
    }

    private void MentalTree_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
      if (DataContext is MentalEpisodicTreeViewModel vm)
        vm.SelectedNode = e.NewValue as MentalEpisodicTreeItem;
    }
  }
}
