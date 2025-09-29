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
        double normalized = Math.Min(1.0, (double)significance / 50.0);
        return 18 + normalized * 10;
      }
      return 18;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
