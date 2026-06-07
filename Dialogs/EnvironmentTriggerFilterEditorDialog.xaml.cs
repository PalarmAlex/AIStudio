using System.Collections.ObjectModel;
using System.Windows;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Dialogs
{
  /// <summary>
  /// Редактор фильтра контекста триггера (schema/trigger-filter.json).
  /// </summary>
  public partial class EnvironmentTriggerFilterEditorDialog : Window
  {
    public ObservableCollection<EnvironmentRecipePreconditionField> FilterFields { get; }

    /// <summary>
    /// Создаёт диалог с копией полей фильтра.
    /// </summary>
    public EnvironmentTriggerFilterEditorDialog(
        ObservableCollection<EnvironmentRecipePreconditionField> filterFields)
    {
      InitializeComponent();
      FilterFields = new ObservableCollection<EnvironmentRecipePreconditionField>();
      EnvironmentSchemaFieldsHelper.ReplaceFields(FilterFields, filterFields);
      DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }
  }
}
