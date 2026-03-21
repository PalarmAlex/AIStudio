using AIStudio.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages.Understanding
{
  public partial class SituationTypesView : UserControl
  {
    public SituationTypesView()
    {
      InitializeComponent();
    }

    /// <summary>Перед сохранением уводим фокус с полей ввода (ComboBox фиксирует значение при потере фокуса).</summary>
    private void SaveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is Control ctrl)
        ctrl.Focus();
    }
  }
}
