using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIStudio.Common
{
  public sealed class ScenarioExpectationChoiceItem
  {
    public string Label { get; set; }
    public string Code { get; set; }
  }

  public static class ScenarioExpectationChoiceLists
  {
    public static List<ScenarioExpectationChoiceItem> BuildStateChoices()
    {
      return new List<ScenarioExpectationChoiceItem>
      {
        new ScenarioExpectationChoiceItem { Label = "Пусто", Code = "-" },
        new ScenarioExpectationChoiceItem { Label = "Неизвестно", Code = "" },
        new ScenarioExpectationChoiceItem { Label = "Плохо", Code = "-1" },
        new ScenarioExpectationChoiceItem { Label = "Норма", Code = "0" },
        new ScenarioExpectationChoiceItem { Label = "Хорошо", Code = "1" }
      };
    }

    public static List<ScenarioExpectationChoiceItem> BuildOrUmChoices()
    {
      return new List<ScenarioExpectationChoiceItem>
      {
        new ScenarioExpectationChoiceItem { Label = "Пусто", Code = "-" },
        new ScenarioExpectationChoiceItem { Label = "Неизвестно", Code = "" },
        new ScenarioExpectationChoiceItem { Label = "ОР1", Code = "ОР1" },
        new ScenarioExpectationChoiceItem { Label = "ОР2", Code = "ОР2" },
        new ScenarioExpectationChoiceItem { Label = "УМ1", Code = "УМ1" },
        new ScenarioExpectationChoiceItem { Label = "УМ2", Code = "УМ2" }
      };
    }

    /// <summary>Загружает комбинации из StyleCombinations.comb (первая колонка — коды через запятую).</summary>
    public static List<ScenarioExpectationChoiceItem> LoadStyleChoices(string combFilePath)
    {
      var list = new List<ScenarioExpectationChoiceItem>
      {
        new ScenarioExpectationChoiceItem { Label = "Пусто", Code = "-" },
        new ScenarioExpectationChoiceItem { Label = "Неизвестно", Code = "" }
      };
      try
      {
        if (string.IsNullOrWhiteSpace(combFilePath) || !File.Exists(combFilePath))
          return list;
        foreach (var line in File.ReadAllLines(combFilePath))
        {
          if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            continue;
          var parts = line.Split('|');
          if (parts.Length < 1)
            continue;
          var raw = parts[0].Trim();
          if (raw.Length == 0)
            continue;
          var codes = string.Join(",", raw.Split(',')
              .Select(s => s.Trim())
              .Where(s => s.Length > 0));
          if (codes.Length == 0)
            continue;
          list.Add(new ScenarioExpectationChoiceItem { Label = codes, Code = codes });
        }
      }
      catch
      {
        /* ignore */
      }
      return list;
    }
  }
}
