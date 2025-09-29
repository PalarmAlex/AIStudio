using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ISIDA.Gomeostas;

namespace AIStudio.Dialogs
{
  public partial class AntagonistStylesEditor : Window
  {
    public class StyleItem
    {
      public int Id { get; set; }
      public string Name { get; set; }
      public bool IsSelected { get; set; }
      public string DisplayText => $"{Name} (ID: {Id})";
    }

    public IEnumerable<int> SelectedStyleIds =>
        _styleItems.Where(x => x.IsSelected).Select(x => x.Id);

    private readonly List<StyleItem> _styleItems;

    public AntagonistStylesEditor(
        string title,
        IEnumerable<GomeostasSystem.BehaviorStyle> availableStyles,
        IEnumerable<int> selectedStyleIds)
    {
      InitializeComponent();
      Title = title;

      var selectedIds = new HashSet<int>(selectedStyleIds);
      _styleItems = availableStyles
          .OrderBy(s => s.Id)
          .Select(s => new StyleItem
          {
            Id = s.Id,
            Name = s.Name,
            IsSelected = selectedIds.Contains(s.Id)
          })
          .ToList();

      StylesListBox.ItemsSource = _styleItems;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
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
        Close(); // Закрываем окно при нажатии Esc
        e.Handled = true;
      }
    }
  }
}
