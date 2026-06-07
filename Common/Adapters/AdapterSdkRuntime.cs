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

    /// <summary>XML-документация SDK (IntelliSense при разработке host).</summary>
    internal static readonly string[] SdkDocumentationFileNames =
    {
      "isida.xml",
      "SymbiontEnv.Contract.xml"
    };
    /// <summary>
    /// Копирует доступные файлы стартового SDK из каталога demo в <paramref name="runtimeTarget"/>.
    /// Отсутствие каталога или файлов не блокирует создание пакета — только предупреждение.
    /// </summary>
    internal static void EnsureSdk(string runtimeTarget, out string warningMessage)
    {
      warningMessage = null;
      if (string.IsNullOrWhiteSpace(runtimeTarget))
        return;
      Directory.CreateDirectory(runtimeTarget);
      string demoRuntime = Path.Combine(AdapterPaths.GetDemoTemplatePath(), "runtime");
      if (!Directory.Exists(demoRuntime))
      {
        warningMessage = "Каталог стартового SDK не найден:\n" + demoRuntime
            + "\n\nПакет создан без стартового SDK. Он должен быть создан установщиком AIStudio.";
        return;
      }
      CopySdkFilesFromDirectory(demoRuntime, runtimeTarget);
      var missing = new List<string>();
      for (int i = 0; i < RequiredSdkFileNames.Length; i++)
      {
        if (!File.Exists(Path.Combine(demoRuntime, RequiredSdkFileNames[i])))
          missing.Add(RequiredSdkFileNames[i]);
      }
      if (missing.Count > 0)
      {
        warningMessage = "В каталоге стартового SDK отсутствуют файлы: " + string.Join(", ", missing)
            + ".\n\n" + demoRuntime
            + "\n\nПакет создан без них. Переустановите AIStudio или скопируйте недостающие файлы в этот каталог.";
      }

      var missingDocs = new List<string>();
      for (int i = 0; i < SdkDocumentationFileNames.Length; i++)
      {
        if (!File.Exists(Path.Combine(demoRuntime, SdkDocumentationFileNames[i])))
          missingDocs.Add(SdkDocumentationFileNames[i]);
      }
      if (missingDocs.Count > 0)
      {
        string docWarning = "В стартовом SDK нет XML-документации: " + string.Join(", ", missingDocs)
            + ".\nIntelliSense по isida и SymbiontEnv.Contract в проекте host может быть неполным.";
        warningMessage = string.IsNullOrEmpty(warningMessage)
            ? docWarning
            : warningMessage + "\n\n" + docWarning;
      }
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
