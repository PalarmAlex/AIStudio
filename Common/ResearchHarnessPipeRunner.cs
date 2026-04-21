using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ISIDA.Gomeostas;
using ISIDA.Research;
using Newtonsoft.Json;

namespace AIStudio.Common
{
  /// <summary>Результат разбора многострочного ввода (без запуска калькулятора).</summary>
  public sealed class ResearchHarnessPipeParseOutcome
  {
    public bool Success { get; set; }
    public List<string> BlockingErrors { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();
    public List<ResearchHarnessPipePreparedRow> Rows { get; } = new List<ResearchHarnessPipePreparedRow>();
  }

  /// <summary>Одна строка прогона после разбора и приведения типов.</summary>
  public sealed class ResearchHarnessPipePreparedRow
  {
    public int SourceLineNumber;
    public string[] RawCells;
    public string CaseId;
    public bool ExpectedBool;
    public bool ActualBool;
    public bool Match;

    internal int CriticalParamId;
    internal float CriticalCur;
    internal float CriticalPrev;
    internal float HarmfulValue;
    internal int ParamWeight;
    internal int ParamNorma;
    internal int ParamSpeed;
    internal bool ParamVital;
    internal float ParamCritMin;
    internal float ParamCritMax;
  }

  /// <summary>Итог записи артефактов прогона.</summary>
  public sealed class ResearchHarnessPipeRunOutcome
  {
    public bool Success;
    public string ErrorMessage;
    public string OutputDirectory;
    public int RowCount;
    public int MismatchCount;
    public long ElapsedMs;
    public string ReportHtmlPath;
  }

  /// <summary>Краткий манифест pipe-прогона (manifest.json).</summary>
  public sealed class PipeHarnessManifest
  {
    public string schema_version = "pipe-2";
    public string harness_id;
    public int row_count;
    public int mismatch_count;
    public long elapsed_ms;
    public string input_pipe_file;
    public string results_csv;
    public string report_html;
  }

  /// <summary>Парсинг строк «P1|P2|…|Out1», валидация и пакетный вызов калькулятора.</summary>
  public static class ResearchHarnessPipeRunner
  {
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Разбирает текст; при блокирующих ошибках <see cref="ResearchHarnessPipeParseOutcome.Success"/> = false.</summary>
    public static ResearchHarnessPipeParseOutcome Parse(
        ResearchHarnessPipeMethodInfo method,
        string multiLineText)
    {
      var outcome = new ResearchHarnessPipeParseOutcome();
      if (method == null)
      {
        outcome.BlockingErrors.Add("Не выбран метод.");
        return outcome;
      }

      var lines = (multiLineText ?? "").Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
      int logicalLine = 0;
      for (int i = 0; i < lines.Length; i++)
      {
        var line = lines[i].Trim();
        if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
          continue;
        logicalLine++;
        var cells = SplitPipeRow(line);
        if (cells.Length != method.ColumnCount)
        {
          outcome.BlockingErrors.Add(
              $"Строка {logicalLine} (файл строка {i + 1}): ожидается {method.ColumnCount} колонок через «|», получено {cells.Length}.");
          continue;
        }

        try
        {
          if (method.HarnessId == HomeostasisHarnessIds.HasCriticalParameterChanges)
            ParseCriticalRow(cells, logicalLine, i + 1, outcome);
          else if (method.HarnessId == HomeostasisHarnessIds.AnyVitalHarmfulZone)
            ParseHarmfulRow(cells, logicalLine, i + 1, outcome);
          else
            outcome.BlockingErrors.Add("Неизвестный harness_id у метода.");
        }
        catch (FormatException ex)
        {
          outcome.BlockingErrors.Add($"Строка {logicalLine}: {ex.Message}");
        }
      }

      if (outcome.Rows.Count == 0 && outcome.BlockingErrors.Count == 0)
        outcome.BlockingErrors.Add("Нет ни одной строки данных (пустой ввод или только комментарии).");

      outcome.Success = outcome.BlockingErrors.Count == 0;
      return outcome;
    }

