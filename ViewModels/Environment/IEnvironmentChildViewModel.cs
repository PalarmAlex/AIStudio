using System;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Дочерняя вкладка оболочки редакторов среды.</summary>
  public interface IEnvironmentChildViewModel : IDisposable
  {
    bool Dirty { get; }
    int ValidationIssueCount { get; }
    bool CanSave { get; }
    void Save();
    void Reload();
    event Action DirtyChanged;
    event Action<int> ValidationIssueCountChanged;
  }
}
