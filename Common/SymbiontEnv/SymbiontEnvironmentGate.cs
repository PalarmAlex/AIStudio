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
        "Редакторы «Среда» доступны только для проекта с адаптером.\n\n" +
        "Создайте новый проект (Проект → Создать проект) и выберите установленный адаптер, " +
        "либо добавьте в Settings\\Settings.xml элемента <AdapterId>ид_адаптера</AdapterId> и переключите проект.";

    /// <summary>
    /// Проверяет, что в активном проекте задан AdapterId.
    /// </summary>
    public static bool IsEnvironmentEditingAllowed()
    {
      return SymbiontProjectAdapterSettings.TryGetCurrentAdapterId(out _);
    }
  }
}
