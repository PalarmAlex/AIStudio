using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIStudio.Common
{
  /// <summary>Разрешение путей к CSV-логам в каталоге проекта или ProgramData.</summary>
  public static class LogFilePaths
  {
    public static string ResolveLogFile(string fileName)
    {
      var candidates = new List<string>();

      var fromConfig = AppConfig.LogsFolderPath;
      if (!string.IsNullOrWhiteSpace(fromConfig))
        candidates.Add(Path.Combine(fromConfig.Trim(), fileName));

      candidates.Add(Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "ISIDA", "Logs", fileName));

      string bestPath = null;
      long bestSize = -1;
      foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
      {
        if (!File.Exists(path))
          continue;
        long size = new FileInfo(path).Length;
        if (size > bestSize)
        {
          bestSize = size;
          bestPath = path;
        }
      }

      return bestPath ?? candidates[0];
    }
  }
}
