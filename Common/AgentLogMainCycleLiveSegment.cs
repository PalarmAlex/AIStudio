using System.Windows.Media;

namespace AIStudio.Common
{
  /// <summary>Фрагмент колонки «Цикл М» в живых логах: номер и цвет по статусу (яркие цвета на тёмно-зелёном фоне таблицы).</summary>
  public sealed class AgentLogMainCycleLiveSegment
  {
    private static readonly Brush BrightRed = CreateFrozen(255, 90, 90);
    private static readonly Brush BrightYellow = CreateFrozen(255, 230, 70);
    private static readonly Brush BrightGreen = CreateFrozen(110, 255, 130);

    private static SolidColorBrush CreateFrozen(byte r, byte g, byte b)
    {
      var br = new SolidColorBrush(Color.FromRgb(r, g, b));
      br.Freeze();
      return br;
    }

    /// <summary>Создаёт сегмент отображения.</summary>
    /// <param name="id">Номер экземпляра цикла.</param>
    /// <param name="taskStatus">Awaiting / NoSolution / Solved / Completed.</param>
    /// <param name="showLeadingComma">Показать запятую перед номером (не первый в списке).</param>
    public AgentLogMainCycleLiveSegment(int id, string taskStatus, bool showLeadingComma)
    {
      Id = id;
      TaskStatus = taskStatus ?? "";
      ShowLeadingComma = showLeadingComma;
      IdForeground = BrushForStatus(TaskStatus);
    }

    /// <summary>Номер цикла.</summary>
    public int Id { get; }

    /// <summary>Статус задачи цикла.</summary>
    public string TaskStatus { get; }

    /// <summary>Запятая-разделитель перед этим номером.</summary>
    public bool ShowLeadingComma { get; }

    /// <summary>Цвет номера.</summary>
    public Brush IdForeground { get; }

    private static Brush BrushForStatus(string st)
    {
      if (string.IsNullOrEmpty(st))
        return BrightRed;
      if (st == "Awaiting")
        return BrightYellow;
      if (st == "Solved" || st == "Completed")
        return BrightGreen;
      return BrightRed;
    }
  }
}
