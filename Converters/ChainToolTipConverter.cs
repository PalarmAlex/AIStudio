using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AIStudio.ViewModels;

namespace AIStudio.Converters
{
  /// <summary>
  /// Конвертер для создания tooltip цепочки автоматизмов
  /// </summary>
  public class ChainToolTipConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      try
      {
        if (values.Length < 2 || values[0] == null || values[1] == null)
          return "Не удалось получить информацию о цепочке";

        if (!(values[0] is int chainId) || chainId <= 0)
          return "Цепочка не привязана";

        if (!(values[1] is AutomatizmsViewModel viewModel))
          return $"Цепочка {chainId}";

        return viewModel.GetChainDetailedInfo(chainId);
      }
      catch (Exception ex)
      {
        return $"Ошибка получения информации: {ex.Message}";
      }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
