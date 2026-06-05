using AIStudio.Common.Adapters;
using AIStudio.Windows;
using ISIDA.SymbiontEnv.Contract;
using System.Collections.Generic;
using System.Windows;

namespace AIStudio.Common
{
  /// <summary>
  /// Мастер «Новый проект симбионта»: каталог проекта и опциональный тип среды.
  /// </summary>
  public static class SymbiontProjectCreateWizard
  {
    /// <summary>
    /// Запускает диалог: опционально тип среды, затем каталог проекта.
    /// </summary>
    /// <param name="adapterId">Идентификатор зарегистрированного пакета или null, если среда не нужна.</param>
    public static bool TryRun(Window owner, out string projectRoot, out string adapterId, out string errorMessage)
    {
      projectRoot = null;
      adapterId = null;
      errorMessage = null;

      IReadOnlyList<AdapterManifest> adapters = AdapterRegistry.GetInstalledAdapters();

      var dialog = new NewSymbiontProjectWindow(adapters)
      {
        Owner = owner
      };

      if (dialog.ShowDialog() != true)
        return false;

      adapterId = dialog.SelectedAdapterId;

      if (!ProjectFolderPicker.TryPickFolderForNewProject(owner, out projectRoot, out string pickError))
      {
        if (!string.IsNullOrEmpty(pickError))
          errorMessage = pickError;
        return false;
      }

      return true;
    }
  }
}
