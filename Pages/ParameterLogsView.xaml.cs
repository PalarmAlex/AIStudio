// ParameterLogsView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels;

namespace AIStudio.Pages
{
  public partial class ParameterLogsView : UserControl
  {
    public ParameterLogsView()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      // Передаем DataGrid в ViewModel для динамического управления колонками
      if (DataContext is ParameterLogsViewModel viewModel)
      {
        viewModel.ParametersDataGrid = ParametersDataGrid;
      }
    }
  }
}