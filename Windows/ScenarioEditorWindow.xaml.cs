using System.ComponentModel;
using System.Windows;
using AIStudio.ViewModels.Research;

namespace AIStudio.Windows
{
  public partial class ScenarioEditorWindow : Window
  {
    public ScenarioEditorWindow()
    {
      InitializeComponent();
      Closing += OnClosingWindow;
    }

    private void OnClosingWindow(object sender, CancelEventArgs e)
    {
      if (DataContext is ScenarioEditorViewModel vm && vm.HasUnsavedChanges)
      {
        if (!vm.TryCancelWithPrompt())
          e.Cancel = true;
      }
    }
  }
}
