using AIStudio.Common.Adapters;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Доступ к редакторам меню «Среда» (требуется <c>AdapterId</c> в проекте, фаза 4).
  /// </summary>
  public static class SymbiontEnvironmentGate
  {
    /// <summary>Сообщение при блокировке редакторов.</summary>
    public const string BlockedMessage =
        "Редакторы «Среда» доступны только если в проекте указан тип среды (AdapterId).\n\n" +
        "Гомеостаз и пульт оператора работают без среды.\n\n" +
        "Зарегистрируйте пакет (Проект → Зарегистрированные пакеты…) и укажите AdapterId в Settings\\Settings.xml " +
        "или создайте проект симбионта с выбором типа среды.";

    /// <summary>
    /// Проверяет, что в активном проекте задан AdapterId.
    /// </summary>
    public static bool IsEnvironmentEditingAllowed()
    {
      return SymbiontProjectAdapterSettings.TryGetCurrentAdapterId(out _);
    }
  }
}
