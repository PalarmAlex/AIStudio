using System;
using System.IO;

namespace AIStudio.Common
{
  /// <summary>Разрешение путей к CSV-логам в каталоге проекта или ProgramData.</summary>
  public static class LogFilePaths
  {
    public static string ResolveLogFile(string fileName)
    {
      var fromConfig = AppConfig.LogsFolderPath;
      if (!string.IsNullOrWhiteSpace(fromConfig))
        return Path.Combine(fromConfig.Trim(), fileName);
      return Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "ISIDA", "Logs", fileName);
    }
  }
}
