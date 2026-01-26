using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Sensors;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class AutomatizmActionsToTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is AutomatizmsViewModel.ActionsImageDisplay actionsImage)
      {
        var sb = new StringBuilder();

        if (actionsImage.ActIdList != null && actionsImage.ActIdList.Any())
        {
          if (AdaptiveActionsSystem.IsInitialized)
          {
            var adaptiveSystem = AdaptiveActionsSystem.Instance;
            var allActions = adaptiveSystem.GetAllAdaptiveActions();
            var names = actionsImage.ActIdList
                .Where(id => allActions.Any(a => a.Id == id))
                .Select(id => allActions.First(a => a.Id == id).Name)
                .ToList();

            sb.AppendLine($"Действия ({actionsImage.ActIdList.Count}): {string.Join(", ", names)}");
          }
          else
          {
            sb.AppendLine($"Действия: {string.Join(", ", actionsImage.ActIdList)}");
          }
        }
        else
          sb.AppendLine("Действия: нет");

        if (actionsImage.PhraseIdList != null && actionsImage.PhraseIdList.Any())
        {
          if (SensorySystem.IsInitialized)
          {
            var sensorySystem = SensorySystem.Instance;
            var allSensors = sensorySystem.VerbalChannel.GetAllPhrases();
            var phraseTexts = actionsImage.PhraseIdList
                .Where(id => allSensors.Any(a => a.Key == id))
                .Select(id =>
                {
                  var phrase = allSensors.First(a => a.Key == id);
                  return $"\"{phrase.Value}\" (ID: {phrase.Key})";
                })
                .ToList();

            if (phraseTexts.Any())
              sb.AppendLine($"Фразы ({actionsImage.PhraseIdList.Count}): {string.Join(", ", phraseTexts)}");
          }
          else
          {
            sb.AppendLine($"Фразы: {string.Join(", ", actionsImage.PhraseIdList)}");
          }
        }
        else
          sb.AppendLine("Фразы: нет");

        if (actionsImage.ToneId != 0)
          sb.AppendLine($"Тон: {actionsImage.ToneId}");
        else
          sb.AppendLine("Тон: 0 (нормальный)");

        if (actionsImage.MoodId != 0)
          sb.AppendLine($"Настроение: {actionsImage.MoodId}");
        else
          sb.AppendLine("Настроение: 0 (нормальное)");

        return sb.ToString();
      }
      return "Нет данных об образе действий";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}