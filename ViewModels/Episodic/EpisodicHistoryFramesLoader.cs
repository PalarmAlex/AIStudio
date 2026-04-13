using ISIDA.Psychic.Memory.Episodic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace AIStudio.ViewModels.Episodic
{
  /// <summary>Загрузка ленты последних кадров эпизодической памяти (как на странице дерева).</summary>
  public static class EpisodicHistoryFramesLoader
  {
    public static ObservableCollection<HistoryFrameItem> Load(EpisodicMemorySystem episodic, EpisodicMemoryNodePresentation presentation)
    {
      var list = new ObservableCollection<HistoryFrameItem>();
      var history = episodic?.History;
      if (history == null) return list;

      var entries = history.GetLastEntries(100);
      var chains = SplitIntoChains(entries);
      for (int i = chains.Count - 1; i >= 0; i--)
      {
        var chain = chains[i];
        for (int j = chain.Count - 1; j >= 0; j--)
        {
          var e = chain[j];
          if (e.NodeId == -1)
          {
            list.Add(new HistoryFrameItem("—", Brushes.White, "Пустой кадр (разрыв цепочки правил)"));
            continue;
          }
          var node = episodic.GetNodeById(e.NodeId);
          int effect = node?.Params?.Effect ?? 0;
          Brush brush = effect < 0 ? Brushes.LightCoral : (effect > 0 ? Brushes.LightGreen : new SolidColorBrush(Color.FromRgb(0xE8, 0xC2, 0x00)));
          string tooltip = presentation.BuildFullFrameTooltip(node);
          list.Add(new HistoryFrameItem(e.NodeId.ToString(), brush, tooltip));
        }
      }
      return list;
    }

    private static List<List<EpisodicHistoryEntry>> SplitIntoChains(List<EpisodicHistoryEntry> entries)
    {
      var chains = new List<List<EpisodicHistoryEntry>>();
      if (entries == null || entries.Count == 0) return chains;
      var current = new List<EpisodicHistoryEntry>();
      foreach (var e in entries)
      {
        if (e.NodeId == -1)
        {
          if (current.Count > 0)
          {
            chains.Add(current);
            current = new List<EpisodicHistoryEntry>();
          }
          chains.Add(new List<EpisodicHistoryEntry> { e });
        }
        else
          current.Add(e);
      }
      if (current.Count > 0)
        chains.Add(current);
      return chains;
    }
  }
}
