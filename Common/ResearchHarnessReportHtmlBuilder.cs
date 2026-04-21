using ISIDA.Research;

namespace AIStudio.Common
{
  /// <summary>Краткий HTML-отчёт по JSON-прогону (делегирует в <see cref="HomeostasisHarnessJsonReportHtmlBuilder"/>).</summary>
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
      HomeostasisHarnessJsonReportHtmlBuilder.WriteReport(manifestPath, jsonlPath, reportHtmlPath, previewMaxLines);
    }
  }
}
