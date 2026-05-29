using ISIDA.Common;
using Ookii.Dialogs.Wpf;
using System;
using System.Windows;
using System.Windows.Interop;

namespace AIStudio.Common
{
  /// <summary>Выбор каталога проекта с поддержкой ввода несуществующего имени в поле «Папка».</summary>
  public static class ProjectFolderPicker
  {
    public static bool TryPickFolderForNewProject(
        Window owner,
        out string projectRoot,
        out string errorMessage)
    {
      projectRoot = null;
      errorMessage = null;

      if (!TryShowFolderDialog(
              owner,
              "Укажите каталог для нового проекта данных (можно ввести имя новой папки в поле «Папка» или создать папку кнопкой «Создать папку»)...",
              ProjectBootstrap.GetDefaultProjectsFolderDialogPath(),
              out string rawPath))
      {
        return false;
      }

      return ProjectBootstrap.TryEnsureFolderFromDialogSelection(rawPath, out projectRoot, out errorMessage);
    }

    /// <summary>
    /// Открывает диалог выбора корня проекта. По умолчанию — <see cref="ProjectBootstrap.DefaultProjectsParentPath"/>.
    /// </summary>
    public static bool TryPickProjectRootFolder(
        Window owner,
        string settingsPath,
        string dataGomeostasFolderPath,
        out string projectRoot)
    {
      projectRoot = null;

      string initialPath = ProjectBootstrap.GetDefaultProjectsFolderDialogPath();
      if (SettingsValidator.TryInferProjectRoot(settingsPath, dataGomeostasFolderPath, out string currentRoot)
          && Directory.Exists(currentRoot))
      {
        initialPath = ProjectBootstrap.ToFolderDialogInitialPath(currentRoot);
      }

      if (!NativeFolderDialogInterop.TryPickFolder(
              ownerHwnd,
              initialPath,
              "Укажите каталог для нового проекта данных (можно ввести имя новой папки в поле «Папка»)...",
              out string rawPath))
      {
        return false;

      resultPath = dialog.SelectedPath;
      return !string.IsNullOrWhiteSpace(resultPath);
    }

    /// <summary>Ookii падает на невалидном <see cref="VistaFolderBrowserDialog.SelectedPath"/> — оставляем только существующий каталог с завершающим «\».</summary>
    private static string SanitizeSelectedPathForOokii(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        return string.Empty;

      try
      {
        string full = Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (Directory.Exists(full))
          return ProjectBootstrap.ToFolderDialogInitialPath(full);
      }
      catch (Exception)
      {
        // невалидный путь — диалог откроется в каталоге по умолчанию Ookii
      }

      return ProjectBootstrap.TryEnsureFolderFromDialogSelection(rawPath, out projectRoot, out errorMessage);
    }
  }
}
