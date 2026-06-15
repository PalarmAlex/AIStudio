using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class RecipeStepValueSelectionDialog : Window
  {
    public string SelectedValue { get; private set; }

    public RecipeStepValueSelectionDialog(
        string title,
        string prompt,
        IEnumerable<RecipeStepCatalogPickItem> items,
        string initiallySelected)
    {
      InitializeComponent();
      Title = title ?? "Выбор значения";
      PromptText.Text = prompt ?? string.Empty;
      SelectedValue = initiallySelected ?? string.Empty;

      var list = (items ?? Enumerable.Empty<RecipeStepCatalogPickItem>())
          .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Value))
          .GroupBy(i => i.Value, System.StringComparer.OrdinalIgnoreCase)
          .Select(g => g.First())
          .OrderBy(i => i.Value)
          .ToList();
      ValuesList.ItemsSource = list;

      if (!string.IsNullOrWhiteSpace(SelectedValue))
      {
        RecipeStepCatalogPickItem match = list.FirstOrDefault(
            i => string.Equals(i.Value, SelectedValue, System.StringComparison.OrdinalIgnoreCase));
        if (match != null)
          ValuesList.SelectedItem = match;
      }
    }

    public RecipeStepValueSelectionDialog(string title, string prompt, IEnumerable<string> values, string initiallySelected)
        : this(
            title,
            prompt,
            (values ?? Enumerable.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => new RecipeStepCatalogPickItem { Value = v.Trim(), Description = string.Empty }),
            initiallySelected)
    {
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      if (ValuesList.SelectedItem is RecipeStepCatalogPickItem item)
        SelectedValue = item.Value;
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
      if (ValuesList.SelectedItem is RecipeStepCatalogPickItem item)
        SelectedValue = item.Value;
      DialogResult = true;
      Close();
    }
  }
}