    /// <summary>
    /// Автогенерация строк сценария для выбранного метода: эталонный Out1 берётся с <see cref="HomeostasisCalculator"/>
    /// (самосогласованные строки для проверки конвейера и логики метода).
    /// </summary>
    public static string BuildAutoScenarioText(ResearchHarnessPipeMethodInfo method, HomeostasisCalculator calculator)
    {
      if (method == null)
        return "# Автогенерация: метод не выбран.";
      if (calculator == null)
        return "# Автогенерация: калькулятор гомеостаза недоступен.";

      var sb = new StringBuilder();
      if (method.HarnessId == HomeostasisHarnessIds.HasCriticalParameterChanges)
        AppendAutoScenario_HasCritical(sb, calculator);
      else if (method.HarnessId == HomeostasisHarnessIds.AnyVitalHarmfulZone)
        AppendAutoScenario_AnyVitalHarmful(sb, calculator);
      else
        sb.AppendLine("# Автогенерация: неизвестный harness_id.");

      return sb.ToString().TrimEnd() + "\n";
    }

    private static void AppendAutoScenario_HasCritical(StringBuilder sb, HomeostasisCalculator calculator)
    {
      sb.AppendLine("# Автоген: HasCriticalParameterChanges — Out1 = факт метода при генерации.");
      int id = 1;
      const int weight = 50;
      const float cmin = 0f;
      const float cmax = 100f;

      // Сетка ~64 строки: дефицит/избыток, жизненная важность, две нормы, два якоря prev, четыре варианта cur
      foreach (bool vital in new[] { false, true })
      {
        foreach (int speed in new[] { -10, 10 })
        {
          foreach (int norma in new[] { 40, 55 })
          {
            foreach (float prev in new[] { 42f, 58f })
            {
              float step = Math.Max(Math.Abs(speed) / 100f, 1e-4f);
              float curNoise = speed < 0 ? prev + step * 0.4f : prev - step * 0.4f;
              float curWorse = speed < 0 ? prev - (step + 0.25f) : prev + (step + 0.25f);
              float curBetter = speed < 0 ? prev + (step + 0.25f) : prev - (step + 0.25f);
              float curEq = prev;
              foreach (var cur in new[] { curNoise, curWorse, curBetter, curEq })
              {
                float c = Clamp(cur, 0f, 100f);
                float p = Clamp(prev, 0f, 100f);
                AppendCriticalLine(sb, calculator, ref id, c, p, weight, norma, speed, vital, cmin, cmax);
              }
            }
          }
        }
      }

      // Малые ненулевые скорости (в ParameterData Speed=0 запрещён сеттером в isida)
      foreach (bool vital in new[] { false, true })
      {
        foreach (int speed in new[] { -2, -1, 1, 2 })
        {
          float prev = 50f;
          int norma = 50;
          float step = Math.Max(Math.Abs(speed) / 100f, 1e-4f);
          float curNoise = speed < 0 ? prev + step * 0.4f : prev - step * 0.4f;
          float curWorse = speed < 0 ? prev - (step + 0.2f) : prev + (step + 0.2f);
          foreach (var cur in new[] { curNoise, curWorse, prev })
            AppendCriticalLine(sb, calculator, ref id, Clamp(cur, 0f, 100f), prev, weight, norma, speed, vital, cmin, cmax);
        }
      }

      AppendCriticalLine(sb, calculator, ref id, 10f, 60f, 20, 50, -10, true, 5f, 95f);
      AppendCriticalLine(sb, calculator, ref id, 88f, 40f, 80, 50, 12, true, 0f, 100f);
    }

