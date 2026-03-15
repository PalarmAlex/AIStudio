using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>Возвращает Bold для строки, начинающейся с «Образ действия» (для панели свойств дерева проблем).</summary>
  public class ActionImageLineToFontWeightConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is string line && !string.IsNullOrEmpty(line))
      {
        if (line.TrimStart().StartsWith("Образ действия", StringComparison.OrdinalIgnoreCase))
          return FontWeights.Bold;
      }
      return FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
