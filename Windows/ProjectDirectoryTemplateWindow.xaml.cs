using System;
using System.Windows;
using System.Windows.Controls;
using ISIDA.Common;

namespace AIStudio.Windows
{
  /// <summary>
  /// Окно с деревом шаблонных каталогов проекта данных (только чтение).
  /// </summary>
  public partial class ProjectDirectoryTemplateWindow : Window
  {
    /// <summary>Создаёт окно и заполняет дерево из <see cref="SettingsValidator.GetProjectDirectoryTemplateRoot"/>.</summary>
    public ProjectDirectoryTemplateWindow()
    {
      InitializeComponent();
      var root = SettingsValidator.GetProjectDirectoryTemplateRoot();
      TemplateTree.ItemsSource = new[] { root };
      Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      Loaded -= OnLoaded;
      Dispatcher.BeginInvoke(new Action(ExpandFirstRoot), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ExpandFirstRoot()
    {
      if (TemplateTree.Items.Count == 0)
        return;
      TemplateTree.UpdateLayout();
      var container = TemplateTree.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
      if (container != null)
        container.IsExpanded = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