    private static void AppendAutoScenario_AnyVitalHarmful(StringBuilder sb, HomeostasisCalculator calculator)
    {
      sb.AppendLine("# Автоген: AnyVitalParameterInHarmfulZone — Out1 = факт метода при генерации.");
      int id = 1;
      const int weight = 50;
      const float cmin = 0f;
      const float cmax = 100f;

      foreach (bool vital in new[] { false, true })
      {
        foreach (int speed in new[] { -10, 1, 10 })
        {
          foreach (int norma in new[] { 35, 60 })
          {
            foreach (float rel in new[] { -15f, -0.5f, 0f, 0.5f, 15f })
            {
              float v = Clamp(norma + rel, 0f, 100f);
              AppendHarmfulLine(sb, calculator, ref id, v, weight, norma, speed, vital, cmin, cmax);
            }
          }
        }
      }

      AppendHarmfulLine(sb, calculator, ref id, 0.5f, 30, 50, -10, true, 0f, 100f);
      AppendHarmfulLine(sb, calculator, ref id, 99.5f, 30, 50, 10, true, 0f, 100f);
    }

    private static void AppendCriticalLine(
        StringBuilder sb,
        HomeostasisCalculator calculator,
        ref int id,
        float cur,
        float prev,
        int w,
        int norma,
        int speed,
        bool vital,
        float cmin,
        float cmax)
    {
      int pid = id++;
      string name = "P" + pid;
      var curList = new List<GomeostasSystem.ParameterData>
      {
        new GomeostasSystem.ParameterData(pid, name, "", cur, w, norma, speed, vital, cmin, cmax)
      };
      var prevList = new List<GomeostasSystem.ParameterData>
      {
        new GomeostasSystem.ParameterData(pid, name, "", prev, w, norma, speed, vital, cmin, cmax)
      };
      int out1 = calculator.HasCriticalParameterChanges(curList, prevList) ? 1 : 0;
      sb.Append(pid.ToString(Inv)).Append('|')
          .Append(cur.ToString(Inv)).Append('|')
          .Append(prev.ToString(Inv)).Append('|')
          .Append(w.ToString(Inv)).Append('|')
          .Append(norma.ToString(Inv)).Append('|')
          .Append(speed.ToString(Inv)).Append('|')
          .Append(vital ? "1" : "0").Append('|')
          .Append(cmin.ToString(Inv)).Append('|')
          .Append(cmax.ToString(Inv)).Append('|')
          .Append(out1.ToString(Inv))
          .AppendLine();
    }

    private static void AppendHarmfulLine(
        StringBuilder sb,
        HomeostasisCalculator calculator,
        ref int id,
        float val,
        int w,
        int norma,
        int speed,
        bool vital,
        float cmin,
        float cmax)
    {
      int pid = id++;
      string name = "P" + pid;
      var list = new List<GomeostasSystem.ParameterData>
      {
        new GomeostasSystem.ParameterData(pid, name, "", val, w, norma, speed, vital, cmin, cmax)
      };
      int out1 = calculator.AnyVitalParameterInHarmfulZone(list) ? 1 : 0;
      sb.Append(pid.ToString(Inv)).Append('|')
          .Append(val.ToString(Inv)).Append('|')
          .Append(w.ToString(Inv)).Append('|')
          .Append(norma.ToString(Inv)).Append('|')
          .Append(speed.ToString(Inv)).Append('|')
          .Append(vital ? "1" : "0").Append('|')
          .Append(cmin.ToString(Inv)).Append('|')
          .Append(cmax.ToString(Inv)).Append('|')
          .Append(out1.ToString(Inv))
          .AppendLine();
    }

    private static float Clamp(float v, float lo, float hi)
    {
      if (v < lo) return lo;
      if (v > hi) return hi;
      return v;
    }

