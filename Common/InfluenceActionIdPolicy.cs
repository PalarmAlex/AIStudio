namespace AIStudio.Common
{
  /// <summary>
  /// Диапазоны ID в <c>InfluenceActions.dat</c> (contract 3.1).
  /// </summary>
  public static class InfluenceActionIdPolicy
  {
    /// <summary>Максимальный рекомендуемый ID операторского стимула с пульта.</summary>
    public const int OperatorMaxId = 100;

    /// <summary>Нижняя граница снятого диапазона EA-прокси среды (legacy / вирт. тест на пульте).</summary>
    public const int DeprecatedEnvironmentProxyMinId = 101;

    /// <summary>Верхняя граница снятого диапазона EA-прокси среды (legacy / вирт. тест на пульте).</summary>
    public const int DeprecatedEnvironmentProxyMaxId = 1000;

    /// <summary>ID в устаревшем диапазоне EA-прокси среды (101–1000).</summary>
    public static bool IsDeprecatedEnvironmentProxyRange(int id) =>
        id >= DeprecatedEnvironmentProxyMinId && id <= DeprecatedEnvironmentProxyMaxId;

    /// <summary>Следующий свободный ID для новой записи операторского стимула (пропускает 101–1000).</summary>
    public static int AllocateNextId(int maxExistingId)
    {
      int next = maxExistingId + 1;
      if (next < 1)
        next = 1;
      if (IsDeprecatedEnvironmentProxyRange(next))
        next = DeprecatedEnvironmentProxyMaxId + 1;
      return next;
    }

    /// <summary>Следующий свободный ID для новой строки EA среды (продолжение общей нумерации).</summary>
    public static int AllocateNextEnvironmentId(int maxExistingId) => AllocateNextId(maxExistingId);
  }
}
