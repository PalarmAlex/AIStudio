using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace AIStudio.Converters
{
  public class InfluenceValidationRule : ValidationRule
  {
    public override ValidationResult Validate(object value, CultureInfo culture)
    {
      string input = value?.ToString() ?? string.Empty;

      // Пропускаем пустые значения и частичный ввод
      if (string.IsNullOrWhiteSpace(input) || input.EndsWith(",") || input.EndsWith(":"))
        return ValidationResult.ValidResult;

      // Исправленное регулярное выражение с закрывающими скобками
      if (!Regex.IsMatch(input, @"^(\s*\d+\s*:\s*-?\d+(\.\d+)?\s*(,\s*\d+\s*:\s*-?\d+(\.\d+)?\s*)*)$"))
      {
        return new ValidationResult(false, "Формат: 'ID:значение, ID:значение'\nПример: '1:-0.5, 2:1.2'");
      }

      return ValidationResult.ValidResult;
    }
  }
}