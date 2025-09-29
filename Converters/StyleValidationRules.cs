using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace AIStudio.Converters
{
  public class StyleAntagonismValidationRule : ValidationRule
  {
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
      if (value is string input)
      {
        var ids = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim());

        foreach (var idStr in ids)
        {
          if (!int.TryParse(idStr, out _))
          {
            return new ValidationResult(false, $"Некорректный ID: {idStr}");
          }
        }

        return ValidationResult.ValidResult;
      }

      return new ValidationResult(false, "Некорректный формат данных");
    }
  }

  public class StyleActivationValidationRule : ValidationRule
  {
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
      if (value is string input)
      {
        var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim());

        foreach (var part in parts)
        {
          if (part.StartsWith("-"))
          {
            if (!int.TryParse(part.Substring(1), out _))
            {
              return new ValidationResult(false, $"Некорректный ID: {part}");
            }
          }
          else
          {
            if (part.StartsWith("2") || part.StartsWith("3"))
            {
              if (!int.TryParse(part.Substring(1), out var num) || num < 0 || num > 9)
              {
                return new ValidationResult(false, $"Сила боли/радости должна быть от 0 до 9: {part}");
              }
            }
            else if (!int.TryParse(part, out _))
            {
              return new ValidationResult(false, $"Некорректный ID: {part}");
            }
          }
        }

        return ValidationResult.ValidResult;
      }

      return new ValidationResult(false, "Некорректный формат данных");
    }
  }
}
