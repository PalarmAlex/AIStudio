using ISIDA.Gomeostas;
using System;
using System.Windows;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Привязка симбионта к зарегистрированному пакету среды (<c>AdapterId</c> в AgentProperties.dat).
  /// </summary>
  public static class SymbiontProjectAdapterSettings
  {
    /// <summary>Legacy: элемент в Settings.xml до миграции в AgentProperties.dat.</summary>
    internal const string LegacySettingsXmlElementName = "AdapterId";

    /// <summary>
    /// AdapterId активного проекта из <c>AgentProperties.dat</c>.
    /// </summary>
    public static bool TryGetCurrentAdapterId(out string adapterId)
    {
      adapterId = string.Empty;
      string agentPropertiesPath = AgentPropertiesAdapterBinding.GetActiveAgentPropertiesPath();
      if (string.IsNullOrWhiteSpace(agentPropertiesPath))
        return false;
      if (!AgentPropertiesAdapterBinding.TryReadAdapterId(agentPropertiesPath, out adapterId))
        return false;
      adapterId = (adapterId ?? string.Empty).Trim();
      return adapterId.Length > 0;
    }

    /// <summary>
    /// AdapterId с проверкой наличия пакета в <see cref="AdapterPaths.AdaptersRootPath"/>.
    /// </summary>
    public static bool TryGetValidatedCurrentAdapterId(out string adapterId)
    {
      if (!TryGetCurrentAdapterId(out adapterId))
        return false;
      return AdapterRegistry.TryGetById(adapterId) != null;
    }

    /// <summary>
    /// Проверяет AdapterId против реестра; при отсутствии пакета сбрасывает привязку.
    /// </summary>
    /// <returns>True, если адаптер задан и зарегистрирован.</returns>
    public static bool ValidateAndRepairBinding(
        GomeostasSystem gomeostas,
        string agentPropertiesPath,
        Window messageOwner,
        out bool bindingWasCleared)
    {
      bindingWasCleared = false;
      string adapterId = string.Empty;
      if (gomeostas != null)
        adapterId = (gomeostas.GetAgentState()?.AdapterId ?? string.Empty).Trim();
      else if (!string.IsNullOrWhiteSpace(agentPropertiesPath))
        AgentPropertiesAdapterBinding.TryReadAdapterId(agentPropertiesPath, out adapterId);
      adapterId = (adapterId ?? string.Empty).Trim();
      if (adapterId.Length == 0)
      {
        ClearBinding(gomeostas, agentPropertiesPath);
        return false;
      }
      if (AdapterRegistry.TryGetById(adapterId) != null)
        return true;

      string removedId = adapterId;
      ClearBinding(gomeostas, agentPropertiesPath);
      bindingWasCleared = true;
      MessageBox.Show(
          messageOwner,
          "Пакет среды «" + removedId + "» не найден в каталоге:\n"
          + AdapterPaths.AdaptersRootPath
          + "\n\nПривязка адаптера сброшена. Зарегистрируйте пакет или выберите другой в свойствах симбионта.",
          "Тип среды",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      return false;
    }

    /// <summary>
    /// Записывает AdapterId в AgentProperties.dat и синхронизирует состояние gomeostas.
    /// </summary>
    public static void WriteAdapterIdToAgentProperties(
        GomeostasSystem gomeostas,
        string agentPropertiesPath,
        string adapterId)
    {
      string trimmed = string.IsNullOrWhiteSpace(adapterId) ? null : adapterId.Trim();
      if (gomeostas != null)
        gomeostas.SetAdapterId(trimmed ?? string.Empty);
      if (!string.IsNullOrWhiteSpace(agentPropertiesPath))
        AgentPropertiesAdapterBinding.WriteAdapterId(agentPropertiesPath, trimmed);
    }

    private static void ClearBinding(GomeostasSystem gomeostas, string agentPropertiesPath)
    {
      if (gomeostas != null)
        gomeostas.SetAdapterId(string.Empty);
      if (!string.IsNullOrWhiteSpace(agentPropertiesPath))
        AgentPropertiesAdapterBinding.WriteAdapterId(agentPropertiesPath, null);
    }
  }
}
