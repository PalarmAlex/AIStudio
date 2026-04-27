using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Конвертер для IsExpanded узла дерева: сворачивает уровни 1–4 (Эмоция, Understanding, NodePID, Триггер).
  /// values: [Level (int), CollapseEmotions, CollapseUnderstanding, CollapseNodePid, CollapseTrigger]
  /// </summary>
  public class EpisodicTreeExpandConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values == null || values.Length < 5) return true;
      if (!(values[0] is int level)) return true;
      bool collapseEmotions = values[1] is bool b1 && b1;
      bool collapseUnderstanding = values[2] is bool b2 && b2;
      bool collapseNodePid = values[3] is bool b3 && b3;
      bool collapseTrigger = values[4] is bool b4 && b4;

      bool shouldCollapse =
          (level == 1 && collapseEmotions) ||
          (level == 2 && collapseUnderstanding) ||
          (level == 3 && collapseNodePid) ||
          (level == 4 && collapseTrigger);
      return !shouldCollapse;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
