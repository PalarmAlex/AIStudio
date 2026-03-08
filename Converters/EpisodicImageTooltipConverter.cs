using ISIDA.Actions;
using ISIDA.Psychic.Automatism;
using ISIDA.Sensors;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Формирует подсказку для образа триггера или акции в формате:
  /// Действие: X | Фраза: Y | Тон/Настроение: Z
  /// </summary>
  public class EpisodicImageTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null) return "Нет данных";
      int imageId = System.Convert.ToInt32(value);
      if (imageId <= 0) return "—";

      bool isTrigger = parameter is string p && string.Equals(p, "Trigger", StringComparison.OrdinalIgnoreCase);
      var sb = new StringBuilder();

      var actImg = ActionsImagesSystem.IsInitialized ? ActionsImagesSystem.Instance.GetActionsImage(imageId) : null;
      if (actImg != null)
      {
        string actionText = "Нет";
        if (actImg.ActIdList != null && actImg.ActIdList.Count > 0 && AdaptiveActionsSystem.IsInitialized)
        {
          var adaptive = AdaptiveActionsSystem.Instance.GetAllAdaptiveActions();
          var names = actImg.ActIdList
              .Where(id => adaptive.Any(a => a.Id == id))
              .Select(id => adaptive.First(a => a.Id == id).Name)
              .ToList();
          actionText = names.Any() ? string.Join(", ", names) : string.Join(", ", actImg.ActIdList);
        }
        else if (actImg.ActIdList != null && actImg.ActIdList.Count > 0 && InfluenceActionSystem.IsInitialized)
        {
          var influence = InfluenceActionSystem.Instance.GetAllInfluenceActions();
          var names = actImg.ActIdList
              .Where(id => influence.Any(a => a.Id == id))
              .Select(id => influence.First(a => a.Id == id).Name)
              .ToList();
          actionText = names.Any() ? string.Join(", ", names) : string.Join(", ", actImg.ActIdList);
        }
        sb.AppendLine($"Действие: {actionText}");

        string phraseText = "Нет";
        if (actImg.PhraseIdList != null && actImg.PhraseIdList.Count > 0 && SensorySystem.IsInitialized)
        {
          var vc = SensorySystem.Instance.VerbalChannel;
          var phrases = actImg.PhraseIdList
              .Select(pid => vc?.GetPhraseFromPhraseId(pid))
              .Where(s => !string.IsNullOrEmpty(s))
              .ToList();
          phraseText = phrases.Any() ? string.Join(" ", phrases) : "Нет";
        }
        sb.AppendLine($"Фраза: {phraseText}");

        string tone = ActionsImagesSystem.GetToneText(actImg.ToneId);
        string mood = ActionsImagesSystem.GetMoodText(actImg.MoodId);
        sb.AppendLine(string.IsNullOrEmpty(tone) && string.IsNullOrEmpty(mood)
            ? "Тон/Настроение: —"
            : $"Тон/Настроение: {tone ?? "—"} - {mood ?? "—"}");

        return sb.ToString().TrimEnd();
      }

      if (isTrigger && InfluenceActionsImagesSystem.IsInitialized)
      {
        var infImg = InfluenceActionsImagesSystem.Instance.GetInfluenceActionsImage(imageId);
        if (infImg?.ActIdList != null && infImg.ActIdList.Count > 0 && InfluenceActionSystem.IsInitialized)
        {
          var influence = InfluenceActionSystem.Instance.GetAllInfluenceActions();
          var names = infImg.ActIdList
              .Where(id => influence.Any(a => a.Id == id))
              .Select(id => influence.First(a => a.Id == id).Name)
              .ToList();
          sb.AppendLine($"Действие: {(names.Any() ? string.Join(", ", names) : "Нет")}");
          sb.AppendLine("Фраза: Нет");
          sb.AppendLine("Тон/Настроение: —");
          return sb.ToString().TrimEnd();
        }
      }

      return $"ID образа: {imageId}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
