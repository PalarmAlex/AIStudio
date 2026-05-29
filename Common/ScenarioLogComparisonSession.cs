namespace AIStudio.Common
{
  /// <summary>Якорь пульса и id сценария после последнего прогона — для сравнения ожидаемых логов с MemoryLogManager.</summary>
  public static class ScenarioLogComparisonSession
  {
    public static int? LastAnchorGlobalPulse { get; set; }
    public static int? LastScenarioId { get; set; }

    /// <summary>Последний отчёт метрик триады §13.3 (если прогон был сценарием [Triad …]).</summary>
    public static TriadValidationMetricsReport LastTriadMetricsReport { get; set; }
  }
}
