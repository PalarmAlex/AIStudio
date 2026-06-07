using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AIStudio.Common.Adapters;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Dialogs
{
  /// <summary>
  /// Редактор правил detect для триггера среды.
  /// </summary>
  public partial class EnvironmentTriggerDetectEditorDialog : Window
  {
    /// <summary>
    /// Справочник типов detect из schema адаптера.
    /// </summary>
    public IReadOnlyList<AdapterSchemaDetectKind> DetectKindChoices { get; }

    /// <summary>
    /// Создаёт диалог.
    /// </summary>
    public EnvironmentTriggerDetectEditorDialog(
        IEnumerable<EnvironmentTriggerDetectRow> rules,
        IReadOnlyList<AdapterSchemaDetectKind> detectKinds)
    {
      InitializeComponent();
      DetectKindChoices = detectKinds ?? new AdapterSchemaDetectKind[0];
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
