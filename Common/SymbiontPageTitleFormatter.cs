using ISIDA.Gomeostas;

namespace AIStudio.Common
{
  /// <summary>Единый формат заголовка страниц редакторов симбионта из главного меню.</summary>
  public static class SymbiontPageTitleFormatter
  {
    public const string UndefinedAgentName = "Не определен";
    public static string Format(string pageTitle, string agentName, int evolutionStage)
    {
      var name = string.IsNullOrWhiteSpace(agentName) ? UndefinedAgentName : agentName;
      return $"{pageTitle} симбионта: {name}. Стадия развития: {evolutionStage}.";
    }

    public static string Format(string pageTitle, GomeostasSystem gomeostas)
    {
      var agentInfo = gomeostas?.GetAgentState();
      return Format(pageTitle, agentInfo?.Name, agentInfo?.EvolutionStage ?? 0);
    }

    public static void ReadAgentContext(GomeostasSystem gomeostas, out string agentName, out int evolutionStage)
    {
      var agentInfo = gomeostas?.GetAgentState();
      agentName = agentInfo?.Name;
      evolutionStage = agentInfo?.EvolutionStage ?? 0;
    }
  }
}
