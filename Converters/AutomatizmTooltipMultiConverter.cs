using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class AutomatizmTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length >= 2 && values[0] is LiveLogsViewModel viewModel && values[1] is string displayValue)
      {
        int? usefulnessSnap = null;
        if (values.Length >= 3 && values[2] != null && values[2] != DependencyProperty.UnsetValue)
        {
          var v3 = values[2];
          if (v3 is int i32)
            usefulnessSnap = i32;
          else if (int.TryParse(System.Convert.ToString(v3, culture), NumberStyles.Integer, culture, out int parsed))
            usefulnessSnap = parsed;
        }

        var actionsImage = viewModel.GetActionsForAutomatizm(displayValue, usefulnessSnap);
        if (actionsImage != null)
        {
          // Используем существующий конвертер
          var converter = new AutomatizmActionsToTooltipConverter();
          return converter.Convert(actionsImage, targetType, parameter, culture);
        }
      }
      return "Нет данных о действиях автоматизма";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
