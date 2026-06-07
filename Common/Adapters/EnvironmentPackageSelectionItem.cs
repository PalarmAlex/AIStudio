using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.Adapters
{
  /// <summary>Элемент списка выбора типа среды при создании проекта симбионта.</summary>
  public sealed class EnvironmentPackageSelectionItem
  {
    /// <summary>Зарегистрированный пакет; null — проект без среды.</summary>
    public AdapterManifest Manifest { get; set; }

    /// <summary>Текст в ComboBox.</summary>
    public string DisplayName { get; set; }

    /// <summary>Пункт «Нет среды».</summary>
    public static EnvironmentPackageSelectionItem None { get; } = new EnvironmentPackageSelectionItem
    {
      Manifest = null,
      DisplayName = "Нет среды"
    };

    /// <summary>Создаёт элемент из manifest пакета.</summary>
    public static EnvironmentPackageSelectionItem FromManifest(AdapterManifest manifest)
    {
      return new EnvironmentPackageSelectionItem
      {
        Manifest = manifest,
        DisplayName = manifest?.DisplayName ?? string.Empty
      };
    }
  }
}
