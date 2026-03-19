using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>Подсказка для объединённой колонки ОР/УМ: либо уровень мышления, либо тип ОР.</summary>
  public class OrUmTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length < 2 || !(values[0] is LiveLogsViewModel viewModel) || !(values[1] is string displayOrUm))
        return "ОР/УМ";

      var s = displayOrUm.Trim();
      if (s == "1" || s == "2" || s == "УМ1" || s == "УМ2")
      {
        bool? success = null;
        if (values.Length >= 3 && values[2] is string successStr)
        {
          if (successStr == "True") success = true;
          else if (successStr == "False") success = false;
        }
        return viewModel.GetThinkingLevelTooltip(s, success);
      }

      if (s == "-" || string.IsNullOrEmpty(s))
        return "Нет активации ОР или уровня мышления";

      return viewModel.GetOrientationReflexTooltip(s);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
