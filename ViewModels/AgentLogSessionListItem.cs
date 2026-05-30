using AIStudio.Common;
using System;
using System.ComponentModel;

namespace AIStudio.ViewModels
{
  /// <summary>Пункт списка сессий симбионтного лога на странице живых логов.</summary>
  public sealed class AgentLogSessionListItem : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    public string SessionId { get; }
    public bool IsCurrent { get; }

    private string _displayLabel;

    public string DisplayLabel
    {
      get => _displayLabel;
      private set
      {
        if (_displayLabel == value)
          return;
        _displayLabel = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLabel)));
      }
    }

    private AgentLogSessionListItem(string sessionId, bool isCurrent, string displayLabel)
    {
      SessionId = sessionId;
      IsCurrent = isCurrent;
      _displayLabel = displayLabel;
    }

    public static AgentLogSessionListItem CreateCurrent(int entryCount)
    {
      string suffix = entryCount > 0
          ? " (" + entryCount + ")"
          : " (пусто)";
      return new AgentLogSessionListItem(null, true, "Текущая сессия" + suffix);
    }

    public void UpdateCurrentEntryCount(int entryCount)
    {
      if (!IsCurrent)
        return;

      string suffix = entryCount > 0
          ? " (" + entryCount + ")"
          : " (пусто)";
      DisplayLabel = "Текущая сессия" + suffix;
    }

    public static AgentLogSessionListItem FromArchived(AgentLogSessionStorage.AgentLogSessionInfo info)
    {
      if (info == null)
        throw new ArgumentNullException(nameof(info));

      return new AgentLogSessionListItem(info.SessionId, false, info.BuildDisplayLabel());
    }
  }
}
