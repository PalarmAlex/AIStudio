using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;
using static AIStudio.Common.MemoryLogManager;

namespace AIStudio.Converters
{
  /// <summary>
  /// Подсказка ячейки «Триггер»: для автоматизма — расшифровка узла дерева автоматизмов,
  /// для рефлексов — образ восприятия (как прежде). Выбор источника — в <see cref="LiveLogsViewModel.GetTriggerCellTooltip"/>.
  /// </summary>
  public class TriggerCellTooltipMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length == 2
          && values[0] is LiveLogsViewModel viewModel
          && values[1] is LogEntry entry)
      {
        return viewModel.GetTriggerCellTooltip(entry);
      }
      return "Нет данных о триггере";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