    /// <summary>Выполняет прогон по уже разобранным строкам и пишет артефакты в каталог.</summary>
    public static ResearchHarnessPipeRunOutcome Execute(
        HomeostasisCalculator calculator,
        ResearchHarnessPipeMethodInfo method,
        List<ResearchHarnessPipePreparedRow> rows,
        string outputDirectory,
        string originalInputText)
    {
      var result = new ResearchHarnessPipeRunOutcome();
      if (calculator == null)
      {
        result.ErrorMessage = "Калькулятор недоступен.";
        return result;
      }

      if (string.IsNullOrWhiteSpace(outputDirectory))
      {
        result.ErrorMessage = "Не задан каталог вывода.";
        return result;
      }

      Directory.CreateDirectory(outputDirectory);
      var sw = Stopwatch.StartNew();

      foreach (var row in rows)
      {
        if (method.HarnessId == HomeostasisHarnessIds.HasCriticalParameterChanges)
          RunCritical(calculator, row);
        else
          RunHarmful(calculator, row);
      }

      sw.Stop();

      var inputPath = Path.Combine(outputDirectory, "input_pipe.txt");
      File.WriteAllText(inputPath, originalInputText ?? "", Encoding.UTF8);

      var csvPath = Path.Combine(outputDirectory, "results.csv");
      WriteCsv(method, rows, csvPath);

      int mismatches = rows.Count(r => !r.Match);
      var manifest = new PipeHarnessManifest
      {
        harness_id = method.HarnessId,
        row_count = rows.Count,
        mismatch_count = mismatches,
        elapsed_ms = sw.ElapsedMilliseconds,
        input_pipe_file = Path.GetFullPath(inputPath),
        results_csv = Path.GetFullPath(csvPath),
        report_html = Path.GetFullPath(Path.Combine(outputDirectory, "report.html"))
      };
      File.WriteAllText(Path.Combine(outputDirectory, "manifest.json"),
          JsonConvert.SerializeObject(manifest, Formatting.Indented), Encoding.UTF8);

      var reportPath = manifest.report_html;
      ResearchHarnessPipeReportHtmlBuilder.WriteReport(method, rows, manifest, reportPath);

      result.Success = true;
      result.OutputDirectory = outputDirectory;
      result.RowCount = rows.Count;
      result.MismatchCount = mismatches;
      result.ElapsedMs = sw.ElapsedMilliseconds;
      result.ReportHtmlPath = reportPath;
      return result;
    }

    private static string[] SplitPipeRow(string line)
    {
      return line.Split('|');
    }

    private static void ParseCriticalRow(
        string[] cells,
        int logicalLine,
        int fileLine,
        ResearchHarnessPipeParseOutcome outcome)
    {
      int pid = ParseIntStrict(cells[0], "P1 (id параметра)", logicalLine, outcome);
      float cur = ParseFloatStrict(cells[1], "P2 (текущее значение)");
      float prev = ParseFloatStrict(cells[2], "P3 (предыдущее значение)");
      int weight = ParseIntWithOptionalFractionWarning(cells[3], "P4 (вес)", logicalLine, outcome);
      int norma = ParseIntWithOptionalFractionWarning(cells[4], "P5 (норма)", logicalLine, outcome);
      int speed = ParseIntWithOptionalFractionWarning(cells[5], "P6 (скорость)", logicalLine, outcome);
      if (speed == 0)
      {
        outcome.BlockingErrors.Add(
            $"Строка {logicalLine}: P6 (скорость) не может быть 0 — в модели ParameterData допустима только ненулевая скорость (отрицательная: дефицит, положительная: избыток).");
        return;
      }

      bool vital = ParseBoolStrict(cells[6], "P7 (жизненно важен)");
      float cmin = ParseFloatStrict(cells[7], "P8 (крит. мин)");
      float cmax = ParseFloatStrict(cells[8], "P9 (крит. макс)");
      bool expected = ParseBoolStrict(cells[9], "P10 (ожидаемый Out1)");

      var row = new ResearchHarnessPipePreparedRow
      {
        SourceLineNumber = fileLine,
        RawCells = (string[])cells.Clone(),
        CaseId = "id=" + pid,
        ExpectedBool = expected,
        Match = false
      };
      AttachCriticalPayload(row, pid, cur, prev, weight, norma, speed, vital, cmin, cmax);
      outcome.Rows.Add(row);
    }

