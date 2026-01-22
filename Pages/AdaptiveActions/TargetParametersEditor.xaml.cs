using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ISIDA.Gomeostas;

namespace AIStudio.Dialogs
{
  public partial class TargetParametersEditor : Window
  {
    public List<int> SelectedParameterIds { get; private set; }

    public TargetParametersEditor(string title,
                                List<GomeostasSystem.ParameterData> availableParameters,
                                List<int> currentlySelectedIds)
    {
      InitializeComponent();
      Title = title;

      SelectedParameterIds = new List<int>(currentlySelectedIds ?? new List<int>());

      var items = availableParameters?
          .Where(p => p != null)
          .Select(p => new ParameterSelectionItem
          {
            Id = p.Id,
            Name = $"ID:{p.Id} - {p.Name ?? $"Параметр {p.Id}"}",
            Description = p.Description ?? "",
            IsSelected = currentlySelectedIds?.Contains(p.Id) == true
          })
          .OrderBy(p => p.Id)
          .ToList() ?? new List<ParameterSelectionItem>();

      ParametersList.ItemsSource = items;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (!(ParametersList.ItemsSource is IEnumerable<ParameterSelectionItem> items))
        {
          DialogResult = false;
          Close();
          return;
        }

        var selectedItems = items.Where(item => item.IsSelected).ToList();
        SelectedParameterIds = selectedItems.Select(item => item.Id).ToList();

        DialogResult = true;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        DialogResult = false;
      }
      finally
      {
        Close();
      }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }
  }

  public class ParameterSelectionItem
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsSelected { get; set; }
  }
}