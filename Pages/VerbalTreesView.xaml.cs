using AIStudio.ViewModels;
using ISIDA.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AIStudio.Pages
{
  public partial class VerbalTreesView : UserControl
  {
    public VerbalTreesView()
    {
      InitializeComponent();
    }

    private void WordTreeView_Expanded(object sender, RoutedEventArgs e)
    {
      if (e.OriginalSource is TreeViewItem item &&
          item.DataContext is VerbalTreesViewModel.WordNode node &&
          node.HasChildren &&
          node.Children.Count == 0)
      {
        if (DataContext is VerbalTreesViewModel vm)
        {
          vm.LoadWordChildren(node.Id);
        }
      }
    }

    private void WordTreeView_Selected(object sender, RoutedEventArgs e)
    {
      if (e.OriginalSource is TreeViewItem item &&
          item.DataContext is VerbalTreesViewModel.WordNode node &&
          !node.IsLetter)
      {
        Logger.Info($"Selected word: {node.Text} (ID: {node.Id})");
      }
    }

    private void PhraseTreeView_Selected(object sender, RoutedEventArgs e)
    {
      if (e.OriginalSource is TreeViewItem item &&
          item.DataContext is VerbalTreesViewModel.PhraseNode node)
      {
        Logger.Info($"Selected phrase: {node.Text} (ID: {node.Id})");
      }
    }
  }

  #region Конвертеры

  public class BoolToExpandedIconConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (value is bool b && b) ? "[-]" : "[+]";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class BoolToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class BoolToBoldConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (value is bool b && b) ? FontWeights.Bold : FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class SearchHighlightMultiConverter : IMultiValueConverter
  {
    public Brush HighlightBrush { get; set; } = Brushes.Yellow;

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (values.Length != 2 || !(values[0] is string text) || !(values[1] is string searchText))
        return Brushes.Transparent;

      return !string.IsNullOrEmpty(searchText) &&
             text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
          ? HighlightBrush
          : Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  #endregion
}