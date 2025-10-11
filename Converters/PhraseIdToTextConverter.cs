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
  public class PhraseIdToTextConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int phraseId && phraseId != 0)
      {
        try
        {
          if (SensorySystem.IsInitialized)
          {
            var sensorySystem = SensorySystem.Instance;
            var phraseText = sensorySystem.VerbalChannel.GetPhraseFromPhraseId(phraseId);
            return string.IsNullOrEmpty(phraseText) ? $"[Фраза не найдена: {phraseId}]" : phraseText;
          }
        }
        catch
        {
          // Игнорируем ошибки
        }
        return $"[ID: {phraseId}]";
      }
      return "Не задана";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
