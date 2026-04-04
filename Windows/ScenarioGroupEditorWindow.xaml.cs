using System.Windows;
using AIStudio.ViewModels.Research;

namespace AIStudio.Windows
{
  public partial class ScenarioGroupEditorWindow : Window
  {
    public ScenarioGroupEditorWindow()
    {
      InitializeComponent();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
      if (DataContext is ScenarioGroupEditorViewModel vm && !vm.TryCancelWithPrompt())
        e.Cancel = true;
      base.OnClosing(e);
    }
  }
}
