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
  }
}
