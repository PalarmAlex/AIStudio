using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AIStudio.ViewModels;

namespace AIStudio.Pages
{
  /// <summary>
  /// Логика взаимодействия для AgentPultView.xaml
  /// </summary>
  public partial class AgentPultView : UserControl
  {
    public AgentPultView()
    {
      InitializeComponent();
    }

    private void OnRootPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Enter)
        return;
      if (Keyboard.Modifiers != ModifierKeys.None)
        return;

      if (Keyboard.FocusedElement is ComboBoxItem)
        return;

      var messageBox = Keyboard.FocusedElement as TextBox;
      if (messageBox != null && messageBox.AcceptsReturn)
        return;

      if (IsFocusInsideOpenComboBoxDropDown(Keyboard.FocusedElement as DependencyObject))
        return;

      var vm = DataContext as AgentPultViewModel;
      if (vm == null)
        return;

      // Text по умолчанию пишет в VM при LostFocus; при Enter фокус ещё в TextBox — сбросить в источник вручную.
      var messageBinding = BindingOperations.GetBindingExpression(MessageToAgentTextBox, TextBox.TextProperty);
      if (messageBinding != null)
        messageBinding.UpdateSource();

      if (!vm.ApplyInfluenceCommand.CanExecute(null))
        return;

      vm.ApplyInfluenceCommand.Execute(null);
      e.Handled = true;
    }

    private static bool IsFocusInsideOpenComboBoxDropDown(DependencyObject focused)
    {
      for (DependencyObject d = focused; d != null; d = VisualTreeHelper.GetParent(d))
      {
        var cb = d as ComboBox;
        if (cb != null && cb.IsDropDownOpen)
          return true;
      }
      return false;
    }
  }
}
