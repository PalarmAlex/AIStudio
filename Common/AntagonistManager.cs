using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIStudio.Common
{
  public class AntagonistManager
  {
    private readonly HashSet<int> _blockedItemIds = new HashSet<int>();
    private readonly List<AntagonistItem> _allItems;
    private readonly Dictionary<int, List<int>> _antagonistsMap;
    private bool _isUpdatingSelection = false;
    private readonly Func<List<AntagonistConflict>, AntagonistItem, bool> _conflictResolutionCallback;

    public AntagonistManager(List<AntagonistItem> items,
                           Func<List<AntagonistConflict>, AntagonistItem, bool> conflictResolutionCallback = null)
    {
      _allItems = items;
      _antagonistsMap = items.ToDictionary(item => item.Id, item => item.AntagonistIds);
      _conflictResolutionCallback = conflictResolutionCallback;

      foreach (var item in items)
      {
        item.OnSelectionChanged += HandleSelectionChanged;
      }
    }

    private void HandleSelectionChanged(AntagonistItem changedItem)
    {
      if (_isUpdatingSelection) return;

      try
      {
        _isUpdatingSelection = true;

        var selectedIds = _allItems.Where(i => i.IsSelected).Select(i => i.Id).ToList();
        var conflicts = AntagonistValidator.ValidateAntagonists(selectedIds, _antagonistsMap);

        // Если есть конфликты, разрешаем их
        if (conflicts.Any())
        {
          ResolveConflicts(conflicts, changedItem);
        }

        // Обновляем блокировки и доступность
        UpdateBlockedItems(selectedIds);
        UpdateItemsAvailability();
      }
      finally
      {
        _isUpdatingSelection = false;
      }
    }

    protected virtual void ResolveConflicts(List<AntagonistConflict> conflicts, AntagonistItem recentlyChangedItem)
    {
      // Если есть callback для подтверждения конфликтов, используем его
      if (_conflictResolutionCallback != null)
      {
        bool shouldResolve = _conflictResolutionCallback(conflicts, recentlyChangedItem);
        if (!shouldResolve)
        {
          // Отменяем изменение - возвращаем предыдущее состояние
          recentlyChangedItem.IsSelected = !recentlyChangedItem.IsSelected;
          return;
        }
      }

      // Стандартная логика разрешения конфликтов (автоматическое снятие)
      var conflictIds = new HashSet<int>();
      foreach (var conflict in conflicts)
      {
        conflictIds.Add(conflict.FirstId);
        conflictIds.Add(conflict.SecondId);
      }

      var itemsToDeselect = _allItems
          .Where(item => conflictIds.Contains(item.Id) &&
                        item.Id != recentlyChangedItem.Id &&
                        item.IsSelected)
          .ToList();

      foreach (var item in itemsToDeselect)
      {
        item.IsSelected = false;
      }
    }

    private void UpdateBlockedItems(List<int> selectedIds)
    {
      _blockedItemIds.Clear();

      foreach (var selectedId in selectedIds)
      {
        if (_antagonistsMap.TryGetValue(selectedId, out var antagonists))
        {
          foreach (var antagonistId in antagonists)
          {
            _blockedItemIds.Add(antagonistId);
          }
        }
      }
    }

    private void UpdateItemsAvailability()
    {
      foreach (var item in _allItems)
      {
        item.IsEnabled = !_blockedItemIds.Contains(item.Id) || item.IsSelected;
      }
    }

    public void Dispose()
    {
      foreach (var item in _allItems)
      {
        item.OnSelectionChanged -= HandleSelectionChanged;
      }
    }
  }
}