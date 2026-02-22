using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class PerceptionImageIdToTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      try
      {
        if (value is int imageId && imageId > 0)
        {
          if (PerceptionImagesSystem.IsInitialized)
          {
            var perceptionSystem = PerceptionImagesSystem.Instance;
            var images = perceptionSystem.GetAllPerceptionImagesList();
            var image = images.FirstOrDefault(img => img.Id == imageId);

            if (image != null)
            {
              var tooltip = new StringBuilder();
              tooltip.AppendLine($"ID образа: {image.Id}");

              // Добавляем воздействия
              tooltip.Append("Воздействия: ");
              if (image.InfluenceActionsList != null && image.InfluenceActionsList.Any())
              {
                if (InfluenceActionSystem.IsInitialized)
                {
                  var influenceSystem = InfluenceActionSystem.Instance;
                  var allActions = influenceSystem.GetAllInfluenceActions();
                  var actionNames = image.InfluenceActionsList
                      .Where(id => allActions.Any(a => a.Id == id))
                      .Select(id =>
                      {
                        var action = allActions.First(a => a.Id == id);
                        return $"{action.Name} (ID: {action.Id})";
                      })
                      .ToList();

                  if (actionNames.Any())
                  {
                    tooltip.Append(string.Join("; ", actionNames));
                  }
                  else
                  {
                    tooltip.Append("не найдены в системе");
                  }
                }
                else
                {
                  tooltip.Append("система воздействий не инициализирована");
                }
              }
              else
              {
                tooltip.Append("нет воздействий");
              }

              tooltip.AppendLine();

              tooltip.Append("Фразы: ");
              if (image.PhraseIdList != null && image.PhraseIdList.Any())
              {
                if (SensorySystem.IsInitialized)
                {
                  var sensorySystem = SensorySystem.Instance;
                  var phraseTexts = new List<string>();

                  foreach (var phraseId in image.PhraseIdList)
                  {
                    string phraseText = sensorySystem.VerbalChannel.GetPhraseFromPhraseId(phraseId);
                    if (!string.IsNullOrEmpty(phraseText))
                      phraseTexts.Add($"\"{phraseText}\" (ID: {phraseId})");
                    else
                      phraseTexts.Add($"[ID:{phraseId}] (фраза не найдена)");
                  }

                  if (phraseTexts.Any())
                  {
                    tooltip.Append(string.Join("; ", phraseTexts));
                  }
                  else
                  {
                    tooltip.Append("не найдены в системе");
                  }
                }
                else
                {
                  tooltip.Append("система сенсорики не инициализирована");
                }
              }
              else
              {
                tooltip.Append("нет фраз");
              }

              return tooltip.ToString();
            }
            else
            {
              return $"Образ восприятия с ID {imageId} не найден";
            }
          }
          else
          {
            return "Система образов восприятия не инициализирована";
          }
        }
        return "Образ восприятия не выбран";
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        return $"Ошибка загрузки деталей образа: {ex.Message}";
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}

