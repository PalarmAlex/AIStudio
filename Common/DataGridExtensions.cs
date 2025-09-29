using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System;

namespace AIStudio.Common
{
  public static class DataGridExtensions
  {
    public static bool IsEditing(this DataGrid dataGrid)
    {
      // Проверяем, находится ли какая-либо ячейка в режиме редактирования
      return dataGrid.IsKeyboardFocusWithin
          && dataGrid.GetEditingElement() != null;
    }

    private static FrameworkElement GetEditingElement(this DataGrid dataGrid)
    {
      // Ищем редактируемую ячейку в визуальном дереве
      var editingElement = dataGrid.FindVisualChild<TextBox>(x => x.IsFocused || x.IsKeyboardFocusWithin);
      return editingElement;
    }

    public static T FindVisualChild<T>(this DependencyObject parent, Func<T, bool> predicate = null)
    where T : DependencyObject
    {
      if (parent == null)
        return null;

      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is T result && (predicate == null || predicate(result)))
        {
          return result;
        }
        var foundChild = FindVisualChild(child, predicate);
        if (foundChild != null)
          return foundChild;
      }
      return null;
    }

    public static DataGridRow GetRowContainingElement(this FrameworkElement element)
    {
      while (element != null)
      {
        if (element is DataGridRow row)
          return row;
        element = VisualTreeHelper.GetParent(element) as FrameworkElement;
      }
      return null;
    }
  }
}