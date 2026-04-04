namespace AIStudio.ViewModels.Research
{
  public sealed class ScenariosHubViewModel
  {
    public ScenariosHubViewModel(ScenarioRegistryViewModel scenarios, ScenarioGroupRegistryViewModel groups)
    {
      Scenarios = scenarios;
      Groups = groups;
    }

    public ScenarioRegistryViewModel Scenarios { get; }
    public ScenarioGroupRegistryViewModel Groups { get; }
  }
}
