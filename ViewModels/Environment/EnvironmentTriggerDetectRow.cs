namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Правило детекции триггера среды.
  /// </summary>
  public sealed class EnvironmentTriggerDetectRow
  {
    /// <summary>Тип правила.</summary>
    public string Kind { get; set; }

    /// <summary>Идентификатор среды.</summary>
    public string Environment { get; set; }

    /// <summary>Включено.</summary>
    public bool Enabled { get; set; }

    /// <summary>ID команд (через запятую).</summary>
    public string CommandIdsText { get; set; }
  }
}
