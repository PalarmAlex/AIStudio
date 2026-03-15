using System.Windows.Controls;
using AIStudio.ViewModels.Understanding;

namespace AIStudio.Pages.Understanding
{
  public partial class ProblemTreeView : UserControl
  {
    public ProblemTreeView()
    {
      InitializeComponent();
    }

    private void ProblemTree_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
      if (DataContext is ProblemTreeViewModel vm)
        vm.SelectedNode = e.NewValue as ProblemTreeNodeItem;
    }
  }
}
