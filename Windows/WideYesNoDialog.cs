using System.Windows;
using System.Windows.Controls;

namespace AIStudio.Windows
{
  /// <summary>
  /// Подтверждение Да/Нет с увеличенной шириной. Обычный <see cref="MessageBox"/> нельзя расширить,
  /// а длинные строки в нём переносятся по пробелам.
  /// </summary>
  public static class WideYesNoDialog
  {
    public static bool Show(Window owner, string message, string title)
    {
      var textBlock = new TextBlock
      {
        Text = message,
        FontSize = 14,
        TextWrapping = TextWrapping.NoWrap
      };
      var scroll = new ScrollViewer
      {
        Content = textBlock,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        Padding = new Thickness(12, 12, 12, 8),
        MaxHeight = 440
      };
      var btnYes = new Button
      {
        Content = "Да",
        Width = 88,
        Height = 26,
        IsDefault = true,
        Margin = new Thickness(0, 0, 10, 0)
      };
      var btnNo = new Button { Content = "Нет", Width = 88, Height = 26, IsCancel = true };
      var buttons = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(12, 0, 12, 12)
      };
      buttons.Children.Add(btnYes);
      buttons.Children.Add(btnNo);

      var root = new Grid();
      root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
      root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      Grid.SetRow(scroll, 0);
      Grid.SetRow(buttons, 1);
      root.Children.Add(scroll);
      root.Children.Add(buttons);

      var win = new Window
      {
        Title = title,
        Content = root,
        MinWidth = 550,
        Width = 550,
        MinHeight = 160,
        SizeToContent = SizeToContent.Height,
        ShowInTaskbar = false,
        ResizeMode = ResizeMode.CanResizeWithGrip
      };
      if (owner != null && owner.IsLoaded)
      {
        win.Owner = owner;
        win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      }
      else
        win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

      btnYes.Click += (_, __) => { win.DialogResult = true; };
      btnNo.Click += (_, __) => { win.DialogResult = false; };

      return win.ShowDialog() == true;
    }
  }
}
