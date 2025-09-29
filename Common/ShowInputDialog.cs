using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AIStudio.Common
{
  public static class DialogHelper
  {
    public static string ShowInputDialog(string title, string promptText, string defaultValue)
    {
      var dialog = new Window()
      {
        Title = title,
        Width = 300,
        Height = 150,
        WindowStartupLocation = WindowStartupLocation.CenterScreen,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.Manual,
        WindowStyle = WindowStyle.SingleBorderWindow,
        ShowInTaskbar = false
      };

      var stackPanel = new StackPanel { Margin = new Thickness(10) };

      var textBlock = new TextBlock { Text = promptText, Margin = new Thickness(0, 0, 0, 10) };
      var textBox = new TextBox { Text = defaultValue };

      var buttonPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 10, 0, 0)
      };

      var okButton = new Button
      {
        Content = "OK",
        Width = 80,
        Margin = new Thickness(0, 0, 10, 0),
        IsDefault = true // Enter = OK
      };
      var cancelButton = new Button
      {
        Content = "Отмена",
        Width = 80,
        IsCancel = true // Esc = Cancel
      };

      // По умолчанию результат - отмена
      string result = null;

      okButton.Click += (sender, e) =>
      {
        result = textBox.Text;
        dialog.DialogResult = true;
        dialog.Close();
      };
      cancelButton.Click += (sender, e) =>
      {
        result = null;
        dialog.DialogResult = false;
        dialog.Close();
      };

      dialog.KeyDown += (sender, e) =>
      {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
          result = null;
          dialog.DialogResult = false;
          dialog.Close();
        }
      };

      buttonPanel.Children.Add(okButton);
      buttonPanel.Children.Add(cancelButton);

      stackPanel.Children.Add(textBlock);
      stackPanel.Children.Add(textBox);
      stackPanel.Children.Add(buttonPanel);

      dialog.Content = stackPanel;
      textBox.Focus();
      textBox.SelectAll();

      dialog.ShowDialog();

      return result;
    }

  }
}
