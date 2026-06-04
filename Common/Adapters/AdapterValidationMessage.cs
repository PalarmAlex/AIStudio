namespace AIStudio.Common.Adapters
{
  /// <summary>Одна строка отчёта «Проверить».</summary>
  public sealed class AdapterValidationMessage
  {
    /// <summary>
    /// Создаёт сообщение проверки.
    /// </summary>
    public AdapterValidationMessage(AdapterValidationSeverity severity, string text)
    {
      Severity = severity;
      Text = text ?? string.Empty;
    }

    /// <summary>Серьёзность.</summary>
    public AdapterValidationSeverity Severity { get; }

    /// <summary>Текст для UI.</summary>
    public string Text { get; }
  }
}
