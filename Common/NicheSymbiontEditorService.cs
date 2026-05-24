using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using ISIDA.Niche;
using ISIDA.Reflexes;
using System;

namespace AIStudio.Common
{
  /// <summary>
  /// Редакторы данных универсального симбионта Niche (Data/Niche/) вне пульсации Creature.
  /// </summary>
  public static class NicheSymbiontEditorService
  {
    private static NicheSymbiontContext _editorContext;
    private static readonly object _lock = new object();

    /// <summary>Открытый контекст редактора (если создан).</summary>
    public static NicheSymbiontContext EditorContext
    {
      get
      {
        lock (_lock)
          return _editorContext;
      }
    }

    /// <summary>
    /// Создаёт или возвращает контекст Niche для Studio-редакторов.
    /// </summary>
    public static NicheSymbiontContext GetOrCreateEditorContext(RoleProfile roleProfile = null)
    {
      lock (_lock)
      {
        if (_editorContext != null)
          return _editorContext;

        string nicheRoot = TriadProjectPaths.GetNicheDataFolder();
        _editorContext = new NicheSymbiontContext();
        _editorContext.Initialize(nicheRoot, roleProfile ?? RoleProfile.NicheStage0, null);
        return _editorContext;
      }
    }

    /// <summary>Сбрасывает кэш редактора (после смены проекта).</summary>
    public static void ResetEditorContext()
    {
      lock (_lock)
      {
        _editorContext?.Dispose();
        _editorContext = null;
      }
    }

    /// <summary>ViewModel параметров гомеостаза Niche.</summary>
    public static SystemParametersViewModel CreateGomeostasViewModel()
    {
      var ctx = GetOrCreateEditorContext();
      return new SystemParametersViewModel(
          ctx.Gomeostas,
          agentTitle: "Параметры гомеостаза Niche (среда)",
          forceNicheEditorMode: true);
    }

    /// <summary>ViewModel безусловных рефлексов Niche.</summary>
    public static GeneticReflexesViewModel CreateGeneticReflexesViewModel()
    {
      var ctx = GetOrCreateEditorContext();
      return new GeneticReflexesViewModel(
          ctx.Gomeostas,
          ctx.GeneticReflexes,
          ctx.AdaptiveActions,
          ctx.InfluenceActions,
          reflexTreeSystem: null,
          reflexChainsSystem: null,
          reflexFileLoader: null,
          bootDataFolder: null,
          nicheSymbiontEditorMode: true);
    }
  }
}
