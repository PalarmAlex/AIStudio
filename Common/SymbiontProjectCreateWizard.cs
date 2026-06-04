using AIStudio.Common.Adapters;
using AIStudio.Windows;
using ISIDA.SymbiontEnv.Contract;
using System.Collections.Generic;
using System.Windows;

namespace AIStudio.Common
{
  /// <summary>
  /// Мастер «Новый проект симбионта»: выбор адаптера и каталога (фаза 4).
  /// </summary>
  public static class SymbiontProjectCreateWizard
  {
    /// <summary>
    /// Запускает диалог выбора адаптера и каталога проекта.
    /// </summary>
    public static bool TryRun(Window owner, out string projectRoot, out string adapterId, out string errorMessage)
    {
      projectRoot = null;
      adapterId = null;
      errorMessage = null;

      IReadOnlyList<AdapterManifest> adapters = AdapterRegistry.GetInstalledAdapters();
      if (adapters == null || adapters.Count == 0)
      {
        errorMessage =
            "Нет установленных адаптеров.\n\n" +
            "Сначала установите пакет: Проект → Адаптеры среды → Установить из папки…";
        return false;
      }

      var dialog = new NewSymbiontProjectWindow(adapters)
      {
        Owner = owner
      };

      if (dialog.ShowDialog() != true || dialog.SelectedAdapter == null)
        return false;

      adapterId = dialog.SelectedAdapter.Id;

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
