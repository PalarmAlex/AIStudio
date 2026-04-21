using ISIDA.Research;

namespace AIStudio.Common
{
  /// <summary>Примеры входного JSON для прогонов (UTF-8, поля в snake_case).</summary>
  public static class ResearchHarnessSampleInputs
  {
    public static string GetJson(string harnessId)
    {
      if (harnessId == HomeostasisHarnessIds.AnyVitalHarmfulZone)
        return AnyVitalJson;
      if (harnessId == HomeostasisHarnessIds.ExternalImpactCriticalFlags)
        return ExternalFlagsJson;
      if (harnessId == HomeostasisHarnessIds.CalculateUrgencyFunction)
        return UrgencyJson;
      if (harnessId == HomeostasisHarnessIds.ComputeOperatorAutomatizmAssessment)
        return OperatorJson;
      if (harnessId == HomeostasisHarnessIds.DominantAndFinalStyles)
        return DominantJson;
      return HasCriticalJson;
    }

    private const string HasCriticalJson = @"{
  ""schema_version"": ""1.0"",
  ""harness_id"": ""homeostasis.has_critical_parameter_changes"",
  ""cases"": [
    {
      ""case_id"": ""vital_worsens_deficit"",
      ""current"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 40, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ],
      ""previous"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 50, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ]
    },
    {
      ""case_id"": ""non_vital_only"",
      ""current"": [
        { ""id"": 2, ""name"": ""X"", ""value"": 10, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": false, ""criticalMin"": 0, ""criticalMax"": 100 }
      ],
      ""previous"": [
        { ""id"": 2, ""name"": ""X"", ""value"": 50, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": false, ""criticalMin"": 0, ""criticalMax"": 100 }
      ]
    }
  ]
}";

    private const string AnyVitalJson = @"{
  ""schema_version"": ""1.0"",
  ""harness_id"": ""homeostasis.any_vital_harmful_zone"",
  ""cases"": [
    {
      ""case_id"": ""vital_below_norma"",
      ""parameters"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 40, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ]
    },
    {
      ""case_id"": ""vital_ok"",
      ""parameters"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 55, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ]
    }
  ]
}";

    private const string ExternalFlagsJson = @"{
  ""schema_version"": ""1.0"",
  ""harness_id"": ""homeostasis.external_impact_critical_flags"",
  ""cases"": [
    {
      ""case_id"": ""threshold_only"",
      ""parameters"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 48, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ],
      ""external_influences"": { ""1"": -6 }
    },
    {
      ""case_id"": ""threshold_and_orientation"",
      ""parameters"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 48, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ],
      ""external_influences"": { ""1"": -12 }
    }
  ]
}";

    private const string UrgencyJson = @"{
  ""schema_version"": ""1.0"",
  ""harness_id"": ""homeostasis.calculate_urgency_function"",
  ""cases"": [
    {
      ""case_id"": ""deficit"",
      ""parameters"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 30, ""weight"": 80, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ]
    }
  ]
}";

    private const string OperatorJson = @"{
  ""schema_version"": ""1.0"",
  ""harness_id"": ""homeostasis.compute_operator_automatizm_assessment"",
  ""cases"": [
    {
      ""case_id"": ""vital_worsens"",
      ""focus_parameter_id"": 0,
      ""overall_before"": 0,
      ""overall_after"": 0,
      ""values_before"": { ""1"": 50.0 },
      ""parameters"": [
        { ""id"": 1, ""name"": ""Energy"", ""value"": 40, ""weight"": 50, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ]
    }
  ]
}";

    private const string DominantJson = @"{
  ""schema_version"": ""1.0"",
  ""harness_id"": ""homeostasis.dominant_and_final_styles"",
  ""cases"": [
    {
      ""case_id"": ""single_param"",
      ""dynamic_time"": 5,
      ""dif_sensor_par"": 0.5,
      ""base_style_ids"": [9101, 9102, 9103],
      ""style_activations"": ""4:9101;5:9102;6:9103"",
      ""parameters"": [
        { ""id"": 1, ""name"": ""P1"", ""value"": 48, ""weight"": 55, ""normaWell"": 50, ""speed"": -10, ""isVital"": true, ""criticalMin"": 0, ""criticalMax"": 100 }
      ]
    }
  ]
}";
  }
}
