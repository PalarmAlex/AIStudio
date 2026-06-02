using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Чтение и запись YAML каталогов среды (совместимо с форматом Velum BootData).
  /// </summary>
  public static partial class EnvironmentYamlCodec
  {
    /// <summary>
    /// Читает все рецепты из файла (<c>recipes:</c> или один рецепт в корне — устаревший формат).
    /// </summary>
    public static List<EnvironmentRecipeData> ReadRecipes(string filePath, IList<string> errors)
    {
      if (errors == null)
        throw new ArgumentNullException(nameof(errors));

      if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
      {
        errors.Add("Файл рецептов не найден: " + filePath);
        return new List<EnvironmentRecipeData>();
      }

      try
      {
        string text = File.ReadAllText(filePath, Encoding.UTF8);
        YamlMap root = ParseDocument(text);
        if (root == null)
        {
          errors.Add("Пустой YAML: " + filePath);
          return new List<EnvironmentRecipeData>();
        }

        YamlSequence recipesSeq = GetSequence(root, "recipes");
        if (recipesSeq != null && recipesSeq.Items.Count > 0)
        {
          var list = new List<EnvironmentRecipeData>();
          foreach (YamlNode node in recipesSeq.Items)
          {
            YamlMap map = node as YamlMap;
            if (map == null)
              continue;

            string itemError;
            EnvironmentRecipeData recipe = MapToRecipe(map, filePath, out itemError);
            if (recipe == null)
            {
              if (!string.IsNullOrEmpty(itemError))
                errors.Add(itemError);
              continue;
            }

            list.Add(recipe);
          }

          return list;
        }

        string singleError;
        EnvironmentRecipeData single = MapToRecipe(root, filePath, out singleError);
        if (single == null)
        {
          if (!string.IsNullOrEmpty(singleError))
            errors.Add(singleError);
          return new List<EnvironmentRecipeData>();
        }

        return new List<EnvironmentRecipeData> { single };
      }
      catch (Exception ex)
      {
        errors.Add(filePath + ": " + ex.Message);
        return new List<EnvironmentRecipeData>();
      }
    }

    private static EnvironmentRecipeData MapToRecipe(YamlMap root, string filePath, out string error)
    {
      error = null;
      string recipeId = GetScalar(root, "id");
      if (string.IsNullOrWhiteSpace(recipeId))
        recipeId = GetScalar(root, "recipe_id");

      if (string.IsNullOrWhiteSpace(recipeId))
      {
        error = "Отсутствует id в " + filePath;
        return null;
      }

      if (!TryGetInt(root, "adaptive_action_id", out int adaptiveActionId) || adaptiveActionId <= 0)
      {
        error = "Некорректный adaptive_action_id в " + filePath;
        return null;
      }

      string displayName = GetScalar(root, "display_name");
      string description = GetScalar(root, "description");
      EnvironmentRecipeRiskTier riskTier = ParseRiskTier(GetScalar(root, "risk_tier"));
      bool reactiveEligible = TryGetBool(root, "reactive_eligible", defaultValue: true);
      ParsePreconditions(GetMap(root, "preconditions"), out List<EnvironmentDocumentKind> docKinds,
          out bool notSketch, out bool notReadOnly, out bool pdmCheckout);
      List<EnvironmentRecipeStepData> steps = ParseSteps(GetSequence(root, "steps"));

      return new EnvironmentRecipeData
      {
        Id = recipeId.Trim(),
        AdaptiveActionId = adaptiveActionId,
        DisplayName = displayName,
        Description = description,
        RiskTier = riskTier,
        ReactiveEligible = reactiveEligible,
        RecommendedTriggerInfluenceIds = new List<int>(ParseIntList(root, "recommended_trigger_influence_ids")),
        DocumentKinds = docKinds,
        NotSketchEdit = notSketch,
        NotReadOnly = notReadOnly,
        PdmCheckoutRequired = pdmCheckout,
        Steps = steps,
        PostconditionLog = GetScalar(root, "postcondition_log"),
        TestNotes = GetScalar(root, "test_notes")
      };
    }

    private static void ParsePreconditions(
        YamlMap map,
        out List<EnvironmentDocumentKind> documentKinds,
        out bool notSketchEdit,
        out bool notReadOnly,
        out bool pdmCheckoutRequired)
    {
      documentKinds = new List<EnvironmentDocumentKind>();
      notSketchEdit = false;
      notReadOnly = false;
      pdmCheckoutRequired = false;

      if (map == null)
        return;

      YamlSequence kindsSeq = GetSequence(map, "document_kinds");
      if (kindsSeq == null)
        kindsSeq = GetSequence(map, "document_types");

      documentKinds.AddRange(ParseDocumentKinds(kindsSeq));
      notSketchEdit = TryGetBool(map, "not_sketch_edit", false);
      notReadOnly = TryGetBool(map, "not_read_only", false);
      pdmCheckoutRequired = TryGetBool(map, "pdm_checkout_required", false);
    }

    private static List<EnvironmentRecipeStepData> ParseSteps(YamlSequence seq)
    {
      if (seq == null || seq.Items.Count == 0)
        return new List<EnvironmentRecipeStepData>();

      var list = new List<EnvironmentRecipeStepData>();
      foreach (YamlNode item in seq.Items)
      {
        YamlMap stepMap = item as YamlMap;
        if (stepMap == null)
          continue;

        string type = GetScalar(stepMap, "type");
        if (string.IsNullOrWhiteSpace(type))
          continue;

        type = NormalizeStepType(type);

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, YamlNode> kv in stepMap.Entries)
        {
          if (string.Equals(kv.Key, "type", StringComparison.OrdinalIgnoreCase))
            continue;
          YamlScalar scalarNode = kv.Value as YamlScalar;
          if (scalarNode != null)
            parameters[kv.Key] = scalarNode.Value;
        }

        list.Add(new EnvironmentRecipeStepData { Type = type.Trim(), Parameters = parameters });
      }

      return list;
    }

    private static List<EnvironmentDocumentKind> ParseDocumentKinds(YamlSequence seq)
    {
      if (seq == null || seq.Items.Count == 0)
        return new List<EnvironmentDocumentKind>();

      var list = new List<EnvironmentDocumentKind>();
      foreach (YamlNode node in seq.Items)
      {
        YamlScalar scalar = node as YamlScalar;
        if (scalar == null)
          continue;
        EnvironmentDocumentKind? kind = ParseDocumentKind(scalar.Value);
        if (kind.HasValue)
          list.Add(kind.Value);
      }

      return list;
    }

    private static EnvironmentDocumentKind? ParseDocumentKind(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return null;

      switch (text.Trim().ToLowerInvariant())
      {
        case "part":
          return EnvironmentDocumentKind.Part;
        case "assembly":
          return EnvironmentDocumentKind.Assembly;
        case "drawing":
          return EnvironmentDocumentKind.Drawing;
        default:
          return null;
      }
    }

    private static EnvironmentRecipeRiskTier ParseRiskTier(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return EnvironmentRecipeRiskTier.Unknown;

      switch (text.Trim().ToUpperInvariant())
      {
        case "A":
          return EnvironmentRecipeRiskTier.A;
        case "B":
          return EnvironmentRecipeRiskTier.B;
        case "C":
          return EnvironmentRecipeRiskTier.C;
        default:
          return EnvironmentRecipeRiskTier.Unknown;
      }
    }

    private static List<int> ParseIntList(YamlMap map, string key)
    {
      YamlSequence seq = GetSequence(map, key);
      if (seq == null || seq.Items.Count == 0)
        return new List<int>();

      var list = new List<int>();
      foreach (YamlNode node in seq.Items)
      {
        YamlScalar scalar = node as YamlScalar;
        if (scalar == null)
          continue;
        if (int.TryParse(scalar.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
          list.Add(id);
      }

      return list;
    }

    private static string GetScalar(YamlMap map, string key)
    {
      if (map == null)
        return string.Empty;
      YamlScalar scalar = map.Entries.TryGetValue(key, out YamlNode node) ? node as YamlScalar : null;
      return scalar?.Value ?? string.Empty;
    }

    private static YamlMap GetMap(YamlMap map, string key)
    {
      if (map == null)
        return null;
      return map.Entries.TryGetValue(key, out YamlNode node) ? node as YamlMap : null;
    }

    private static YamlSequence GetSequence(YamlMap map, string key)
    {
      if (map == null)
        return null;
      return map.Entries.TryGetValue(key, out YamlNode node) ? node as YamlSequence : null;
    }

    private static bool TryGetBool(YamlMap map, string key, bool defaultValue)
    {
      string text = GetScalar(map, key);
      if (string.IsNullOrWhiteSpace(text))
        return defaultValue;
      if (bool.TryParse(text, out bool b))
        return b;
      if (text == "1")
        return true;
      if (text == "0")
        return false;
      return defaultValue;
    }

    private static bool TryGetInt(YamlMap map, string key, out int value)
    {
      value = 0;
      string text = GetScalar(map, key);
      return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    #region YAML tree

    internal abstract class YamlNode
    {
    }

    internal sealed class YamlScalar : YamlNode
    {
      public YamlScalar(string value) { Value = value ?? string.Empty; }
      public string Value { get; }
    }

    internal sealed class YamlMap : YamlNode
    {
      public YamlMap() { Entries = new Dictionary<string, YamlNode>(StringComparer.OrdinalIgnoreCase); }
      public Dictionary<string, YamlNode> Entries { get; }
    }

    internal sealed class YamlSequence : YamlNode
    {
      public YamlSequence() { Items = new List<YamlNode>(); }
      public List<YamlNode> Items { get; }
    }

    private sealed class YamlParseFrame
    {
      public YamlParseFrame(int indent, YamlNode container)
      {
        Indent = indent;
        Container = container;
      }

      public int Indent { get; }
      public YamlNode Container { get; }
    }

    private static YamlMap ParseDocument(string text)
    {
      var root = new YamlMap();
      var stack = new Stack<YamlParseFrame>();
      stack.Push(new YamlParseFrame(-1, root));

      string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
      foreach (string rawLine in lines)
      {
        string line = StripComment(rawLine);
        if (string.IsNullOrWhiteSpace(line))
          continue;

        int indent = CountIndent(rawLine);
        PopToIndent(stack, indent);

        YamlParseFrame frame = stack.Peek();
        string trimmed = line.Trim();

        if (trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
          YamlSequence seq = frame.Container as YamlSequence;
          if (seq == null)
            continue;

          string itemText = trimmed.Substring(2).Trim();
          if (itemText.StartsWith("{", StringComparison.Ordinal) && itemText.EndsWith("}", StringComparison.Ordinal))
          {
            seq.Items.Add(ParseInlineMap(itemText));
            continue;
          }

          int colon = itemText.IndexOf(':');
          if (colon < 0)
          {
            seq.Items.Add(new YamlScalar(Unquote(itemText)));
            continue;
          }

          string itemKey = itemText.Substring(0, colon).Trim();
          string rest = itemText.Substring(colon + 1).Trim();
          var itemMap = new YamlMap();
          seq.Items.Add(itemMap);
          if (rest.Length == 0)
          {
            stack.Push(new YamlParseFrame(indent + 2, itemMap));
            continue;
          }

          itemMap.Entries[itemKey] = ParseScalarValue(rest);
          stack.Push(new YamlParseFrame(indent + 2, itemMap));
          continue;
        }

        int keyColon = trimmed.IndexOf(':');
        if (keyColon < 0)
          continue;

        string key = trimmed.Substring(0, keyColon).Trim();
        string valuePart = trimmed.Substring(keyColon + 1).Trim();
        YamlMap parentMap = frame.Container as YamlMap;
        if (parentMap == null)
          continue;

        if (valuePart.Length == 0)
        {
          // Вложенные ключи в YAML с отступом +2; frame на (indent+2), чтобы при
          // возврате к родителю (например steps: после preconditions:) PopToIndent снял блок.
          int childIndent = indent + 2;
          if (IsBlockSequenceKey(key))
          {
            var childSeq = new YamlSequence();
            parentMap.Entries[key] = childSeq;
            stack.Push(new YamlParseFrame(childIndent, childSeq));
          }
          else
          {
            var child = new YamlMap();
            parentMap.Entries[key] = child;
            stack.Push(new YamlParseFrame(childIndent, child));
          }

          continue;
        }

        if (valuePart.StartsWith("[", StringComparison.Ordinal) && valuePart.EndsWith("]", StringComparison.Ordinal))
        {
          parentMap.Entries[key] = ParseInlineSequence(valuePart);
          continue;
        }

        parentMap.Entries[key] = ParseScalarValue(valuePart);
      }

      return root;
    }

    private static string NormalizeStepType(string type)
    {
      if (string.Equals(type, "set_property", StringComparison.OrdinalIgnoreCase))
        return "set_custom_property";

      return type.Trim();
    }

    private static bool IsBlockSequenceKey(string key)
    {
      return string.Equals(key, "steps", StringComparison.OrdinalIgnoreCase)
          || string.Equals(key, "triggers", StringComparison.OrdinalIgnoreCase)
          || string.Equals(key, "recipes", StringComparison.OrdinalIgnoreCase)
          || string.Equals(key, "detect", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Разбор YAML-документа (общий подмножественный парсер для рецептов и триггеров SW).</summary>
    internal static YamlMap ParseDocumentPublic(string text)
    {
      return ParseDocument(text);
    }

    private static void PopToIndent(Stack<YamlParseFrame> stack, int indent)
    {
      // Только строго глубже: иначе на том же отступе снимается map элемента списка (- item)
      // и поля вроде influence_action_id не попадают в триггер (см. SwUserTriggers.yaml).
      while (stack.Count > 1 && stack.Peek().Indent > indent)
        stack.Pop();
    }

    private static int CountIndent(string line)
    {
      int n = 0;
      foreach (char c in line)
      {
        if (c == ' ')
          n++;
        else if (c == '\t')
          n += 2;
        else
          break;
      }

      return n;
    }

    private static string StripComment(string line)
    {
      bool inQuotes = false;
      for (int i = 0; i < line.Length; i++)
      {
        char c = line[i];
        if (c == '"')
          inQuotes = !inQuotes;
        else if (c == '#' && !inQuotes)
          return line.Substring(0, i);
      }

      return line;
    }

    private static YamlScalar ParseScalarValue(string text)
    {
      return new YamlScalar(Unquote(text));
    }

    private static string Unquote(string text)
    {
      if (text == null)
        return string.Empty;
      text = text.Trim();
      if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
        return text.Substring(1, text.Length - 2).Replace("\\\"", "\"");
      return text;
    }

    private static YamlSequence ParseInlineSequence(string text)
    {
      var seq = new YamlSequence();
      string inner = text.Trim();
      if (inner.Length < 2)
        return seq;
      inner = inner.Substring(1, inner.Length - 2).Trim();
      if (inner.Length == 0)
        return seq;

      foreach (string part in SplitInlineList(inner))
        seq.Items.Add(new YamlScalar(Unquote(part.Trim())));
      return seq;
    }

    private static YamlMap ParseInlineMap(string text)
    {
      var map = new YamlMap();
      string inner = text.Trim();
      if (inner.Length < 2)
        return map;
      inner = inner.Substring(1, inner.Length - 2);
      foreach (string part in SplitInlineList(inner))
      {
        int eq = part.IndexOf(':');
        if (eq < 0)
          continue;
        string k = part.Substring(0, eq).Trim();
        string v = part.Substring(eq + 1).Trim();
        map.Entries[k] = new YamlScalar(Unquote(v));
      }

      return map;
    }

    private static IEnumerable<string> SplitInlineList(string inner)
    {
      var current = new StringBuilder();
      bool inQuotes = false;
      for (int i = 0; i < inner.Length; i++)
      {
        char c = inner[i];
        if (c == '"')
        {
          inQuotes = !inQuotes;
          current.Append(c);
          continue;
        }

        if (c == ',' && !inQuotes)
        {
          yield return current.ToString();
          current.Clear();
          continue;
        }

        current.Append(c);
      }

      if (current.Length > 0)
        yield return current.ToString();
    }

    #endregion
  }
}
