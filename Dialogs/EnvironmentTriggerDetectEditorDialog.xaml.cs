using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Dialogs
{
  /// <summary>
  /// Редактор правил detect для триггера среды.
  /// </summary>
  public partial class EnvironmentTriggerDetectEditorDialog : Window
  {
    /// <summary>
    /// Создаёт диалог.
    /// </summary>
    public EnvironmentTriggerDetectEditorDialog(IEnumerable<EnvironmentTriggerDetectRow> rules)
    {
      InitializeComponent();
      DetectGrid.ItemsSource = rules?.Select(Clone).ToList() ?? new List<EnvironmentTriggerDetectRow>();
    }

    /// <summary>
    /// Результат редактирования.
    /// </summary>
    public List<EnvironmentTriggerDetectRow> ResultRules =>
        (DetectGrid.ItemsSource as IEnumerable<EnvironmentTriggerDetectRow>)?.ToList()
        ?? new List<EnvironmentTriggerDetectRow>();
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }

    private static EnvironmentTriggerDetectRow Clone(EnvironmentTriggerDetectRow r)
    {
      return new EnvironmentTriggerDetectRow
      {
        Kind = r?.Kind,
        Environment = r?.Environment,
        Enabled = r != null && r.Enabled,
        CommandIdsText = r?.CommandIdsText
      };
    }
  }
}
