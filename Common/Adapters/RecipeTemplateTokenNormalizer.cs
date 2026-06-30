using System;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Нормализация токенов шаблонов SolidWorks из каталога рецепта.
  /// </summary>
  public static class RecipeTemplateTokenNormalizer
  {
    /// <summary>
    /// Убирает ошибочное JSON-экранирование внутренних кавычек (<c>\"</c> → <c>"</c>),
    /// чтобы ссылки вида <c>$PRP:"SW-File Name"</c> попадали в рецепт как в SolidWorks.
    /// </summary>
    public static string NormalizeForSolidWorks(string token)
    {
      if (string.IsNullOrEmpty(token))
        return token ?? string.Empty;
      if (token.IndexOf("\\\"", StringComparison.Ordinal) < 0)
        return token;
      return token.Replace("\\\"", "\"");
    }
  }
}
