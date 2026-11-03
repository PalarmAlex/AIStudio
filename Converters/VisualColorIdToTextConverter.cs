using ISIDA.Reflexes;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Преобразует код зрительного канала (VisualColorId, <see cref="AgentVisualColor"/>)
  /// в краткое имя цвета для таблицы. 0 — белый wildcard («любой документ»).
  /// </summary>
  public class VisualColorIdToTextConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int code)
      {
        if (code == AgentVisualColor.White)
          return "Белый (любой)";
        if (AgentVisualColor.IsValidCode(code))
          return $"{AgentVisualColor.GetDisplayName(code)} ({code})";
        return $"Код {code}";
      }
      return "Белый (любой)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
