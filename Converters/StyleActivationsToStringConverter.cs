using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class StyleActivationsToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is Dictionary<int, List<int>> styleActivations)
      {
        var parts = new List<string>();
        foreach (var kvp in styleActivations.Where(x => x.Value.Any()))
        {
          parts.Add($"{kvp.Key}:{string.Join(",", kvp.Value)}");
        }
        return string.Join(";", parts);
      }
      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
