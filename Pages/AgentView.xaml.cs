using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AIStudio.Dialogs;
using AIStudio.ViewModels;

namespace AIStudio.Pages
{
  public partial class AgentView : UserControl
  {
    private bool _isSelectionChanging = false;

    public AgentView()
    {
      InitializeComponent();
    }

    private void StageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!(sender is ComboBox comboBox) ||
          !(comboBox.DataContext is AgentViewModel viewModel) ||
          e.AddedItems.Count == 0 ||
          !(e.AddedItems[0] is int newStage))
      {
        return;
      }

      int currentStage = viewModel.Gomeostas.GetAgentState().EvolutionStage;
      if (newStage == currentStage)
      {
        if (viewModel.SelectedStage != currentStage)
          viewModel.SelectedStage = currentStage;
        return;
      }

      if (newStage > currentStage + 1)
      {
        MessageBox.Show($"Недопустимый переход! Можно переходить только на следующую стадию.",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        comboBox.Dispatcher.InvokeAsync(() =>
        {
          comboBox.SelectedValue = currentStage;
        });
        return;
      }

      bool isBackward = newStage < currentStage;
      string title = isBackward ? "ВНИМАНИЕ: Возврат на предыдущую стадию" : "Подтверждение перехода";
      string message = isBackward
          ? $"Возврат на стадию {newStage} очистит все данные последующих стадий. Продолжить?"
          : $"Перейти со стадии {currentStage} на стадию {newStage}?";

      var result = MessageBox.Show(message, title,
          MessageBoxButton.YesNo,
          isBackward ? MessageBoxImage.Warning : MessageBoxImage.Question);

      if (result != MessageBoxResult.Yes)
      {
        comboBox.Dispatcher.InvokeAsync(() =>
        {
          comboBox.SelectedValue = currentStage;
        });
        return;
      }

      var stageResult = viewModel.Gomeostas.SetEvolutionStage(newStage, isBackward, false);

      if (stageResult.Success)
      {
        viewModel.LoadAgentData();

        var (saveSuccess, error) = viewModel.Gomeostas.SaveAgentProperties();
        if (!saveSuccess)
        {
          MessageBox.Show($"Ошибка сохранения: {error}",
              "Предупреждение",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
        }
      }
      else
      {
        MessageBox.Show(stageResult.Message, "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);

        comboBox.Dispatcher.InvokeAsync(() =>
        {
          comboBox.SelectedValue = currentStage;
        });
      }
    }

    private void DescriptionCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is TextBox textBox && textBox.DataContext is AgentViewModel viewModel)
      {
        // Получаем текущее значение описания
        string currentDescription = viewModel.AgentProperties[1].Value;

        // Нормализуем символы новой строки для отображения в диалоге
        currentDescription = currentDescription?.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine) ?? "";

        var dialog = new TextInputDialog
        {
          Owner = Window.GetWindow(textBox),
          Title = "Редактирование описания агента",
          Text = currentDescription,
          Multiline = true
        };

        if (dialog.ShowDialog() == true)
        {
          // Сохраняем текст как есть (символы новой строки уже в правильном формате)
          viewModel.AgentProperties[1].Value = dialog.Text;
        }
      }

      e.Handled = true;
    }
  }
}