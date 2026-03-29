using System.Windows;

namespace AIStudio.Windows
{
  public partial class ScenarioRunProgressWindow : Window
  {
    public ScenarioRunProgressWindow()
    {
      InitializeComponent();
    }

    public void SetStatus(string text)
    {
      StatusTextBlock.Text = text ?? "";
    }
  }
}
