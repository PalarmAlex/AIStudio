using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using static ISIDA.Actions.AdaptiveActionsSystem;

namespace AIStudio.Converters
{
  public class ActionSizeConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is AdaptiveAction action)
      {
        int significance = action.GetSignificance();

        // Более чувствительная шкала
        double normalized = Math.Min(1.0, (double)significance / 30.0);

        // Увеличиваем диапазон размеров
        return 14 + normalized * 10;
      }
      return 14;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
