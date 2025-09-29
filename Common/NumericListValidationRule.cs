using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AIStudio.Common
{
  public class NumericListValidationRule : ValidationRule
  {
    public override ValidationResult Validate(object value, CultureInfo culture)
    {
      string input = value?.ToString() ?? string.Empty;

      // Разрешаем пустую строку или строку, заканчивающуюся на запятую (частичный ввод)
      if (string.IsNullOrWhiteSpace(input) || input.EndsWith(","))
        return ValidationResult.ValidResult;

      // Проверяем, что все элементы - числа
      var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var part in parts)
      {
        if (!int.TryParse(part.Trim(), out _))
        {
          return new ValidationResult(false,
              "Допустимы только целые числа через запятую\nПример: '1, 2, 3'");
        }
      }

      return ValidationResult.ValidResult;
    }
  }
}
