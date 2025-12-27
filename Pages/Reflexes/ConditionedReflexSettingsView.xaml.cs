using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels;

namespace AIStudio.Views
{
  public partial class ConditionedReflexSettingsView : Window
  {
    public ConditionedReflexSettingsView(ConditionedReflexSettingsViewModel viewModel)
    {
      InitializeComponent();
      DataContext = viewModel;

      // Подписываемся на события
      viewModel.SettingsSaved += OnSettingsSaved;
      viewModel.SettingsCancelled += OnSettingsCancelled;

      // Закрытие окна при нажатии Esc
      this.PreviewKeyDown += (s, e) =>
      {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
          Close();
        }
      };
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        // Автоматическое выделение всего текста при фокусе
        textBox.SelectAll();
      }
    }

    private void TextBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        // Если TextBox еще не в фокусе, обрабатываем клик мышью
        if (!textBox.IsFocused)
        {
          textBox.Focus();
          e.Handled = true;
        }
      }
    }

    private void OnSettingsSaved(object sender, System.EventArgs e)
    {
      DialogResult = true;
      Close();
    }

    private void OnSettingsCancelled(object sender, System.EventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        if (!string.IsNullOrEmpty(textBox.Text))
        {
          string correctedText = textBox.Text.Replace(',', '.');
          if (correctedText != textBox.Text)
          {
            textBox.Text = correctedText;
          }
        }
      }
    }
  }
}