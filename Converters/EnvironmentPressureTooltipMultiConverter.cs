using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class EnvironmentPressureTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length >= 3 && values[0] is LiveLogsViewModel viewModel)
      {
        string cell = values[1] as string;
        string stored = values[2] as string;
        return viewModel.GetEnvironmentPressureTooltip(cell, stored);
      }
      return "Давление метрик внешней среды на пульсе";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
