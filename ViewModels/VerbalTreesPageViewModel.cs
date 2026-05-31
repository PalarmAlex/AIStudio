using AIStudio.Common;
using ISIDA.Gomeostas;
using ISIDA.Sensors;

namespace AIStudio.ViewModels
{
  /// <summary>Обёртка страницы сенсорных деревьев: заголовок и две вкладки.</summary>
  public sealed class VerbalTreesPageViewModel
  {
    private readonly GomeostasSystem _gomeostas;

    public VerbalTreesViewModel VerbalViewModel { get; }
    public VerbalTreesViewModel CommandViewModel { get; }

    public string CurrentAgentTitle => SymbiontPageTitleFormatter.Format("Сенсорные деревья", _gomeostas);

    public VerbalTreesPageViewModel(GomeostasSystem gomeostas, SensorySystem sensorySystem)
    {
      _gomeostas = gomeostas ?? throw new System.ArgumentNullException(nameof(gomeostas));
      if (sensorySystem == null) throw new System.ArgumentNullException(nameof(sensorySystem));

      VerbalViewModel = new VerbalTreesViewModel(gomeostas, sensorySystem.VerbalChannel, SensorTreesPageLabels.Verbal);
      CommandViewModel = new VerbalTreesViewModel(gomeostas, sensorySystem.CommandChannel, SensorTreesPageLabels.Command);
    }
  }
}
