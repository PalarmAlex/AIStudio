using ISIDA.Actions;
using ISIDA.Psychic.Automatism;
using ISIDA.Psychic.Understanding;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>Код события агента (слоты 1–20) → подпись из AgentEventsCatalog.</summary>
  public sealed class SituationEventCodeDisplayConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is int code) || code < 0)
        return "—";
      return $"{code}: {AgentEventsCatalog.GetName(code)}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }

  /// <summary>MoodId (слоты 21–40) → подпись из справочника настроений ActionsImagesSystem.</summary>
  public sealed class SituationMoodIdDisplayConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is int moodId) || moodId == SituationTypeSystem.EmptySlotValue)
        return "—";
      if (!ActionsImagesSystem.IsInitialized)
        return $"{moodId}";
      var moods = ActionsImagesSystem.GetMoodList();
      string name;
      if (moods != null && moods.TryGetValue(moodId, out name) && !string.IsNullOrEmpty(name))
        return $"{moodId}: {name}";
      return $"{moodId}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }

  /// <summary>InfluenceId (слоты 41–60) → наименование из InfluenceActionSystem (как в справочнике воздействий).</summary>
  public sealed class SituationInfluenceIdDisplayConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is int infId) || infId == SituationTypeSystem.EmptySlotValue)
        return "—";
      if (!InfluenceActionSystem.IsInitialized)
        return $"{infId}";
      var all = InfluenceActionSystem.Instance.GetAllInfluenceActions();
      var a = all?.FirstOrDefault(x => x.Id == infId);
      if (a != null && !string.IsNullOrEmpty(a.Name))
        return $"{a.Id}: {a.Name}";
      return $"{infId}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }

  /// <summary>ThemeTypeId для подсказки в слоте темы (полный текст).</summary>
  public sealed class ThemeTypeIdSituationTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is int themeId) || themeId <= 0)
        return "—";
      string desc;
      if (ThemeImageSystem.IsInitialized)
        desc = ThemeImageSystem.Instance.GetThemeTypeDescription(themeId);
      else
      {
        var list = ThemeImageSystem.GetDefaultThemeTypesForSettings();
        var match = list.FirstOrDefault(x => x.Id == themeId);
        desc = match.Id == themeId ? (match.Description ?? "") : "";
      }
      return $"{themeId}: {desc}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
