using AIStudio.Common.Adapters;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Доступ к редакторам меню «Среда» (требуется зарегистрированный AdapterId в свойствах симбионта).
  /// </summary>
  public static class SymbiontEnvironmentGate
  {
    /// <summary>Сообщение при блокировке редакторов.</summary>
    public const string BlockedMessage =
        "Редакторы «Среда» доступны только если в свойствах симбионта указан зарегистрированный пакет среды.\n\n" +
        "Гомеостаз и пульт оператора работают без среды.\n\n" +
        "Зарегистрируйте пакет (Проект → Зарегистрированные пакеты…) и выберите тип среды в свойствах симбионта " +
        "(или при создании проекта с типом среды).";
    /// <summary>
    /// Проверяет, что в активном проекте задан и зарегистрирован AdapterId.
    /// </summary>
    public static bool IsEnvironmentEditingAllowed()
    {
      return SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out _);
    }
  }
}
