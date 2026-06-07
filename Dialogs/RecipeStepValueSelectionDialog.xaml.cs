using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class RecipeStepValueSelectionDialog : Window
  {
    public string SelectedValue { get; private set; }

    public RecipeStepValueSelectionDialog(string title, string prompt, IEnumerable<string> values, string initiallySelected)
    {
      InitializeComponent();
      Title = title ?? "Выбор значения";
      PromptText.Text = prompt ?? string.Empty;
      SelectedValue = initiallySelected ?? string.Empty;

      var items = (values ?? Enumerable.Empty<string>())
          .Where(v => !string.IsNullOrWhiteSpace(v))
          .Distinct()
          .OrderBy(v => v)
          .ToList();
      ValuesList.ItemsSource = items;

      if (!string.IsNullOrWhiteSpace(SelectedValue))
      {
        object match = items.FirstOrDefault(v => v == SelectedValue);
        if (match != null)
          ValuesList.SelectedItem = match;
      }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      if (ValuesList.SelectedItem is string value)
        SelectedValue = value;
      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
        e.Handled = true;
      }
      else if (e.Key == Key.Enter)
      {
        OkButton_Click(sender, e);
        e.Handled = true;
      }
    }

    private void ValuesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (ValuesList.SelectedItem is string value)
        SelectedValue = value;
      DialogResult = true;
      Close();
    }
  }
}
