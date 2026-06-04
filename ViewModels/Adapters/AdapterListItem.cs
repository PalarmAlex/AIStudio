namespace AIStudio.ViewModels.Adapters
{
  /// <summary>Строка списка установленных адаптеров.</summary>
  public sealed class AdapterListItem
  {
    /// <summary>Идентификатор пакета.</summary>
    public string Id { get; set; }

    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }

    /// <summary>Версия пакета.</summary>
    public string Version { get; set; }

    /// <summary>Версия контракта.</summary>
    public string ContractVersion { get; set; }

    /// <summary>Каталог в Adapters.</summary>
    public string PackageRootPath { get; set; }
  }
}