    private static void ParseHarmfulRow(
        string[] cells,
        int logicalLine,
        int fileLine,
        ResearchHarnessPipeParseOutcome outcome)
    {
      int pid = ParseIntStrict(cells[0], "P1 (id параметра)", logicalLine, outcome);
      float val = ParseFloatStrict(cells[1], "P2 (значение)");
      int weight = ParseIntWithOptionalFractionWarning(cells[2], "P3 (вес)", logicalLine, outcome);
      int norma = ParseIntWithOptionalFractionWarning(cells[3], "P4 (норма)", logicalLine, outcome);
      int speed = ParseIntWithOptionalFractionWarning(cells[4], "P5 (скорость)", logicalLine, outcome);
      if (speed == 0)
      {
        outcome.BlockingErrors.Add(
            $"Строка {logicalLine}: P5 (скорость) не может быть 0 — в модели ParameterData допустима только ненулевая скорость (отрицательная: дефицит, положительная: избыток).");
        return;
      }

      bool vital = ParseBoolStrict(cells[5], "P6 (жизненно важен)");
      float cmin = ParseFloatStrict(cells[6], "P7 (крит. мин)");
      float cmax = ParseFloatStrict(cells[7], "P8 (крит. макс)");
      bool expected = ParseBoolStrict(cells[8], "P9 (ожидаемый Out1)");

      var row = new ResearchHarnessPipePreparedRow
      {
        SourceLineNumber = fileLine,
        RawCells = (string[])cells.Clone(),
        CaseId = "id=" + pid,
        ExpectedBool = expected,
        Match = false
      };
      AttachHarmfulPayload(row, pid, val, weight, norma, speed, vital, cmin, cmax);
      outcome.Rows.Add(row);
    }

    private static void AttachCriticalPayload(
        ResearchHarnessPipePreparedRow row,
        int pid, float cur, float prev, int weight, int norma, int speed, bool vital, float cmin, float cmax)
    {
      row.CriticalParamId = pid;
      row.CriticalCur = cur;
      row.CriticalPrev = prev;
      row.ParamWeight = weight;
      row.ParamNorma = norma;
      row.ParamSpeed = speed;
      row.ParamVital = vital;
      row.ParamCritMin = cmin;
      row.ParamCritMax = cmax;
    }

    private static void AttachHarmfulPayload(
        ResearchHarnessPipePreparedRow row,
        int pid, float val, int weight, int norma, int speed, bool vital, float cmin, float cmax)
    {
      row.CriticalParamId = pid;
      row.HarmfulValue = val;
      row.ParamWeight = weight;
      row.ParamNorma = norma;
      row.ParamSpeed = speed;
      row.ParamVital = vital;
      row.ParamCritMin = cmin;
      row.ParamCritMax = cmax;
    }

    private static void RunCritical(HomeostasisCalculator calculator, ResearchHarnessPipePreparedRow row)
    {
      string name = "P" + row.CriticalParamId;
      var cur = new List<GomeostasSystem.ParameterData>
      {
        new GomeostasSystem.ParameterData(
            row.CriticalParamId, name, "", row.CriticalCur,
            row.ParamWeight, row.ParamNorma, row.ParamSpeed, row.ParamVital,
            row.ParamCritMin, row.ParamCritMax)
      };
      var prev = new List<GomeostasSystem.ParameterData>
      {
        new GomeostasSystem.ParameterData(
            row.CriticalParamId, name, "", row.CriticalPrev,
            row.ParamWeight, row.ParamNorma, row.ParamSpeed, row.ParamVital,
            row.ParamCritMin, row.ParamCritMax)
      };
      bool actual = calculator.HasCriticalParameterChanges(cur, prev);
      row.ActualBool = actual;
      row.Match = actual == row.ExpectedBool;
    }

