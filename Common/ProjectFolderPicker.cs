using System;
using System.IO;
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

      IntPtr ownerHwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;
      string initialPath = ProjectBootstrap.GetDefaultProjectsFolderDialogPath();

      if (!NativeFolderDialogInterop.TryPickFolder(
              ownerHwnd,
              initialPath,
              "Укажите каталог для нового проекта данных (можно ввести имя новой папки в поле «Папка»)...",
              out string rawPath))
      {
        return false;
      }

      return ProjectBootstrap.TryEnsureFolderFromDialogSelection(rawPath, out projectRoot, out errorMessage);
    }
  }
}
