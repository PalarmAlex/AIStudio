using System;
using System.Collections.Generic;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>Тип документа среды (деталь, сборка, чертёж).</summary>
  public enum EnvironmentDocumentKind
  {
    Part,
    Assembly,
    Drawing
  }

  /// <summary>Уровень риска рецепта.</summary>
  public enum EnvironmentRecipeRiskTier
  {
    Unknown = 0,
    A,
    B,
    C
  }

  /// <summary>Шаг рецепта среды.</summary>
  public sealed class EnvironmentRecipeStepData
  {
    /// <summary>Тип шага.</summary>
    public string Type { get; set; }

    /// <summary>Параметры шага.</summary>
    public Dictionary<string, string> Parameters { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>Рецепт среды (одна запись в <c>EnvironmentRecipes.yaml</c>).</summary>
  public sealed class EnvironmentRecipeData
  {
    /// <summary>Идентификатор рецепта.</summary>
    public string Id { get; set; }

    /// <summary>ID адаптивного действия ISIDA.</summary>
    public int AdaptiveActionId { get; set; }

    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }

    /// <summary>Описание.</summary>
    public string Description { get; set; }

    /// <summary>Уровень риска.</summary>
    public EnvironmentRecipeRiskTier RiskTier { get; set; }

    /// <summary>Реактивное исполнение.</summary>
    public bool ReactiveEligible { get; set; } = true;

    /// <summary>Рекомендуемые ID воздействий.</summary>
    public List<int> RecommendedTriggerInfluenceIds { get; set; } = new List<int>();

    /// <summary>Допустимые типы документа.</summary>
    public List<EnvironmentDocumentKind> DocumentKinds { get; set; } = new List<EnvironmentDocumentKind>();

    /// <summary>Не в режиме эскиза.</summary>
    public bool NotSketchEdit { get; set; }

    /// <summary>Не read-only.</summary>
    public bool NotReadOnly { get; set; }

    /// <summary>Требуется checkout PDM.</summary>
    public bool PdmCheckoutRequired { get; set; }

    /// <summary>Шаги.</summary>
    public List<EnvironmentRecipeStepData> Steps { get; set; } = new List<EnvironmentRecipeStepData>();

    /// <summary>Метка лога после успеха.</summary>
    public string PostconditionLog { get; set; }

    /// <summary>Заметки для теста.</summary>
    public string TestNotes { get; set; }

    /// <summary>Копия для редактирования в памяти.</summary>
    public EnvironmentRecipeData Clone()
    {
      var clone = new EnvironmentRecipeData
      {
        Id = Id,
        AdaptiveActionId = AdaptiveActionId,
        DisplayName = DisplayName,
        Description = Description,
        RiskTier = RiskTier,
        ReactiveEligible = ReactiveEligible,
        NotSketchEdit = NotSketchEdit,
        NotReadOnly = NotReadOnly,
        PdmCheckoutRequired = PdmCheckoutRequired,
        PostconditionLog = PostconditionLog,
        TestNotes = TestNotes
      };

      clone.RecommendedTriggerInfluenceIds.AddRange(RecommendedTriggerInfluenceIds);
      clone.DocumentKinds.AddRange(DocumentKinds);
      foreach (EnvironmentRecipeStepData step in Steps)
      {
        clone.Steps.Add(new EnvironmentRecipeStepData
        {
          Type = step.Type,
          Parameters = new Dictionary<string, string>(step.Parameters, StringComparer.OrdinalIgnoreCase)
        });
      }

      return clone;
    }
  }

  /// <summary>Правило детекции триггера.</summary>
  public sealed class EnvironmentTriggerDetectData
  {
    /// <summary>Тип правила.</summary>
    public string Kind { get; set; }

    /// <summary>Идентификатор среды.</summary>
    public string Environment { get; set; }

    /// <summary>Включено.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>ID команд.</summary>
    public List<int> CommandIds { get; set; } = new List<int>();
  }

  /// <summary>Триггер среды.</summary>
  public sealed class EnvironmentTriggerData
  {
    /// <summary>Идентификатор триггера.</summary>
    public string Id { get; set; }

    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }

    /// <summary>ID воздействия на гомеостаз.</summary>
    public int InfluenceActionId { get; set; }

    /// <summary>Фильтр типов документа.</summary>
    public List<EnvironmentDocumentKind> DocumentKinds { get; set; } = new List<EnvironmentDocumentKind>();

    /// <summary>Правила detect.</summary>
    public List<EnvironmentTriggerDetectData> DetectRules { get; set; } = new List<EnvironmentTriggerDetectData>();

    /// <summary>Копия.</summary>
    public EnvironmentTriggerData Clone()
    {
      var clone = new EnvironmentTriggerData
      {
        Id = Id,
        DisplayName = DisplayName,
        InfluenceActionId = InfluenceActionId
      };
      clone.DocumentKinds.AddRange(DocumentKinds);
      foreach (EnvironmentTriggerDetectData rule in DetectRules)
      {
        clone.DetectRules.Add(new EnvironmentTriggerDetectData
        {
          Kind = rule.Kind,
          Environment = rule.Environment,
          Enabled = rule.Enabled,
          CommandIds = new List<int>(rule.CommandIds)
        });
      }

      return clone;
    }
  }
}
