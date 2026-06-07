using AIStudio.Common.Adapters;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Русская подпись detect.kind из schema/trigger-detect.json.
  /// </summary>
  public sealed class DetectKindLabelConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string kind = value as string ?? string.Empty;
      if (string.IsNullOrWhiteSpace(kind))
        return string.Empty;

      var kinds = AdapterSchemaLoader.LoadForCurrentProject().TriggerDetectKinds;
      AdapterSchemaDetectKind match = kinds.FirstOrDefault(
          k => string.Equals(k?.Kind, kind, StringComparison.OrdinalIgnoreCase));
      if (match != null && !string.IsNullOrWhiteSpace(match.Label))
        return match.Label;

      return kind;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
