using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Конвертер для отображения текста фразы по PhraseId
  /// </summary>
  public class WordIdToTextConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int wordId && wordId != 0)
      {
        try
        {
          if (SensorySystem.IsInitialized)
          {
            var sensorySystem = SensorySystem.Instance;
            var wordText = sensorySystem.VerbalChannel.GetWordFromWordId(wordId);
            return string.IsNullOrEmpty(wordText) ? $"[Слово не найдено: {wordId}]" : wordText;
          }
        }
        catch
        {
          // Игнорируем ошибки
        }
        return $"[ID: {wordId}]";
      }
      return "Не задана";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
