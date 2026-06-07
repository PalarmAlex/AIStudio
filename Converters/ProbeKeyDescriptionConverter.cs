using AIStudio.Common.Adapters;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Возвращает описание пробы по ключу ProbeKey (для ToolTip в таблице правил давления).
  /// </summary>
  public sealed class ProbeKeyDescriptionConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string key = value as string ?? string.Empty;
      if (string.IsNullOrWhiteSpace(key))
        return null;

      var probes = AdapterSchemaLoader.LoadMetricProbesForCurrentProject();
      AdapterSchemaMetricProbe probe = probes.FirstOrDefault(p => string.Equals(p?.Key, key, StringComparison.Ordinal));
      if (probe == null)
        return null;

      if (!string.IsNullOrWhiteSpace(probe.Description))
        return probe.Description;

      if (!string.IsNullOrWhiteSpace(probe.Label))
        return probe.Label;

      return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
