using AIStudio.Common;
using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace AIStudio.ViewModels
{
  /// <summary>Общая логика удаления выделенных сессий в окне выбора.</summary>
  internal static class LogSessionPickerDeleteOperations
  {
    public static bool TryDeleteSelected(
        ObservableCollection<LiveLogSessionPickerItem> items,
        Func<IEnumerable<int>, (bool ok, string error)> deleteSessions,
        Action reloadItems,
        Window owner)
    {
      if (!LogSessionPickerGate.EnsurePulsationStopped(owner))
        return false;

      var checkedItems = items.Where(i => i.IsChecked).ToList();
      if (checkedItems.Count == 0)
      {
        MessageBox.Show(
            owner,
            "Ничего не выделено.",
            "Удаление сессий",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
      }

      if (!items.Any(i => !i.IsCurrent))
      {
        MessageBox.Show(
            owner,
            "Файл логов ещё не сохранён — удалять нечего.",
            "Удаление сессий",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
      }

      bool currentChecked = checkedItems.Any(i => i.IsCurrent);
      var deletable = checkedItems.Where(i => !i.IsCurrent).ToList();

      if (deletable.Count == 0)
      {
        MessageBox.Show(
            owner,
            "Текущую сессию удалить нельзя.\n\nДругих выделенных сессий нет — ничего не будет удалено.",
            "Удаление сессий",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
      }

      if (currentChecked)
      {
        MessageBox.Show(
            owner,
            "Текущую сессию удалить нельзя.",
            "Удаление сессий",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }

      var confirm = MessageBox.Show(
          owner,
          "Вы точно хотите удалить выделенные файлы логов?\nОтменить изменения будет невозможно.",
          "Удаление сессий",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);
      if (confirm != MessageBoxResult.Yes)
        return false;

      var indices = new List<int>();
      foreach (var item in deletable)
      {
        if (!int.TryParse(item.SessionKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ix))
        {
          MessageBox.Show(
              owner,
              "Не удалось определить индекс сессии для удаления.",
              "Удаление сессий",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }

        indices.Add(ix);
      }

      var (ok, error) = deleteSessions(indices);
      if (!ok)
      {
        MessageBox.Show(
            owner,
            string.IsNullOrWhiteSpace(error) ? "Не удалось удалить сессии." : error,
            "Удаление сессий",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
      }

      reloadItems();
      return true;
    }
  }
}
