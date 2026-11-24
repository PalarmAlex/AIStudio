using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Psychic;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace AIStudio.ViewModels
{
  /// <summary>
  /// Результат экспорта карты параметров и справочника типов.
  /// </summary>
  public class ExportActionParamResult
  {
    /// <summary>Успешность экспорта.</summary>
    public bool Success { get; set; }
    /// <summary>Сообщение (путь к файлу или текст ошибки).</summary>
    public string Message { get; set; }
  }

  /// <summary>
  /// Сервис экспорта карты параметров моторных действий и справочника типов параметров
  /// в формат JSON.
  /// <para>
  /// Карта параметров строится из активных моторных действий (AdaptiveAction.Params)
  /// и справочника типов (ActionParamTypesSystem). Пустой словарь Params означает
  /// отсутствие ключей (не пустую строку).
  /// </para>
  /// </summary>
  public static class ActionParamExportService
  {
    /// <summary>
    /// Экспортирует карту параметров и справочник типов в файл JSON.
    /// </summary>
    /// <param name="actionsSystem">Система моторных действий ISIDA.</param>
    /// <param name="paramTypesSystem">Справочник типов параметров ISIDA.</param>
    /// <param name="filePath">Путь сохранения (выбирается через диалог, если null).</param>
    /// <returns>Результат экспорта (успех + путь/ошибка).</returns>
    public static ExportActionParamResult Export(
        AdaptiveActionsSystem actionsSystem,
        ActionParamTypesSystem paramTypesSystem,
        string filePath = null)
    {
      // Сбор карты параметров
      var actionParams = new List<ExportActionParamEntry>();
      if (actionsSystem != null)
      {
        try
        {
          var allActions = actionsSystem.GetAllAdaptiveActions();
          if (allActions != null)
          {
            foreach (AdaptiveActionsSystem.AdaptiveAction action in allActions)
            {
              if (action == null || action.Id <= 0)
                continue;

              actionParams.Add(new ExportActionParamEntry
              {
                ActionId = action.Id,
                ActionName = action.Name ?? string.Empty,
                Description = action.Description ?? string.Empty,
                Params = action.Params != null && action.Params.Count > 0
                    ? new Dictionary<string, string>(action.Params, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>()
              });
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Error("Экспорт карты параметров (действия): " + ex.Message);
        }
      }

      // Сбор справочника типов
      var paramTypes = new List<ExportParamTypeRecord>();
      if (paramTypesSystem != null)
      {
        try
        {
          var allTypes = paramTypesSystem.GetAll();
          if (allTypes != null)
          {
            foreach (ActionParamTypeRecord rec in allTypes)
            {
              if (rec == null)
                continue;

              paramTypes.Add(new ExportParamTypeRecord
              {
                Id = rec.Id,
                Name = rec.Name ?? string.Empty,
                Type = rec.Type ?? "text"
              });
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Error("Экспорт справочника типов: " + ex.Message);
        }
      }

      // Формирование корневого объекта
      var exportData = new ExportActionParamData
      {
        ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        ActionParams = actionParams,
        ParamTypes = paramTypes
      };

      string json;
      try
      {
        json = JsonConvert.SerializeObject(exportData, Formatting.Indented, new JsonSerializerSettings
        {
          NullValueHandling = NullValueHandling.Ignore,
          Culture = CultureInfo.InvariantCulture
        });
      }
      catch (Exception ex)
      {
        return new ExportActionParamResult
        {
          Success = false,
          Message = "Ошибка сериализации JSON: " + ex.Message
        };
      }

      // Сохранение
      string savePath = filePath;
      if (string.IsNullOrWhiteSpace(savePath))
      {
        var dlg = new SaveFileDialog
        {
          Filter = "Файл параметров (*.json)|*.json|Все файлы|*.*",
          FileName = "action_params_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".json",
          Title = "Экспорт карты параметров и справочника типов"
        };

        if (dlg.ShowDialog() != true)
          return new ExportActionParamResult
          {
            Success = false,
            Message = "Экспорт отменён пользователем"
          };

        savePath = dlg.FileName;
      }

      try
      {
        File.WriteAllText(savePath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return new ExportActionParamResult
        {
          Success = true,
          Message = "Экспортировано в: " + savePath
        };
      }
      catch (Exception ex)
      {
        return new ExportActionParamResult
        {
          Success = false,
          Message = "Ошибка записи файла: " + ex.Message
        };
      }
    }
  }

  /// <summary>
  /// Запись карты параметров для одного моторного действия.
  /// </summary>
  public class ExportActionParamEntry
  {
    /// <summary>ID моторного действия (AdaptiveAction.Id).</summary>
    public int ActionId { get; set; }
    /// <summary>Название действия.</summary>
    public string ActionName { get; set; }
    /// <summary>Описание действия.</summary>
    public string Description { get; set; }
    /// <summary>Параметры моторного действия из образа (ActionsImage.Params). Пустой словарь — нет параметров.</summary>
    public Dictionary<string, string> Params { get; set; }
  }

  /// <summary>
  /// Запись справочника типов параметров моторных действий.
  /// </summary>
  public class ExportParamTypeRecord
  {
    /// <summary>ID типа.</summary>
    public int Id { get; set; }
    /// <summary>Имя параметра (ключ).</summary>
    public string Name { get; set; }
    /// <summary>Тип: text/number/int/bool/enum/date.</summary>
    public string Type { get; set; }
  }

  /// <summary>
  /// Корневой объект экспорта.
  /// </summary>
  public class ExportActionParamData
  {
    /// <summary>Дата и время экспорта.</summary>
    public string ExportDate { get; set; }
    /// <summary>Карта параметров: список моторных действий с их параметрами.</summary>
    public List<ExportActionParamEntry> ActionParams { get; set; }
    /// <summary>Справочник типов параметров.</summary>
    public List<ExportParamTypeRecord> ParamTypes { get; set; }
  }
}
