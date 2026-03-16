using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels.Understanding;

namespace AIStudio.Pages.Understanding
{
  /// <summary>Выбор шаблона: для «Узел дерева автоматизмов» — подпись сверху и расшифровка под ней; для остальных — подпись и значение в одной строке.</summary>
  public class PropertyRowTemplateSelector : DataTemplateSelector
  {
    public DataTemplate DefaultTemplate { get; set; }
    public DataTemplate AutNodeTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is PropertyRow row)
      {
        // Используем AutNodeTemplate для всех строк, которые имеют ValueLines
        if (row.ValueLines != null && row.ValueLines.Count > 0)
          return AutNodeTemplate ?? DefaultTemplate;
      }
      return DefaultTemplate;
    }
  }
}
