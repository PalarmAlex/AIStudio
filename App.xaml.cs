using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AIStudio
{
  public partial class App : Application
  {
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      // Глобальный обработчик исключений
      AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
      {
        var exception = args.ExceptionObject as Exception;
        HandleGlobalException(exception);
      };

      DispatcherUnhandledException += (sender, args) =>
      {
        HandleGlobalException(args.Exception);
        args.Handled = true; // Предотвращаем аварийное завершение
      };

      TaskScheduler.UnobservedTaskException += (sender, args) =>
      {
        HandleGlobalException(args.Exception);
        args.SetObserved();
      };
    }

    private void HandleGlobalException(Exception ex)
    {
      if (ex is InvalidOperationException && ex.Message.Contains("Агент мертв"))
      {
        // Мягкая обработка смерти агента
        MessageBox.Show("Операция невозможна: агент мертв",
            "Агент мертв",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
      else
      {
        // Для других исключений показываем детальную информацию
        MessageBox.Show($"Произошла ошибка:\n{ex?.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }

      Debug.WriteLine($"Глобальное исключение: {ex}");
    }
  }
}
