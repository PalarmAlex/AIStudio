using AIStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class StyleTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length == 2 && values[0] is LiveLogsViewModel viewModel && values[1] is string displayValue)
      {
        return viewModel.GetStyleTooltip(displayValue);
      }
      return "Нет данных о стилях";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
