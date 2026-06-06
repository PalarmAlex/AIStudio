using System.Collections.Generic;

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
      DocumentKindPart = true;
      DocumentKindAssembly = true;
    }

    /// <summary>Идентификатор триггера.</summary>
    public string Id { get; set; }
    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }
    /// <summary>ID воздействия на гомеостаз.</summary>
    public int InfluenceActionId { get; set; }
    /// <summary>Деталь.</summary>
    public bool DocumentKindPart { get; set; }
    /// <summary>Сборка.</summary>
    public bool DocumentKindAssembly { get; set; }
    /// <summary>Чертёж.</summary>
    public bool DocumentKindDrawing { get; set; }
    /// <summary>Правила детекции.</summary>
    public List<EnvironmentTriggerDetectRow> DetectRules { get; set; }
    /// <summary>Краткое описание detect для таблицы.</summary>
    public string DetectSummary { get; set; }
  }
}
