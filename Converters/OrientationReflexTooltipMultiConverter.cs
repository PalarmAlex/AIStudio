using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class OrientationReflexTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length == 2 && values[0] is LiveLogsViewModel viewModel && values[1] is string displayValue)
      {
        return viewModel.GetOrientationReflexTooltip(displayValue);
      }
      return "Нет данных об ориентировочном рефлексе";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
