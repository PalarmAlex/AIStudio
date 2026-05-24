using System;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace AIStudio.Common
{
  /// <summary>Выбор каталога для нового проекта (Vista folder browser).</summary>
  public static class ProjectFolderPicker
  {
    public static bool TryPickFolderForNewProject(
        Window owner,
        out string projectRoot,
        out string errorMessage)
    {
      projectRoot = null;
      errorMessage = null;

      var dialog = new VistaFolderBrowserDialog
      {
        Description = "Укажите каталог для нового проекта данных (при необходимости создайте папку кнопкой «Создать папку»)...",
        UseDescriptionForTitle = true,
        SelectedPath = ProjectBootstrap.GetDefaultProjectsFolderDialogPath()
      };

      if (dialog.ShowDialog(owner) != true)
        return false;

      if (string.IsNullOrWhiteSpace(dialog.SelectedPath))
      {
        errorMessage = "Не указан каталог проекта.";
        return false;
      }

      return ProjectBootstrap.TryEnsureFolderFromDialogSelection(
          dialog.SelectedPath,
          out projectRoot,
          out errorMessage);
    }
  }
}
