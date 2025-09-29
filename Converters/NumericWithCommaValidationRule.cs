using System.Globalization;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace AIStudio.Converters
{
  public class NumericWithCommaValidationRule : ValidationRule
  {
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
      if (value is string strValue)
      {
        // Проверка на пустую строку
        if (string.IsNullOrWhiteSpace(strValue))
        {
          return new ValidationResult(false, "Значение не может быть пустым");
        }

        // Разбиваем строку по запятым
        string[] parts = strValue.Split(',');

        foreach (string part in parts)
        {
          string trimmedPart = part.Trim();

          // Пропускаем пустые части (две запятые подряд)
          if (string.IsNullOrEmpty(trimmedPart))
          {
            return new ValidationResult(false, "Нельзя использовать несколько запятых подряд");
          }

          // Проверяем, что часть является корректным целым числом
          if (!Regex.IsMatch(trimmedPart, @"^-?\d+$"))
          {
            return new ValidationResult(false, $"Некорректный формат числа: '{trimmedPart}'. Допустимы только целые числа (например: 1, -2, 3)");
          }
        }

        // Проверка на запятую в начале или конце
        if (strValue.StartsWith(",") || strValue.EndsWith(","))
        {
          return new ValidationResult(false, "Запятая не может быть первым или последним символом");
        }
      }

      return ValidationResult.ValidResult;
    }
  }
}