using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class ThinkingLevelTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length >= 2 && values[0] is LiveLogsViewModel viewModel && values[1] is string displayValue)
      {
        bool? success = null;
        if (values.Length >= 3 && values[2] is string successStr)
        {
          if (successStr == "True") success = true;
          else if (successStr == "False") success = false;
        }
        return viewModel.GetThinkingLevelTooltip(displayValue, success);
      }
      return "Уровень мышления";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
