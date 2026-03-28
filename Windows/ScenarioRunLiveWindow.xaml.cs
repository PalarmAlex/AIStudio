using System.Windows;

namespace AIStudio.Windows
{
  public partial class ScenarioRunLiveWindow : Window
  {
    public ScenarioRunLiveWindow()
    {
      InitializeComponent();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
