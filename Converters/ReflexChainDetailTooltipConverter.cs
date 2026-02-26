using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Преобразует (GeneticReflexesViewModel, ReflexChainID) в текст подсказки с описанием цепочки рефлексов.
  /// </summary>
  public class ReflexChainDetailTooltipConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values?.Length >= 2 && values[0] is GeneticReflexesViewModel viewModel && values[1] is int chainId)
        return viewModel.GetChainDetailedInfo(chainId);
      return "Цепочка не привязана";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
