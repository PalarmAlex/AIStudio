using ISIDA.Actions;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows;

namespace AIStudio.Converters
{
  public class IdListToNamesConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is List<int> idList && idList != null && idList.Any() && parameter is string systemType)
      {
        try
        {
          switch (systemType)
          {
            case "Level2":
              if (GomeostasSystem.IsInitialized)
              {
                var gomeostas = GomeostasSystem.Instance;
                var behaviorStyles = gomeostas.GetAllBehaviorStyles();
                var names = idList
                    .Where(id => behaviorStyles.ContainsKey(id))
                    .Select(id => behaviorStyles[id].Name)
                    .ToList();
                return string.Join(", ", names);
              }
              break;

            case "Level3":
              if (InfluenceActionSystem.IsInitialized)
              {
                var influenceSystem = InfluenceActionSystem.Instance;
                var allActions = influenceSystem.GetAllInfluenceActions();
                var names = idList
                    .Where(id => allActions.Any(a => a.Id == id))
                    .Select(id => allActions.First(a => a.Id == id).Name)
                    .ToList();
                return string.Join(", ", names);
              }
              break;

            case "AdaptiveActions":
              if (AdaptiveActionsSystem.IsInitialized)
              {
                var adaptiveSystem = AdaptiveActionsSystem.Instance;
                var allActions = adaptiveSystem.GetAllAdaptiveActions();
                var names = idList
                    .Where(id => allActions.Any(a => a.Id == id))
                    .Select(id => allActions.First(a => a.Id == id).Name)
                    .ToList();
                return string.Join(", ", names);
              }
              break;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Ошибка в IdListToNamesConverter ({systemType}): {ex.Message}");
          return string.Join(", ", idList.Select(id => $"[{id}]"));
        }
      }
      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}