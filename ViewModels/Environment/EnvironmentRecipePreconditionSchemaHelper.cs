using AIStudio.Common.Adapters;
using ISIDA.SymbiontEnv.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Синхронизация предусловий рецепта со schema/recipe-preconditions.json.
  /// </summary>
  public static class EnvironmentRecipePreconditionSchemaHelper
  {
    public static void Initialize(
        EnvironmentRecipeEditorModel model,
        EnvironmentRecipeData recipe,
        AdapterEnvironmentSchema schema,
        bool applyNewRecipeDefaults = false)
    {
      if (model == null)
        throw new ArgumentNullException(nameof(model));
      if (schema == null)
        throw new ArgumentNullException(nameof(schema));

      EnvironmentSchemaFieldsHelper.PopulateFields(
          model.PreconditionFields,
          schema.RecipePreconditions,
          key => GetSelectedStringListValues(recipe, key),
          key => GetPreconditionBool(recipe?.Preconditions, key),
          applyNewRecipeDefaults);
    }

    public static void ApplyToData(
        EnvironmentRecipeEditorModel model,
        EnvironmentRecipeData data)
    {
      if (model == null)
        throw new ArgumentNullException(nameof(model));
      if (data == null)
        throw new ArgumentNullException(nameof(data));

      data.DocumentKinds.Clear();
      data.Preconditions.Clear();

      foreach (EnvironmentRecipePreconditionField field in model.PreconditionFields)
      {
        if (field == null || string.IsNullOrWhiteSpace(field.Key))
          continue;

        if (field.IsStringListType)
        {
          if (!string.Equals(field.Key, EnvironmentSchemaFieldsHelper.DocumentKindsKey, StringComparison.OrdinalIgnoreCase))
            continue;

          foreach (EnvironmentRecipePreconditionListItem item in field.ListItems)
          {
            if (item == null || !item.IsSelected || string.IsNullOrWhiteSpace(item.Value))
              continue;
            data.DocumentKinds.Add(item.Value);
          }
        }
        else if (field.IsChecked)
        {
          data.Preconditions[field.Key] = true;
        }
      }
    }

    private static IList<string> GetSelectedStringListValues(EnvironmentRecipeData recipe, string key)
    {
      if (recipe == null)
        return new List<string>();

      if (string.Equals(key, EnvironmentSchemaFieldsHelper.DocumentKindsKey, StringComparison.OrdinalIgnoreCase))
        return recipe.DocumentKinds?.ToList() ?? new List<string>();

      return new List<string>();
    }

    private static bool GetPreconditionBool(IDictionary<string, bool> preconditions, string key)
    {
      if (preconditions == null || string.IsNullOrWhiteSpace(key))
        return false;
      return preconditions.TryGetValue(key, out bool value) && value;
    }
  }
}
