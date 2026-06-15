using AIStudio.ViewModels.SymbiontEnv;
using System.Windows;
using System.Windows.Controls;

namespace AIStudio.Controls.Environment
{
  /// <summary>Панель выбора действия и параметров по schema.</summary>
  public partial class SchemaActionPanel : UserControl
  {
    public SchemaActionPanel()
    {
      InitializeComponent();
    }

    private SchemaActionEditorViewModel Vm => DataContext as SchemaActionEditorViewModel;

    private void InsertPlaceholder_Click(object sender, RoutedEventArgs e)
    {
      if (sender is FrameworkElement element && element.Tag is SchemaParamRow row)
        Vm?.PickTemplatePlaceholder(Window.GetWindow(this), row);
    }

    private void PickPropertyName_Click(object sender, RoutedEventArgs e)
    {
      if (sender is FrameworkElement element && element.Tag is SchemaParamRow row)
        Vm?.PickPropertyName(Window.GetWindow(this), row);
    }
  }
}
