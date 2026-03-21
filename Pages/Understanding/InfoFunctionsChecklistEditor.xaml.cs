using ISIDA.Psychic.Thinking;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class InfoFunctionsChecklistEditor : Window
  {
    public List<int> SelectedIds { get; private set; }

    private sealed class FuncSelection
    {
      public int Id { get; set; }
      public string Label { get; set; }
      public bool IsSelected { get; set; }
    }

    /// <param name="title">Заголовок окна</param>
    /// <param name="currentIds">Уже выбранные номера инфо-функций</param>
    public InfoFunctionsChecklistEditor(string title, IEnumerable<int> currentIds)
    {
      InitializeComponent();
      Title = title;
      var cur = new HashSet<int>(currentIds ?? Enumerable.Empty<int>());
      SelectedIds = new List<int>(cur);

      var items = InfoFunctionsCatalog.GetAll()
          .Select(x => new FuncSelection
          {
            Id = x.Id,
            Label = $"{x.Id} — {x.Name}",
            IsSelected = cur.Contains(x.Id)
          })
          .ToList();

      FuncsList.ItemsSource = items;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      if (FuncsList.ItemsSource is IEnumerable<FuncSelection> src)
        SelectedIds = src.Where(x => x.IsSelected).Select(x => x.Id).OrderBy(x => x).ToList();
      else
        SelectedIds = new List<int>();

      DialogResult = true;
      Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close();
        e.Handled = true;
      }
    }
  }
}
