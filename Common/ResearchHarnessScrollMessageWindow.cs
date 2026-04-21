using System.Windows;
using System.Windows.Controls;

namespace AIStudio.Common
{
  /// <summary>Диалог с прокручиваемым текстом (длинные ошибки разбора и т.п.).</summary>
  public static class ResearchHarnessScrollMessageWindow
  {
    public static void Show(string title, string body)
    {
      if (body == null)
        body = "";
      var win = new Window
      {
        Title = title,
        Width = 560,
        Height = 420,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ShowInTaskbar = false,
        ResizeMode = ResizeMode.CanResizeWithGrip
      };

      var tb = new TextBox
      {
        Text = body,
        IsReadOnly = true,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
        FontSize = 12,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(8)
      };

      var panel = new DockPanel();
      var btn = new Button
      {
        Content = "Закрыть",
        Margin = new Thickness(8),
        Padding = new Thickness(16, 6, 16, 6),
        HorizontalAlignment = HorizontalAlignment.Right,
        IsDefault = true,
        IsCancel = true
      };
      btn.Click += (_, __) => win.Close();
      DockPanel.SetDock(btn, Dock.Bottom);
      panel.Children.Add(btn);
      panel.Children.Add(tb);

      win.Content = panel;
      try
      {
        win.Owner = Application.Current?.MainWindow;
      }
      catch
      {
        /* без владельца */
      }

      win.ShowDialog();
    }
  }
}
