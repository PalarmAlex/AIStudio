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
      if (_isSelectionChanging) return;

      if (sender is ComboBox comboBox &&
          comboBox.DataContext is AgentViewModel viewModel &&
          e.AddedItems.Count > 0 &&
          e.AddedItems[0] is int newStage)
      {
        // Сохраняем текущее значение ДО любых изменений
        int oldStage = viewModel.SelectedStage;

        // Если новая стадия равна текущей - ничего не делаем
        if (newStage == oldStage) return;

        _isSelectionChanging = true;

        try
        {
          viewModel.ChangeStageCommand.Execute(newStage);
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
          // Откатываем значение ComboBox при ошибке
          comboBox.SelectedValue = oldStage;
        }
        finally
        {
          _isSelectionChanging = false;
        }

        viewModel.UpdateEditableProperties();
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