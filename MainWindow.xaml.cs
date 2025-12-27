using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace AIStudio
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private MainViewModel _viewModel;

    public MainWindow()
    {
      InitializeComponent();

      _viewModel = new MainViewModel();
      DataContext = _viewModel;

      // Closing - для подтверждения/отмены
      this.Closing += MainWindow_Closing;

      // Closed - для гарантированного завершения
      this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (_viewModel != null && !_viewModel.IsAgentDead)
      {
        var result = MessageBox.Show(
            "Закрыть приложение?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.No)
        {
          e.Cancel = true;
          return;
        }
      }
      GlobalTimer.Stop();
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
      try
      {
        _viewModel?.Shutdown();
        Task.Delay(1000).Wait(); // пауза для завершения всех операций
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"[MainWindow] Ошибка при завершении: {ex.Message}");
      }
    }
  }
}
