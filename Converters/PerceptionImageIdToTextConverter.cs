using ISIDA.Reflexes;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class PerceptionImageIdToTextConverter : IValueConverter
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
              // Создаем описание аналогичное тому, что в фильтрах
              var description = $"Образ {image.Id}";

              if (image.InfluenceActionsList != null && image.InfluenceActionsList.Any())
              {
                description += $", возд.: {image.InfluenceActionsList.Count}";
              }

              if (image.PhraseIdList != null && image.PhraseIdList.Any())
              {
                description += $", фраз: {image.PhraseIdList.Count}";
              }

              return description;
            }
            return $"Образ #{imageId}";
          }
        }
        return "Без образа";
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Ошибка в PerceptionImageIdToTextConverter: {ex.Message}");
        return "Ошибка загрузки";
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}