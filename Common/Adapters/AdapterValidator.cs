using System.Collections.Generic;
using System.Linq;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Проверка пакета адаптера в UI студии (делегирует <see cref="AdapterPackageValidator"/>).
  /// </summary>
  public static class AdapterValidator
  {
    /// <summary>
    /// Проверяет каталог пакета или установленную копию в Adapters.
    /// </summary>
    public static IReadOnlyList<AdapterValidationMessage> Validate(string packageRoot)
    {
      return AdapterPackageValidator.Validate(packageRoot)
          .Select(ToStudioMessage)
          .ToList();
    }

    /// <summary>Есть ли ошибки в отчёте.</summary>
    public static bool HasErrors(IReadOnlyList<AdapterValidationMessage> messages)
    {
      return messages != null && messages.Any(m => m.Severity == AdapterValidationSeverity.Error);
    }

    private static AdapterValidationMessage ToStudioMessage(ValidationMessage message)
    {
      return new AdapterValidationMessage(MapSeverity(message.Severity), message.Text);
    }

    private static AdapterValidationSeverity MapSeverity(ValidationSeverity severity)
    {
      switch (severity)
      {
        case ValidationSeverity.Warning:
          return AdapterValidationSeverity.Warning;
        case ValidationSeverity.Error:
          return AdapterValidationSeverity.Error;
        default:
          return AdapterValidationSeverity.Info;
      }
    }
  }
}
