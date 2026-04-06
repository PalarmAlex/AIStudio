using System.Windows;

namespace AIStudio.Windows
{
  public partial class ScenarioRunProgressWindow : Window
  {
    private const double DefaultStatusFontSize = 14;
    private const double CompactStatusFontSize = 12.5;

    private const string DefaultFooter =
        "Выполняется сценарий — не закрывайте приложение до завершения.";

    public ScenarioRunProgressWindow()
    {
      InitializeComponent();
    }

    public void SetRunChromeDefault()
    {
      Title = "Выполняется сценарий";
      FooterTextBlock.Text = DefaultFooter;
    }

    public void SetRunChromeForScenario(int scenarioId)
    {
      Title = $"Выполняется сценарий №{scenarioId}";
      FooterTextBlock.Text =
          $"Выполняется сценарий №{scenarioId} — не закрывайте приложение до завершения.";
    }

    /// <param name="scenarioOrdinalInGroup">Номер сценария в порядке прогона группы (1-based).</param>
    public void SetRunChromeForScenarioGroup(int scenarioOrdinalInGroup)
    {
      Title = $"Выполняется сценарий №{scenarioOrdinalInGroup}";
      FooterTextBlock.Text =
          $"Выполняется сценарий №{scenarioOrdinalInGroup} — не закрывайте приложение до завершения.";
    }

    public void SetStatus(string text, bool compactFont = false)
    {
      StatusTextBlock.Text = text ?? "";
      StatusTextBlock.FontSize = compactFont ? CompactStatusFontSize : DefaultStatusFontSize;
      StatusTextBlock.FontWeight = compactFont ? FontWeights.Normal : FontWeights.SemiBold;
    }
  }
}
