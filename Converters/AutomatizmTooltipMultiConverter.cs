using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class AutomatizmTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length == 2 && values[0] is LiveLogsViewModel viewModel && values[1] is string displayValue)
      {
        var actionsImage = viewModel.GetActionsForAutomatizm(displayValue);
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
