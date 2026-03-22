using AIStudio.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AIStudio.Pages
{
  /// <summary>
  /// Логика взаимодействия для ThinkingCyclesView.xaml
  /// </summary>
  public partial class ThinkingCyclesView : UserControl
  {
    public ThinkingCyclesView()
    {
      InitializeComponent();
      Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is IDisposable disposable)
        disposable.Dispose();
    }
  }
}

