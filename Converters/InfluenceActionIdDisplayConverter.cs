using ISIDA.Actions;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Отображение ID воздействия с наименованием (для таблицы триггеров среды).
  /// </summary>
  public sealed class InfluenceActionIdDisplayConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      int id = 0;
      if (value is int intValue)
        id = intValue;
      else if (value != null && int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        id = parsed;

      if (id <= 0)
        return string.Empty;

      if (!InfluenceActionSystem.IsInitialized)
        return id.ToString(CultureInfo.InvariantCulture);

      var action = InfluenceActionSystem.Instance.GetAllInfluenceActions().FirstOrDefault(a => a.Id == id);
      if (action == null || string.IsNullOrWhiteSpace(action.Name))
        return id.ToString(CultureInfo.InvariantCulture);

      return id.ToString(CultureInfo.InvariantCulture) + " — " + action.Name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
