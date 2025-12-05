using AIStudio.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AIStudio.Pages.Reflexes
{
  public partial class ReflexChainsView : UserControl
  {
    public ReflexChainsView()
    {
      InitializeComponent();
      Loaded += OnLoaded;
      Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }
}