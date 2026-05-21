namespace AIStudio.ViewModels
{
  public sealed class SensorTreesPageLabels
  {
    public string ChannelTabTitle { get; }
    public string TokenTreeHeader { get; }
    public string PatternTreeHeader { get; }
    public string TokenCountWord { get; }
    public string PatternCountWord { get; }

    public SensorTreesPageLabels(
        string channelTabTitle,
        string tokenTreeHeader,
        string patternTreeHeader,
        string tokenCountWord,
        string patternCountWord)
    {
      ChannelTabTitle = channelTabTitle;
      TokenTreeHeader = tokenTreeHeader;
      PatternTreeHeader = patternTreeHeader;
      TokenCountWord = tokenCountWord;
      PatternCountWord = patternCountWord;
    }

    public static readonly SensorTreesPageLabels Verbal = new SensorTreesPageLabels(
        "Речь",
        "Дерево токенов",
        "Дерево паттернов",
        "токенов",
        "паттернов");

    public static readonly SensorTreesPageLabels Command = new SensorTreesPageLabels(
        "Команды",
        "Контуры",
        "Группы контуров",
        "контуров",
        "групп");
  }
}
