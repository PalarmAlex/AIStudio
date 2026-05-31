using AIStudio.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Windows
{
  public partial class LogSessionPickerWindow : Window
  {
    public LogSessionPickerViewModel ViewModel { get; }

    public LogSessionPickerWindow(string title, string headerText, LogSessionPickerKind kind, IEnumerable<string> initiallySelectedKeys)
    {
      InitializeComponent();
      Title = title;
      HeaderText.Text = headerText;
      ViewModel = new LogSessionPickerViewModel(kind, initiallySelectedKeys);
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
