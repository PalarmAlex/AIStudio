using System;
using System.Collections.Generic;
using System.IO;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Реестр установленных адаптеров в <see cref="AdapterPaths.AdaptersRootPath"/>.
  /// </summary>
  public static class AdapterRegistry
  {
    private static readonly object Sync = new object();
    private static List<AdapterManifest> _cache;
    private static DateTime _cacheAdaptersRootUtc = DateTime.MinValue;

    /// <summary>
    /// Возвращает установленные адаптеры (сканирование с кэшем по mtime корня Adapters).
    /// </summary>
    public static IReadOnlyList<AdapterManifest> GetInstalledAdapters()
    {
      lock (Sync)
      {
        AdapterPaths.EnsureAdaptersRoot();
        DateTime rootUtc = GetDirectoryWriteTimeUtc(AdapterPaths.AdaptersRootPath);

        if (_cache != null && rootUtc == _cacheAdaptersRootUtc)
          return _cache;

        var list = new List<AdapterManifest>();
        foreach (string dir in Directory.GetDirectories(AdapterPaths.AdaptersRootPath))
        {
          if (!AdapterManifest.TryLoad(dir, out AdapterManifest manifest, out _))
            continue;

          list.Add(manifest);
        }

        list.Sort((a, b) => string.Compare(
            a?.DisplayName ?? a?.Id,
            b?.DisplayName ?? b?.Id,
            StringComparison.CurrentCultureIgnoreCase));

        _cache = list;
        _cacheAdaptersRootUtc = rootUtc;
        return _cache;
      }
    }

    /// <summary>
    /// Ищет установленный адаптер по <c>id</c>.
    /// </summary>
    public static AdapterManifest TryGetById(string adapterId)
    {
      if (string.IsNullOrWhiteSpace(adapterId))
        return null;

      string id = adapterId.Trim();
      foreach (AdapterManifest manifest in GetInstalledAdapters())
      {
        if (string.Equals(manifest.Id, id, StringComparison.OrdinalIgnoreCase))
          return manifest;
      }

      return null;
    }

    /// <summary>Сбрасывает кэш (после установки или замены пакета).</summary>
    public static void InvalidateCache()
    {
      lock (Sync)
      {
        _cache = null;
        _cacheAdaptersRootUtc = DateTime.MinValue;
      }
    }

    private static DateTime GetDirectoryWriteTimeUtc(string path)
    {
      if (!Directory.Exists(path))
        return DateTime.MinValue;

      DateTime latest = Directory.GetLastWriteTimeUtc(path);
      foreach (string sub in Directory.GetDirectories(path))
      {
        DateTime subTime = GetDirectoryWriteTimeUtc(sub);
        if (subTime > latest)
          latest = subTime;
      }

      foreach (string file in Directory.GetFiles(path))
      {
        DateTime fileTime = File.GetLastWriteTimeUtc(file);
        if (fileTime > latest)
          latest = fileTime;
      }

      return latest;
    }
  }
}
