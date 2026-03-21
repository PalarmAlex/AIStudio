using AIStudio.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages.Understanding
{
  public partial class SituationTypesView : UserControl
  {
    public SituationTypesView()
    {
      InitializeComponent();
    }

    /// <summary>Перед сохранением фиксируем изменения в ячейках (ComboBox, TextBox) — иначе значения не попадают в модель.</summary>
    private void SaveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      EventGrid.CommitEdit(DataGridEditingUnit.Row, true);
      EventGrid.CommitEdit(DataGridEditingUnit.Cell, true);
      MoodGrid.CommitEdit(DataGridEditingUnit.Row, true);
      MoodGrid.CommitEdit(DataGridEditingUnit.Cell, true);
      InfluenceGrid.CommitEdit(DataGridEditingUnit.Row, true);
      InfluenceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
      if (sender is Control ctrl)
        ctrl.Focus();
    }

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
      if (e.EditAction == DataGridEditAction.Commit && e.Row != null && !e.Row.IsEditing)
      {
        var grid = (DataGrid)sender;
        grid.CommitEdit(DataGridEditingUnit.Row, true);
      }
    }
  }
}
