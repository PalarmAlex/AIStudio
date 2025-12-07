using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class ReflexChainBindingDialog : Window
  {
    private readonly ReflexChainsSystem _chainsSystem;
    private readonly GeneticReflexesSystem _reflexesSystem;
    private readonly int _reflexId;
    private readonly int _currentChainId;

    public int SelectedChainId { get; private set; }
    public bool ChainDeleted { get; private set; }

    public ReflexChainBindingDialog(int reflexId, int currentChainId,
        ReflexChainsSystem chainsSystem, GeneticReflexesSystem reflexesSystem)
    {
      InitializeComponent();

      _reflexId = reflexId;
      _currentChainId = currentChainId;
      _chainsSystem = chainsSystem ?? throw new ArgumentNullException(nameof(chainsSystem));
      _reflexesSystem = reflexesSystem ?? throw new ArgumentNullException(nameof(reflexesSystem));

      DataContext = new ReflexChainBindingViewModel(reflexId, currentChainId, chainsSystem, reflexesSystem);

      Loaded += ReflexChainBindingDialog_Loaded;
      PreviewKeyDown += Window_PreviewKeyDown;
    }

    private void ReflexChainBindingDialog_Loaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is ReflexChainBindingViewModel viewModel)
      {
        viewModel.LoadData();
      }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        CloseButton_Click(sender, e);
        e.Handled = true;
      }
    }

    private void ChainsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      if (DataContext is ReflexChainBindingViewModel viewModel)
      {
        viewModel.UpdateSelectionInfo();
      }
    }

    private void EditChainButton_Click(object sender, RoutedEventArgs e)
    {
      if (DataContext is ReflexChainBindingViewModel viewModel && viewModel.SelectedChain != null)
      {
        // Открываем редактор цепочки
        var chainEditor = new ReflexChainEditorDialog(
            _reflexId,
            0, // Базовое состояние не используется в этом контексте
            new List<int>(), // Стили не используются
            new List<int>(), // Стимулы не используются
            new List<int>(), // Действия не используются
            viewModel.SelectedChain.ID,
            _chainsSystem,
            AdaptiveActionsSystem.Instance
        );

        if (chainEditor.ShowDialog() == true)
        {
          // Обновляем данные после редактирования
          viewModel.LoadData();
        }
      }
    }

    private void DeleteChainButton_Click(object sender, RoutedEventArgs e)
    {
      if (DataContext is ReflexChainBindingViewModel viewModel && viewModel.SelectedChain != null)
      {
        var result = MessageBox.Show(
            $"Вы действительно хотите удалить цепочку \"{viewModel.SelectedChain.Name}\" (ID: {viewModel.SelectedChain.ID})?\n" +
            "Это действие также удалит все звенья цепочки и отвяжет ее от всех рефлексов.",
            "Подтверждение удаления цепочки",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
          try
          {
            bool deleted = _chainsSystem.RemoveReflexChain(viewModel.SelectedChain.ID);
            if (deleted)
            {
              ChainDeleted = true;
              MessageBox.Show(
                  $"Цепочка \"{viewModel.SelectedChain.Name}\" успешно удалена.",
                  "Цепочка удалена",
                  MessageBoxButton.OK,
                  MessageBoxImage.Information);

              // Обновляем данные
              viewModel.LoadData();
            }
            else
            {
              MessageBox.Show(
                  "Не удалось удалить цепочку.",
                  "Ошибка удаления",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
            }
          }
          catch (Exception ex)
          {
            MessageBox.Show(
                $"Ошибка при удалении цепочки: {ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
      }
    }

    private void BindChainButton_Click(object sender, RoutedEventArgs e)
    {
      if (DataContext is ReflexChainBindingViewModel viewModel && viewModel.SelectedChain != null)
      {
        if (viewModel.SelectedChain.ID == _currentChainId)
        {
          MessageBox.Show(
              "Эта цепочка уже привязана к текущему рефлексу.",
              "Цепочка уже привязана",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
          return;
        }

        // Проверяем, привязана ли цепочка к другим рефлексам
        var reflexesWithChain = _reflexesSystem.GetReflexesForChain(viewModel.SelectedChain.ID);
        if (reflexesWithChain.Any() && reflexesWithChain[0] != _reflexId)
        {
          var result = MessageBox.Show(
              $"Цепочка \"{viewModel.SelectedChain.Name}\" уже привязана к рефлексу(ам): {string.Join(", ", reflexesWithChain)}\n" +
              "Привязка к новому рефлексу отвяжет ее от текущих рефлексов.\n" +
              "Продолжить?",
              "Цепочка уже используется",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result != MessageBoxResult.Yes)
          {
            return;
          }

          // Отвязываем от всех рефлексов
          foreach (var reflexId in reflexesWithChain)
          {
            try
            {
              _reflexesSystem.DetachChainFromReflex(reflexId);
            }
            catch (Exception ex)
            {
              MessageBox.Show(
                  $"Ошибка при отвязке цепочки от рефлекса {reflexId}: {ex.Message}",
                  "Предупреждение",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }
          }
        }

        try
        {
          // Привязываем к текущему рефлексу
          bool attached = _reflexesSystem.AttachChainToReflex(_reflexId, viewModel.SelectedChain.ID);
          if (attached)
          {
            SelectedChainId = viewModel.SelectedChain.ID;

            MessageBox.Show(
                $"Цепочка \"{viewModel.SelectedChain.Name}\" успешно привязана к рефлексу {_reflexId}.",
                "Цепочка привязана",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
          }
          else
          {
            MessageBox.Show(
                "Не удалось привязать цепочку.",
                "Ошибка привязки",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show(
              $"Ошибка при привязке цепочки: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }
  }
}