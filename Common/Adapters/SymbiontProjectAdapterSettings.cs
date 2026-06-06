using ISIDA.Common;
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
    /// <summary>Ключ в <see cref="AppConfig"/> (runtime студии).</summary>
    public const string AdapterIdElementName = "AdapterId";
    /// <summary>Legacy: элемент в Settings.xml до миграции в AgentProperties.dat.</summary>
    internal const string LegacySettingsXmlElementName = "AdapterId";
    /// <summary>
    /// AdapterId активного проекта из <see cref="AppConfig"/> (после синхронизации с AgentProperties).
    /// </summary>
    public static bool TryGetCurrentAdapterId(out string adapterId)
    {
      adapterId = (AppConfig.GetSetting(AdapterIdElementName) ?? string.Empty).Trim();
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
    /// Синхронизирует AppConfig из AgentProperties.dat (до или без загрузки Gomeostas).
    /// </summary>
    public static void SyncAppConfigFromAgentPropertiesFile(string agentPropertiesPath)
    {
      string adapterId = string.Empty;
      if (!string.IsNullOrWhiteSpace(agentPropertiesPath))
        AgentPropertiesAdapterBinding.TryReadAdapterId(agentPropertiesPath, out adapterId);
      adapterId = (adapterId ?? string.Empty).Trim();
      if (adapterId.Length > 0)
        AppConfig.SetSetting(AdapterIdElementName, adapterId);
      else
        AppConfig.SetSetting(AdapterIdElementName, string.Empty);
    }

    /// <summary>
    /// Синхронизирует AppConfig из загруженного состояния симбионта.
    /// </summary>
    public static void SyncAppConfigFromGomeostas(GomeostasSystem gomeostas)
    {
      if (gomeostas == null)
      {
        AppConfig.SetSetting(AdapterIdElementName, string.Empty);
        return;
      }
      string adapterId = (gomeostas.GetAgentState()?.AdapterId ?? string.Empty).Trim();
      if (adapterId.Length > 0)
        AppConfig.SetSetting(AdapterIdElementName, adapterId);
      else
        AppConfig.SetSetting(AdapterIdElementName, string.Empty);
    }

    /// <summary>
    /// Проверяет AdapterId против реестра; при отсутствии пакета сбрасывает привязку.
    /// </summary>
    /// <returns>True, если адаптер задан и зарегистрирован (или не задан).</returns>
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
      {
        SyncAppConfigFromGomeostas(gomeostas);
        return true;
      }
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
    /// Записывает AdapterId в AgentProperties.dat и синхронизирует AppConfig.
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
      if (trimmed != null)
        AppConfig.SetSetting(AdapterIdElementName, trimmed);
      else
        AppConfig.SetSetting(AdapterIdElementName, string.Empty);
    }

    private static void ClearBinding(GomeostasSystem gomeostas, string agentPropertiesPath)
    {
      if (gomeostas != null)
        gomeostas.SetAdapterId(string.Empty);
      if (!string.IsNullOrWhiteSpace(agentPropertiesPath))
        AgentPropertiesAdapterBinding.WriteAdapterId(agentPropertiesPath, null);
      AppConfig.SetSetting(AdapterIdElementName, string.Empty);
    }
  }
}
