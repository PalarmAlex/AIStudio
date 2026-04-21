using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ISIDA.Research;

namespace AIStudio.Common
{
  /// <summary>Краткий HTML-отчёт по результатам прогона (манифест + превью jsonl).</summary>
  public static class ResearchHarnessReportHtmlBuilder
  {
    /// <summary>
    /// Записывает report.html в каталог прогона.
    /// </summary>
    /// <param name="manifestPath">Путь к manifest.json.</param>
    /// <param name="jsonlPath">Путь к results.jsonl.</param>
    /// <param name="reportHtmlPath">Путь к создаваемому report.html.</param>
    /// <param name="previewMaxLines">Максимум строк превью из jsonl.</param>
    public static void WriteReport(string manifestPath, string jsonlPath, string reportHtmlPath, int previewMaxLines = 12)
    {
      HomeostasisHarnessManifest manifest = null;
      try
      {
        if (File.Exists(manifestPath))
          manifest = JsonConvert.DeserializeObject<HomeostasisHarnessManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
      }
      catch
      {
        // оставим manifest null
      }

      var previewRows = new List<string[]>();
      if (File.Exists(jsonlPath))
      {
        foreach (var line in File.ReadLines(jsonlPath).Take(previewMaxLines))
        {
          if (string.IsNullOrWhiteSpace(line))
            continue;
          try
          {
            var row = JsonConvert.DeserializeObject<HomeostasisHarnessResultRow>(line);
            if (row != null)
            {
              previewRows.Add(new[]
              {
                EscapeHtml(row.CaseId),
                EscapeHtml(row.HarnessId),
                row.HasCritical.HasValue ? (row.HasCritical.Value ? "true" : "false") : "—",
                row.AnyVitalHarmful.HasValue ? (row.AnyVitalHarmful.Value ? "true" : "false") : "—",
                EscapeHtml(row.Error ?? "")
              });
            }
          }
          catch
          {
            previewRows.Add(new[] { EscapeHtml(line), "", "", "", "" });
          }
        }
      }

      var sb = new StringBuilder();
      sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
      sb.AppendLine("<title>Прогон гомеостаза</title>");
      sb.AppendLine("<style>body{font-family:Segoe UI,sans-serif;margin:16px;} table{border-collapse:collapse;} th,td{border:1px solid #ccc;padding:6px 8px;font-size:12px;} th{background:#eee;} .muted{color:#666;} .bad{color:#b71c1c;}</style>");
      sb.AppendLine("</head><body>");
      sb.AppendLine("<h1>Исследовательский прогон гомеостаза</h1>");
      if (manifest != null)
      {
        sb.AppendLine("<p class=\"muted\">manifest.json</p><table>");
        sb.AppendLine($"<tr><th>harness_id</th><td>{EscapeHtml(manifest.HarnessId)}</td></tr>");
        sb.AppendLine($"<tr><th>row_count</th><td>{manifest.RowCount}</td></tr>");
        sb.AppendLine($"<tr><th>errors_count</th><td class=\"{(manifest.ErrorsCount > 0 ? "bad" : "")}\">{manifest.ErrorsCount}</td></tr>");
        sb.AppendLine($"<tr><th>elapsed_ms</th><td>{manifest.ElapsedMs}</td></tr>");
        sb.AppendLine($"<tr><th>schema_version</th><td>{EscapeHtml(manifest.SchemaVersion)}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("<p><strong>Файлы:</strong></p><ul>");
        sb.AppendLine($"<li><code>{EscapeHtml(manifest.OutputJsonl)}</code> — JSON Lines (по строке на кейс)</li>");
        sb.AppendLine($"<li><code>{EscapeHtml(manifest.OutputCsv)}</code> — CSV</li>");
        sb.AppendLine($"<li><code>{EscapeHtml(manifest.InputPath)}</code> — входной JSON</li>");
        sb.AppendLine("</ul>");
      }
      else
      {
        sb.AppendLine("<p class=\"bad\">Не удалось прочитать manifest.json</p>");
      }

      sb.AppendLine("<h2>Превью результатов (первые строки results.jsonl)</h2>");
      sb.AppendLine("<table><tr><th>case_id</th><th>harness_id</th><th>has_critical</th><th>any_vital_harmful</th><th>error</th></tr>");
      foreach (var cells in previewRows)
        sb.AppendLine("<tr><td>" + string.Join("</td><td>", cells) + "</td></tr>");
      if (previewRows.Count == 0)
        sb.AppendLine("<tr><td colspan=\"5\">(пусто)</td></tr>");
      sb.AppendLine("</table>");
      sb.AppendLine("<p class=\"muted\">Глубокий анализ — во внешних инструментах (Python и т.д.) по jsonl/csv.</p>");
      sb.AppendLine("</body></html>");

      File.WriteAllText(reportHtmlPath, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeHtml(string s)
    {
      if (string.IsNullOrEmpty(s))
        return "";
      return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
  }
}
