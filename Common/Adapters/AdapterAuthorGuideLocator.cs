using System;
using System.IO;
using System.Reflection;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Поиск <c>AdapterAuthorGuide.md</c> для пункта «Руководство автора».
  /// </summary>
  public static class AdapterAuthorGuideLocator
  {
    /// <summary>
    /// Возвращает путь к руководству или <c>null</c>.
    /// </summary>
    public static string TryFindGuidePath()
    {
      string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

      string[] candidates =
      {
        Path.Combine(baseDir, "docs", "AdapterAuthorGuide.md"),
        Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\docs\AdapterAuthorGuide.md")),
        Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\docs\AdapterAuthorGuide.md"))
      };

      foreach (string path in candidates)
      {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
          return path;
      }

      return null;
    }
  }
}