    private static void RunHarmful(HomeostasisCalculator calculator, ResearchHarnessPipePreparedRow row)
    {
      string name = "P" + row.CriticalParamId;
      var list = new List<GomeostasSystem.ParameterData>
      {
        new GomeostasSystem.ParameterData(
            row.CriticalParamId, name, "", row.HarmfulValue,
            row.ParamWeight, row.ParamNorma, row.ParamSpeed, row.ParamVital,
            row.ParamCritMin, row.ParamCritMax)
      };
      bool actual = calculator.AnyVitalParameterInHarmfulZone(list);
      row.ActualBool = actual;
      row.Match = actual == row.ExpectedBool;
    }

    private static void WriteCsv(ResearchHarnessPipeMethodInfo method, List<ResearchHarnessPipePreparedRow> rows, string path)
    {
      var sb = new StringBuilder();
      var headers = new List<string>();
      foreach (var label in method.ColumnLabels)
        headers.Add(EscapeCsv(label));
      headers.Add("Out факт");
      headers.Add("Итог строки");
      sb.AppendLine(string.Join(",", headers));

      foreach (var r in rows)
      {
        var cells = new List<string>();
        foreach (var c in r.RawCells)
          cells.Add(EscapeCsv(c));
        cells.Add(EscapeCsv(r.ActualBool ? "1" : "0"));
        cells.Add(EscapeCsv(r.Match ? "OK" : "NO"));
        sb.AppendLine(string.Join(",", cells));
      }

      File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string s)
    {
      if (s == null) return "";
      bool need = s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
      if (!need) return s;
      return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    private static int ParseIntStrict(string raw, string fieldName, int logicalLine, ResearchHarnessPipeParseOutcome outcome)
    {
      raw = (raw ?? "").Trim();
      if (raw.Length == 0)
        throw new FormatException($"{fieldName}: пустое значение (нужно целое число).");

      if (int.TryParse(raw, NumberStyles.Integer, Inv, out int v))
        return v;

      if (double.TryParse(NormalizeNumber(raw), NumberStyles.Float, Inv, out double d))
      {
        double rounded = Math.Round(d);
        if (Math.Abs(d - rounded) < 1e-9)
          return (int)rounded;
        outcome.Warnings.Add($"Строка {logicalLine}: {fieldName} — «{raw}» нецелое; для прогона будет использовано {(int)rounded}.");
        return (int)rounded;
      }

      throw new FormatException($"{fieldName}: «{raw}» не распознано как число (для целого поля нужны цифры, опционально знак и десятичная часть только с предупреждением).");
    }

    private static int ParseIntWithOptionalFractionWarning(string raw, string fieldName, int logicalLine, ResearchHarnessPipeParseOutcome outcome)
    {
      return ParseIntStrict(raw, fieldName, logicalLine, outcome);
    }

    private static float ParseFloatStrict(string raw, string fieldName)
    {
      raw = (raw ?? "").Trim();
      if (raw.Length == 0)
        throw new FormatException($"{fieldName}: пустое значение.");

      if (double.TryParse(NormalizeNumber(raw), NumberStyles.Float, Inv, out double d))
        return (float)d;

      throw new FormatException($"{fieldName}: «{raw}» не число.");
    }

    private static string NormalizeNumber(string raw)
    {
      return raw.Replace(',', '.');
    }

    private static bool ParseBoolStrict(string raw, string fieldName)
    {
      raw = (raw ?? "").Trim();
      if (raw.Length == 0)
        throw new FormatException($"{fieldName}: пустое значение (ожидается 0/1, да/нет).");

      if (int.TryParse(raw, NumberStyles.Integer, Inv, out int iv))
      {
        if (iv == 0) return false;
        if (iv == 1) return true;
        throw new FormatException($"{fieldName}: для логического поля допустимы 0 или 1, не «{raw}».");
      }

      if (bool.TryParse(raw, out bool b))
        return b;

      var low = raw.ToLowerInvariant();
      if (low == "да" || low == "yes" || low == "y")
        return true;
      if (low == "нет" || low == "no" || low == "n")
        return false;

      throw new FormatException($"{fieldName}: «{raw}» не распознано как логическое значение (0, 1, да, нет, true, false).");
    }
  }

}
