using ISIDA.Actions;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
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

              // Добавляем фразы
              tooltip.Append("Фразы: ");
              if (image.PhraseIdList != null && image.PhraseIdList.Any())
              {
                if (SensorySystem.IsInitialized)
                {
                  var sensorySystem = SensorySystem.Instance;
                  var allSensors = sensorySystem.VerbalChannel.GetAllPhrases();
                  var phraseTexts = image.PhraseIdList
                      .Where(id => allSensors.Any(a => a.Key == id))
                      .Select(id =>
                      {
                        var phrase = allSensors.First(a => a.Key == id);
                        return $"\"{phrase.Value}\" (ID: {phrase.Key})";
                      })
                      .ToList();

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
        System.Diagnostics.Debug.WriteLine($"Ошибка в PerceptionImageIdToTooltipConverter: {ex.Message}");
        return $"Ошибка загрузки деталей образа: {ex.Message}";
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}

