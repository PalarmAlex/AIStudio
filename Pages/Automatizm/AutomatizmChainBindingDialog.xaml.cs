using AIStudio.ViewModels;
using ISIDA.Psychic.Automatism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class AutomatizmChainBindingDialog : Window
  {
    private readonly AutomatizmChainsSystem _chainsSystem;
    private readonly AutomatizmTreeSystem _treeSystem;
    private readonly int _treeNodeId;
    private readonly int _currentChainId;

    public int SelectedChainId { get; private set; }
    public bool ChainDeleted { get; private set; }

    public AutomatizmChainBindingDialog(int treeNodeId, int currentChainId,
        AutomatizmChainsSystem chainsSystem, AutomatizmTreeSystem treeSystem)
    {
      InitializeComponent();

      _treeNodeId = treeNodeId;
      _currentChainId = currentChainId;
      _chainsSystem = chainsSystem ?? throw new ArgumentNullException(nameof(chainsSystem));
      _treeSystem = treeSystem ?? throw new ArgumentNullException(nameof(treeSystem));

      DataContext = new AutomatizmChainBindingViewModel(treeNodeId, currentChainId, chainsSystem, treeSystem);

      Loaded += AutomatizmChainBindingDialog_Loaded;
      PreviewKeyDown += Window_PreviewKeyDown;
    }

    private void AutomatizmChainBindingDialog_Loaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is AutomatizmChainBindingViewModel viewModel)
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
      if (DataContext is AutomatizmChainBindingViewModel viewModel)
      {
        viewModel.UpdateSelectionInfo();
      }
    }

    private void LinksDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      if (DataContext is AutomatizmChainBindingViewModel viewModel && viewModel.SelectedLink != null)
      {
        // Показываем подсказку для образа действий
        var link = viewModel.SelectedLink;
        var toolTip = $"Образ действий ID: {link.ActionsImageId}\nДвойной клик - открыть справочник";
        LinksDataGrid.ToolTip = toolTip;
      }
    }

    private void LinksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is AutomatizmChainBindingViewModel viewModel && viewModel.SelectedLink != null)
      {
        // Открываем справочник образов действий
        OpenActionsImageReference(viewModel.SelectedLink.ActionsImageId);
      }
    }

    private void OpenActionsImageReference(int actionsImageId)
    {
      // TODO: Реализовать открытие справочника образов действий
      MessageBox.Show($"Открытие справочника для образа действий ID: {actionsImageId}\n" +
                     "Этот функционал будет реализован позже.",
                     "Справочник образов действий",
                     MessageBoxButton.OK,
                     MessageBoxImage.Information);
    }

    private void EditChainButton_Click(object sender, RoutedEventArgs e)
    {
      if (DataContext is AutomatizmChainBindingViewModel viewModel && viewModel.SelectedChain != null)
      {
        // Открываем редактор цепочки
        var chainEditor = new AutomatizmChainEditorDialog(
            _treeNodeId,
            viewModel.SelectedChain.ID,
            _chainsSystem,
            ActionsImagesSystem.Instance
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
      if (DataContext is AutomatizmChainBindingViewModel viewModel && viewModel.SelectedChain != null)
      {
        var result = MessageBox.Show(
            $"Вы действительно хотите удалить цепочку \"{viewModel.SelectedChain.Name}\" (ID: {viewModel.SelectedChain.ID})?\n" +
            "Это действие также удалит все звенья цепочки и отвяжет ее от всех узлов дерева.",
            "Подтверждение удаления цепочки",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
          try
          {
            bool deleted = _chainsSystem.RemoveAutomatizmChain(viewModel.SelectedChain.ID);
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
      if (DataContext is AutomatizmChainBindingViewModel viewModel && viewModel.SelectedChain != null)
      {
        if (viewModel.SelectedChain.ID == _currentChainId)
        {
          MessageBox.Show(
              "Эта цепочка уже привязана к текущему узлу дерева.",
              "Цепочка уже привязана",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
          return;
        }

        // Проверяем, привязана ли цепочка к другим узлам дерева
        var chain = viewModel.SelectedChain;
        if (chain.TreeNodeId > 0 && chain.TreeNodeId != _treeNodeId)
        {
          var treeNode = _treeSystem.GetNodeById(chain.TreeNodeId);
          string treeNodeInfo = treeNode != null ? $"ID: {chain.TreeNodeId}" : $"неизвестный узел (ID: {chain.TreeNodeId})";

          var result = MessageBox.Show(
              $"Цепочка \"{chain.Name}\" уже привязана к узлу дерева: {treeNodeInfo}\n" +
              "Привязать к текущему узлу? (старая привязка будет удалена)",
              "Цепочка уже привязана к другому узлу",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result != MessageBoxResult.Yes)
            return;
        }

        try
        {
          // Привязываем к текущему узлу дерева
          chain.TreeNodeId = _treeNodeId;
          var (success, error) = _chainsSystem.SaveAutomatizmChains();

          if (success)
          {
            SelectedChainId = chain.ID;

            MessageBox.Show(
                $"Цепочка \"{chain.Name}\" успешно привязана к узлу дерева {_treeNodeId}.",
                "Цепочка привязана",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
          }
          else
          {
            MessageBox.Show(
                $"Не удалось привязать цепочку: {error}",
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
