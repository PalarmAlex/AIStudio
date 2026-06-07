using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Строка таблицы триггеров среды.
  /// </summary>
  public sealed class EnvironmentTriggerRow
  {
    /// <summary>
    /// Создаёт строку с пустым списком правил detect.
    /// </summary>
    public EnvironmentTriggerRow()
    {
      DetectRules = new List<EnvironmentTriggerDetectRow>();
      FilterFields = new ObservableCollection<EnvironmentRecipePreconditionField>();
    }

    /// <summary>Идентификатор триггера.</summary>
    public string Id { get; set; }
    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }
    /// <summary>ID воздействия на гомеостаз.</summary>
    public int InfluenceActionId { get; set; }
    /// <summary>Поля фильтра (из schema/trigger-filter.json).</summary>
    public ObservableCollection<EnvironmentRecipePreconditionField> FilterFields { get; }
    /// <summary>Правила детекции.</summary>
    public List<EnvironmentTriggerDetectRow> DetectRules { get; set; }
    /// <summary>Краткое описание фильтра для таблицы.</summary>
    public string FilterSummary { get; set; }
    /// <summary>Краткое описание detect для таблицы.</summary>
    public string DetectSummary { get; set; }
  }
}
