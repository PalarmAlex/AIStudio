using ISIDA.Common;
using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Windows;

namespace AIStudio.Common
{
  /// <summary>Выбор каталога проекта через Ookii <see cref="VistaFolderBrowserDialog"/>.</summary>
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
      if (!TryShowFolderDialog(
              owner,
              "Укажите корневой каталог данных проекта...",
              initialPath,
              out string selectedPath))
      {
        return false;
      }
      projectRoot = selectedPath;
      return !string.IsNullOrWhiteSpace(projectRoot);
    }

    private static bool TryShowFolderDialog(
        Window owner,
        string description,
        string selectedPath,
        out string resultPath)
    {
      resultPath = null;
      var dialog = new VistaFolderBrowserDialog
      {
        Description = description,
        UseDescriptionForTitle = true,
        SelectedPath = SanitizeSelectedPathForOokii(selectedPath)
      };
      bool? accepted = owner != null
          ? dialog.ShowDialog(owner)
          : dialog.ShowDialog();
      if (accepted != true)
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
      return string.Empty;
    }
  }
}
