using AIStudio.ViewModels.Adapters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages.Adapters
{
  /// <summary>Страница «Зарегистрированные пакеты среды».</summary>
  public partial class AdaptersView : UserControl
  {
    /// <summary>
    /// Создаёт представление списка адаптеров.
    /// </summary>
    public AdaptersView()
    {
      InitializeComponent();
    }

    private AdaptersViewModel ViewModel => DataContext as AdaptersViewModel;
    private void AdaptersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (ViewModel?.Selected == null)
        return;
      ViewModel.EditSelected(Window.GetWindow(this));
    }

    private void AdaptersGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete || ViewModel?.Selected == null)
        return;
      ViewModel.DeleteSelected(Window.GetWindow(this));
      e.Handled = true;
    }
  }
}
