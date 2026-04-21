using ISIDA.Research;

namespace AIStudio.Common
{
  /// <summary>Описание метода для прогона в формате «строки с разделителем |» (карточка + пример данных).</summary>
  public sealed class ResearchHarnessPipeMethodInfo
  {
    public ResearchHarnessPipeMethodInfo(
        string harnessId,
        string title,
        string cardDescription,
        string pipeFormatLine,
        string[] columnLabels,
        string defaultSampleText)
    {
      HarnessId = harnessId;
      Title = title;
      CardDescription = cardDescription;
      PipeFormatLine = pipeFormatLine;
      ColumnLabels = columnLabels;
      DefaultSampleText = defaultSampleText;
    }

    /// <summary>Идентификатор прогона (как в <see cref="HomeostasisHarnessIds"/>).</summary>
    public string HarnessId { get; }

    /// <summary>Краткое имя для списка выбора.</summary>
    public string Title { get; }

    /// <summary>Текст карточки: что делает метод, какие поля.</summary>
    public string CardDescription { get; }

    /// <summary>Одна строка-подсказка: имена колонок через |.</summary>
    public string PipeFormatLine { get; }

    public string[] ColumnLabels { get; }

    public string DefaultSampleText { get; }

    public int ColumnCount => ColumnLabels.Length;

    /// <summary>Два встроенных прогона гомеостаза.</summary>
    public static ResearchHarnessPipeMethodInfo[] All { get; } =
    {
      new ResearchHarnessPipeMethodInfo(
          HomeostasisHarnessIds.HasCriticalParameterChanges,
          "Ухудшение жизненно важных параметров (HasCriticalParameterChanges)",
          "Проверяет, было ли за один шаг значительное ухудшение хотя бы одного жизненно важного параметра по сравнению с предыдущим снимком.\n" +
          "Вход: один параметр в «текущем» и «предыдущем» состоянии (значение, вес, норма, скорость, жизненная важность, критические границы).\n" +
          "Первая колонка — числовой id параметра (как в модели), отдельное текстовое имя кейса не используется.\n" +
          "Выход Out1: ожидаемый результат метода (0/1): было ли критическое ухудшение.",
          "id параметра|текущее значение|предыдущее значение|вес|норма|скорость|жизненно важен|крит.мин|крит.макс|Out1 ожидание",
          new[]
          {
            "P1 id параметра (целое)",
            "P2 текущее значение (число)",
            "P3 предыдущее значение (число)",
            "P4 вес (целое)",
            "P5 норма (целое)",
            "P6 скорость (целое, отрицательная — дефицит)",
            "P7 жизненно важен (0/1, да/нет)",
            "P8 критический минимум (число)",
            "P9 критический максимум (число)",
            "P10 Out1 ожидаемый результат (0/1)"
          },
          "1|40|50|50|50|-10|1|0|100|1\n" +
          "2|10|50|50|50|-10|0|0|100|0"),

      new ResearchHarnessPipeMethodInfo(
          HomeostasisHarnessIds.AnyVitalHarmfulZone,
          "Опасная зона для жизненно важных (AnyVitalParameterInHarmfulZone)",
          "Проверяет, находится ли хотя бы один жизненно важный параметр в зоне «хуже нормы» (для дефицита — значение ниже нормы, для избытка — выше).\n" +
          "Вход: один параметр (id, значение, вес, норма, скорость, жизненная важность, критические границы).\n" +
          "Выход Out1: ожидаемый результат (0/1).",
          "id параметра|значение|вес|норма|скорость|жизненно важен|крит.мин|крит.макс|Out1 ожидание",
          new[]
          {
            "P1 id параметра (целое)",
            "P2 значение (число)",
            "P3 вес (целое)",
            "P4 норма (целое)",
            "P5 скорость (целое)",
            "P6 жизненно важен (0/1, да/нет)",
            "P7 критический минимум (число)",
            "P8 критический максимум (число)",
            "P9 Out1 ожидаемый результат (0/1)"
          },
          "1|40|50|50|-10|1|0|100|1\n" +
          "1|55|50|50|-10|1|0|100|0")
    };
  }
}
