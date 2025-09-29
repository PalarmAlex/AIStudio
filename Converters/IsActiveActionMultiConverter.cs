using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using ISIDA.Actions;
using static ISIDA.Actions.AdaptiveActionsSystem;

namespace AIStudio.Converters
{
  public class IsActiveActionMultiConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values == null || values.Length != 2 || values[0] == null || values[1] == null)
      {
        return targetType == typeof(Brush) ? (object)Brushes.Gray : false;
      }

      if (values[0] is ObservableCollection<AdaptiveAction> currentActiveActions &&
          values[1] is AdaptiveAction actionToCheck)
      {
        bool isCurrentlyActive = currentActiveActions.Any(a => a.Id == actionToCheck.Id);

        if (targetType == typeof(Brush))
        {
          return isCurrentlyActive ? GetActiveColorBrush(actionToCheck) : Brushes.Gray;
        }

        return isCurrentlyActive;
      }

      return targetType == typeof(Brush) ? (object)Brushes.Gray : false;
    }

    private Brush GetActiveColorBrush(AdaptiveAction action)
    {
      int significance = action.GetSignificance();
      double normalized = Math.Min(1.0, significance / 50.0);

      // Фиолетовый -> Синий -> Голубой -> Зеленый -> Желтый -> Красный
      if (normalized < 0.2)
        return new SolidColorBrush(Color.FromRgb((byte)(128 + normalized * 635), 0, 255));
      else if (normalized < 0.4)
        return new SolidColorBrush(Color.FromRgb(0, (byte)((normalized - 0.2) * 1275), 255));
      else if (normalized < 0.6)
        return new SolidColorBrush(Color.FromRgb(0, 255, (byte)(255 - (normalized - 0.4) * 1275)));
      else if (normalized < 0.8)
        return new SolidColorBrush(Color.FromRgb((byte)((normalized - 0.6) * 1275), 255, 0));
      else
        return new SolidColorBrush(Color.FromRgb(255, (byte)(255 - (normalized - 0.8) * 1275), 0));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}