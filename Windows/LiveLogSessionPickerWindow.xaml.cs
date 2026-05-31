using AIStudio.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Windows
{
  public partial class LiveLogSessionPickerWindow : Window
  {
    public LiveLogSessionPickerViewModel ViewModel { get; }

    public LiveLogSessionPickerWindow(IEnumerable<string> initiallySelectedKeys)
    {
      InitializeComponent();
      ViewModel = new LiveLogSessionPickerViewModel(initiallySelectedKeys);
      DataContext = ViewModel;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
      }
    }
  }
}
