using ISIDA.Actions;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Описание воздействия по ID (ToolTip в таблице триггеров среды).
  /// </summary>
  public sealed class InfluenceActionIdDescriptionConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      int id = 0;
      if (value is int intValue)
        id = intValue;
      else if (value != null && int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        id = parsed;

      if (id <= 0 || !InfluenceActionSystem.IsInitialized)
        return null;

      var action = InfluenceActionSystem.Instance.GetAllInfluenceActions().FirstOrDefault(a => a.Id == id);
      if (action == null)
        return null;

      if (!string.IsNullOrWhiteSpace(action.Description))
        return action.Description;

      if (!string.IsNullOrWhiteSpace(action.Name))
        return action.Name;

      return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
