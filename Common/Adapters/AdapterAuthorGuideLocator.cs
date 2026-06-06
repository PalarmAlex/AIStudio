using System;

using System.Diagnostics;

using System.IO;

using System.Reflection;

using System.Windows;



namespace AIStudio.Common.Adapters

{

  /// <summary>

  /// Поиск и открытие <c>AdapterAuthorGuide.html</c> (руководство автора адаптера).

  /// </summary>

  public static class AdapterAuthorGuideLocator

  {

    private const string GuideFileName = "AdapterAuthorGuide.html";

    private const string GuideTitle = "Руководство автора адаптера";



    /// <summary>

    /// Возвращает путь к HTML-руководству или <c>null</c>.

    /// </summary>

    public static string TryFindGuidePath()

    {

      string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;



      string[] candidates =

      {

        Path.Combine(baseDir, "docs", GuideFileName),

        Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\docs", GuideFileName)),

        Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\docs", GuideFileName))

      };



      foreach (string path in candidates)

      {

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))

          return path;

      }



      return null;

    }



    /// <summary>

    /// Находит HTML-руководство и открывает его в браузере по умолчанию.

    /// </summary>

    /// <returns><c>true</c>, если файл найден и запущен.</returns>

    public static bool TryOpenGuide()

    {

      string path = TryFindGuidePath();

      if (string.IsNullOrEmpty(path))

      {

        MessageBox.Show(

            "Файл " + GuideFileName + " не найден рядом с AIStudio.\nОжидается docs\\" + GuideFileName + " в каталоге приложения или репозитория.",

            GuideTitle,

            MessageBoxButton.OK,

            MessageBoxImage.Information);

        return false;

      }



      try

      {

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

        return true;

      }

      catch (Exception ex)

      {

        MessageBox.Show("Не удалось открыть файл: " + ex.Message, GuideTitle, MessageBoxButton.OK, MessageBoxImage.Warning);

        return false;

      }

    }

  }

}


