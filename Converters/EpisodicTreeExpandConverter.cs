using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Конвертер для IsExpanded узла дерева: сворачивает узлы уровня Эмоция (1), NodePID (2) или Триггер (3)
  /// при установленном соответствующем флажке в заголовке колонки.
  /// values: [Level (int), CollapseEmotions (bool), CollapseNodePid (bool), CollapseTrigger (bool)]
  /// </summary>
  public class EpisodicTreeExpandConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values == null || values.Length < 4) return true;
      if (!(values[0] is int level)) return true;
      bool collapseEmotions = values[1] is bool b1 && b1;
      bool collapseNodePid = values[2] is bool b2 && b2;
      bool collapseTrigger = values[3] is bool b3 && b3;

      bool shouldCollapse = (level == 1 && collapseEmotions) || (level == 2 && collapseNodePid) || (level == 3 && collapseTrigger);
      return !shouldCollapse;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
