using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Gomeostas;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages
{
  public partial class ParametersStylesMatrixView : UserControl
  {
    public ParametersStylesMatrixView()
    {
      InitializeComponent();
    }

    private void MatrixCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
      {
        var border = sender as Border;
        if (border?.DataContext is ParametersStylesMatrixViewModel.ParameterStyleCell cell)
        {
          HandleCellDoubleClick(cell);
        }
      }
    }

    private void HandleCellDoubleClick(ParametersStylesMatrixViewModel.ParameterStyleCell cell)
    {
      if (DataContext is ParametersStylesMatrixViewModel viewModel)
      {
        var agentInfo = viewModel.GetAgentState();
        var currentAgentStage = agentInfo?.EvolutionStage ?? 0;

        if (currentAgentStage > 0)
        {
          MessageBox.Show(
            "Редактирование стилей поведения запрещено в стадиях развития больше 0.\n" +
            "Для редактирования необходимо вернуть агента в стадию 0.",
            "Редактирование запрещено",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
          return;
        }

        var parameter = viewModel.GetParameterById(cell.ParameterId);
        if (parameter == null) return;

        var currentStyleIds = GetCurrentStyleIdsForZone(parameter, cell.ZoneId);
        var editor = new StyleSelectionEditor(
            $"Выбор стилей для параметра '{parameter.Name}' (Зона: {GetZoneName(cell.ZoneId)})",
            viewModel.GetAllStyles(),
            currentStyleIds);

        if (editor.ShowDialog() == true)
        {
          UpdateParameterStyles(parameter, cell.ZoneId, editor.SelectedStyleIds.ToList());
          viewModel.LoadMatrixFromParameters(viewModel.GetAllParameters());
        }
      }
    }

    private List<int> GetCurrentStyleIdsForZone(GomeostasSystem.ParameterData parameter, int zoneId)
    {
      if (parameter.StyleActivations != null &&
          parameter.StyleActivations.ContainsKey(zoneId))
      {
        return parameter.StyleActivations[zoneId];
      }
      return new List<int>();
    }

    private string GetZoneName(int zoneId)
    {
      var zoneNames = new[]
      {
        "Выход из нормы",
        "Возврат в норму",
        "Норма",
        "Слабое отклонение",
        "Значительное отклонение",
        "Сильное отклонение",
        "Критическое отклонение"
      };
      return zoneNames.Length > zoneId ? zoneNames[zoneId] : $"Зона {zoneId}";
    }

    private void UpdateParameterStyles(GomeostasSystem.ParameterData parameter, int zoneId, List<int> styleIds)
    {
      if (parameter.StyleActivations != null)
      {
        parameter.StyleActivations[zoneId] = styleIds;
      }
    }
  }
}