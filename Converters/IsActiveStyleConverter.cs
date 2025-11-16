using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.Converters
{
  public class IsActiveStyleConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length < 2) return false;

      var currentStyle = values[0] as BehaviorStyle;
      var activeStyles = values[1] as IList<BehaviorStyle>;

      return activeStyles?.Any(s => s?.Id == currentStyle?.Id) ?? false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}