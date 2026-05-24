namespace AIStudio.Common
{
  /// <summary>
  /// Источник данных справочников редактора: симбионт (Creature) или среда (Niche).
  /// </summary>
  public sealed class EditorSubjectScope
  {
    /// <summary>Симбионт-существо, каталог Data/.</summary>
    public static readonly EditorSubjectScope Symbiont = new EditorSubjectScope(
        isEnvironment: false,
        titleLabel: "Симбионта",
        genitiveLabel: "симбионта");

    /// <summary>Симбионт-среда (Niche), каталог Data/Niche/.</summary>
    public static readonly EditorSubjectScope Environment = new EditorSubjectScope(
        isEnvironment: true,
        titleLabel: "среды",
        genitiveLabel: "среды");

    private EditorSubjectScope(bool isEnvironment, string titleLabel, string genitiveLabel)
    {
      IsEnvironment = isEnvironment;
      TitleLabel = titleLabel;
      GenitiveLabel = genitiveLabel;
    }

    /// <summary>Редактирование справочников среды (Niche).</summary>
    public bool IsEnvironment { get; }

    /// <summary>Подпись для заголовка страницы («… Симбионта» / «… среды»).</summary>
    public string TitleLabel { get; }

    /// <summary>Родительный падеж для сообщений («… симбионта» / «… среды»).</summary>
    public string GenitiveLabel { get; }
  }
}
