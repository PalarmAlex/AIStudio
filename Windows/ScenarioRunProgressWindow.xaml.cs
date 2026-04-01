using System.Windows;

namespace AIStudio.Windows
{
  public partial class ScenarioRunProgressWindow : Window
  {
    private const double DefaultStatusFontSize = 14;
    private const double CompactStatusFontSize = 12.5;

    public ScenarioRunProgressWindow()
    {
      InitializeComponent();
    }

    public void SetStatus(string text, bool compactFont = false)
    {
      StatusTextBlock.Text = text ?? "";
      StatusTextBlock.FontSize = compactFont ? CompactStatusFontSize : DefaultStatusFontSize;
      StatusTextBlock.FontWeight = compactFont ? FontWeights.Normal : FontWeights.SemiBold;
    }
  }
}
