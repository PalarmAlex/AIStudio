using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Psychic.Automatism;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Конвертер подсказки для колонки «Образ восприятия» в таблице условных рефлексов.
  /// Принимает [Level3, ToneId, MoodId] и выводит детали образа с реальными тоном и настроением рефлекса.
  /// </summary>
  public class ConditionedReflexPerceptionTooltipConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      try
      {
        int imageId = values != null && values.Length > 0 && values[0] is int id ? id : 0;
        int toneId = values != null && values.Length > 1 && values[1] is int t ? t : 0;
        int moodId = values != null && values.Length > 2 && values[2] is int m ? m : 0;

        if (imageId <= 0)
          return "Образ восприятия не выбран";

        if (!PerceptionImagesSystem.IsInitialized)
          return "Система образов восприятия не инициализирована";

        var perceptionSystem = PerceptionImagesSystem.Instance;
        var images = perceptionSystem.GetAllPerceptionImagesList();
        var image = images?.FirstOrDefault(img => img.Id == imageId);

        if (image == null)
          return $"Образ восприятия с ID {imageId} не найден";

        var tooltip = new StringBuilder();
        tooltip.AppendLine($"ID образа: {image.Id}");

        tooltip.Append("Воздействия: ");
        if (image.InfluenceActionsList != null && image.InfluenceActionsList.Any())
        {
          if (InfluenceActionSystem.IsInitialized)
          {
            var influenceSystem = InfluenceActionSystem.Instance;
            var allActions = influenceSystem.GetAllInfluenceActions();
            var actionNames = image.InfluenceActionsList
                .Where(actId => allActions.Any(a => a.Id == actId))
                .Select(actId =>
                {
                  var action = allActions.First(a => a.Id == actId);
                  return $"{action.Name} (ID: {action.Id})";
                })
                .ToList();

            tooltip.Append(actionNames.Any() ? string.Join("; ", actionNames) : "не найдены в системе");
          }
          else
            tooltip.Append("система воздействий не инициализирована");
        }
        else
          tooltip.Append("нет воздействий");

        tooltip.AppendLine();

        tooltip.Append("Фразы: ");
        if (image.PhraseIdList != null && image.PhraseIdList.Any())
        {
          if (SensorySystem.IsInitialized)
          {
            var sensorySystem = SensorySystem.Instance;
            var phraseTexts = image.PhraseIdList
                .Select(phraseId =>
                {
                  string phraseText = sensorySystem.VerbalChannel?.GetPhraseFromPhraseId(phraseId);
                  return !string.IsNullOrEmpty(phraseText) ? $"\"{phraseText}\" (ID: {phraseId})" : $"[ID:{phraseId}] (фраза не найдена)";
                })
                .ToList();

            tooltip.Append(phraseTexts.Any() ? string.Join("; ", phraseTexts) : "не найдены в системе");
          }
          else
            tooltip.Append("система сенсорики не инициализирована");
        }
        else
          tooltip.Append("нет фраз");

        tooltip.AppendLine();

        // Тон и настроение берём из условного рефлекса (ToneId, MoodId)
        string toneText = ActionsImagesSystem.IsInitialized ? ActionsImagesSystem.GetToneText(toneId) : null;
        string moodText = ActionsImagesSystem.IsInitialized ? ActionsImagesSystem.GetMoodText(moodId) : null;
        if (string.IsNullOrEmpty(toneText)) toneText = "Нормальный";
        if (string.IsNullOrEmpty(moodText)) moodText = "Нормальное";
        tooltip.Append($"Тон/Настроение: {toneText} - {moodText}.");

        return tooltip.ToString();
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        return $"Ошибка загрузки деталей образа: {ex.Message}";
      }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
