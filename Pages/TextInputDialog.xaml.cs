using System;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class TextInputDialog : Window
  {
    private const string NewLineReplacement = "\\n"; // Специальный символ для замены переносов строк

    public string Text { get; set; }
    public bool Multiline { get; set; }

    public TextInputDialog()
    {
      InitializeComponent();
      DataContext = this;
    }

    // Новый метод для установки текста с учетом многострочности
    public void SetText(string text, bool isMultiline)
    {
      Multiline = isMultiline;

      if (isMultiline && !string.IsNullOrEmpty(text))
      {
        // Заменяем специальные символы на настоящие переносы строк для отображения
        Text = text.Replace(NewLineReplacement, Environment.NewLine);
      }
      else
      {
        Text = text;
      }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
        e.Handled = true;
      }
      else if (e.Key == Key.Enter && !Multiline)
      {
        DialogResult = true;
        Close();
        e.Handled = true;
      }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      if (Multiline && !string.IsNullOrEmpty(Text))
      {
        // Заменяем настоящие переносы строк на специальные символы
        Text = Text.Replace("\r\n", NewLineReplacement)
                   .Replace("\n", NewLineReplacement)
                   .Replace("\r", NewLineReplacement);
      }

      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }
  }
}