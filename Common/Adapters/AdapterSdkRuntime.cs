using System;
using System.Collections.Generic;
using System.IO;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Стартовый SDK в <c>runtime\</c> пакета адаптера (isida, Contract, Newtonsoft).
  /// </summary>
  internal static class AdapterSdkRuntime
  {
    /// <summary>Файлы стартового SDK в <c>%ProgramData%\ISIDA\AdapterPackageTemplates\demo\runtime\</c>.</summary>
    internal static readonly string[] RequiredSdkFileNames =
    {
      "isida.dll",
      "isida.dll.config",
      "SymbiontEnv.Contract.dll",
      "Newtonsoft.Json.dll"
    };
    /// <summary>
    /// Копирует стартовый SDK из каталога demo (установщик AIStudio) в <paramref name="runtimeTarget"/>.
    /// </summary>
    internal static bool TryEnsureSdk(string runtimeTarget, out string errorMessage)
    {
      errorMessage = null;
      if (string.IsNullOrWhiteSpace(runtimeTarget))
      {
        errorMessage = "Не указан каталог runtime.";
        return false;
      }
      string demoRuntime = Path.Combine(AdapterPaths.GetDemoTemplatePath(), "runtime");
      if (!Directory.Exists(demoRuntime))
      {
        errorMessage = "Каталог стартового SDK не найден:\n" + demoRuntime
            + "\n\nОн должен быть создан установщиком AIStudio.";
        return false;
      }
      var missing = new List<string>();
      for (int i = 0; i < RequiredSdkFileNames.Length; i++)
      {
        if (!File.Exists(Path.Combine(demoRuntime, RequiredSdkFileNames[i])))
          missing.Add(RequiredSdkFileNames[i]);
      }
      if (missing.Count > 0)
      {
        errorMessage = "В каталоге стартового SDK отсутствуют файлы: " + string.Join(", ", missing)
            + ".\n\n" + demoRuntime
            + "\n\nПереустановите AIStudio или скопируйте недостающие файлы в этот каталог.";
        return false;
      }
      Directory.CreateDirectory(runtimeTarget);
      CopySdkFilesFromDirectory(demoRuntime, runtimeTarget);
      return true;
    }

    /// <summary>Дополняет runtime DLL из каталога сборки host (поверх SDK).</summary>
    internal static void MergeHostBin(string hostBinDirectory, string runtimeTarget)
    {
      if (string.IsNullOrWhiteSpace(hostBinDirectory) || !Directory.Exists(hostBinDirectory))
        return;
      MergeDllsFromDirectory(hostBinDirectory, runtimeTarget, overwrite: true);
    }

    private static void CopySdkFilesFromDirectory(string sourceDir, string runtimeTarget)
    {
      foreach (string file in Directory.GetFiles(sourceDir))
      {
        string dest = Path.Combine(runtimeTarget, Path.GetFileName(file));
        File.Copy(file, dest, overwrite: true);
      }
    }

    private static void MergeDllsFromDirectory(string sourceDir, string runtimeTarget, bool overwrite)
    {
      foreach (string file in Directory.GetFiles(sourceDir, "*.dll", SearchOption.TopDirectoryOnly))
      {
        string dest = Path.Combine(runtimeTarget, Path.GetFileName(file));
        File.Copy(file, dest, overwrite: overwrite);
      }
    }
  }
}
