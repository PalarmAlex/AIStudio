using AIStudio.Converters;
using AIStudio.ViewModels;
using ISIDA.Psychic.Automatism;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static AIStudio.ViewModels.AutomatizmsViewModel;
using static ISIDA.Psychic.Automatism.ActionsImagesSystem;
using static ISIDA.Psychic.Automatism.AutomatizmChainsSystem;

namespace AIStudio.Dialogs
{
  public partial class AutomatizmChainEditorDialog : Window, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly AutomatizmChainsSystem _automatizmChainsSystem;
    private readonly ActionsImagesSystem _actionsImagesSystem;
    private readonly AutomatizmsViewModel _automatizmsViewModel;
    private string _treeNodeConditionsInfo;

    private int _chainId;
    private string _chainName;
    private string _chainDescription;
    private AutomatizmChain _editingChain;
    private readonly int _initialChainId;
    private bool _hasUnsavedChanges = false;

    public int ChainId => _chainId;
    public int TreeNodeId { get; }
    public string TreeNodeBaseState { get; private set; }
    public int TreeNodeEmotionId { get; private set; }
    public int TreeNodeActivityId { get; private set; }

    public string ChainName
    {
      get => _chainName;
      set
      {
        if (_chainName != value)
        {
          _chainName = value;
          _hasUnsavedChanges = true;
          OnPropertyChanged(nameof(ChainName));
          OnPropertyChanged(nameof(CanSave));
        }
      }
    }

    public string ChainDescription
    {
      get => _chainDescription;
      set
      {
        if (_chainDescription != value)
        {
          _chainDescription = value;
          _hasUnsavedChanges = true;
          OnPropertyChanged(nameof(ChainDescription));
          OnPropertyChanged(nameof(CanSave));
        }
      }
    }

    public ObservableCollection<ChainLink> ChainLinks { get; } = new ObservableCollection<ChainLink>();
    public ICollectionView ChainLinksView { get; }

    public string TreeNodeConditionsInfo
    {
      get => _treeNodeConditionsInfo;
      set
      {
        _treeNodeConditionsInfo = value;
        OnPropertyChanged(nameof(TreeNodeConditionsInfo));
      }
    }

    private ChainLink _selectedLink;
    public ChainLink SelectedLink
    {
      get => _selectedLink;
      set
      {
        _selectedLink = value;
        OnPropertyChanged(nameof(SelectedLink));
        OnPropertyChanged(nameof(IsLinkSelected));
      }
    }

    public bool IsLinkSelected => SelectedLink != null;
    public bool CanSave => !string.IsNullOrWhiteSpace(ChainName) && ChainLinks.Any();

    public List<KeyValuePair<int, string>> ActionsImageOptions { get; private set; }

    public AutomatizmChainEditorDialog(int treeNodeId, int chainId,
        AutomatizmChainsSystem automatizmChainsSystem,
        ActionsImagesSystem actionsImagesSystem,
         AutomatizmsViewModel automatizmsViewModel = null)
    {
      if (automatizmChainsSystem == null)
        throw new ArgumentNullException(nameof(automatizmChainsSystem));

      if (actionsImagesSystem == null)
        throw new ArgumentNullException(nameof(actionsImagesSystem));

      TreeNodeId = treeNodeId;
      _initialChainId = chainId;
      _automatizmChainsSystem = automatizmChainsSystem;
      _actionsImagesSystem = actionsImagesSystem;

      InitializeComponent();
      InitializeContextMenu();

      _automatizmsViewModel = automatizmsViewModel;

      LoadTreeNodeInfo();
      LoadTreeNodeConditionsInfo();

      ChainLinksView = CollectionViewSource.GetDefaultView(ChainLinks);
      ChainLinks.CollectionChanged += (s, e) =>
      {
        _hasUnsavedChanges = true;
        OnPropertyChanged(nameof(CanSave));
      };

      // Загружаем информацию об узле дерева
      LoadTreeNodeInfo();

      if (_initialChainId > 0)
        LoadExistingChain();
      else
      {
        ChainName = $"Цепочка для узла дерева {TreeNodeId}";
        ChainDescription = $"Автоматически созданная цепочка для узла дерева {TreeNodeId}";

        // Добавляем стартовое звено
        var startLink = new ChainLink
        {
          ID = 1,
          ActionsImageId = 0, // По умолчанию 0 - будет выбираться
          SuccessNextLink = 0,
          FailureNextLink = 0,
          SuccessThreshold = 1,
          Description = "Стартовое звено"
        };
        ChainLinks.Add(startLink);
      }

      //LoadActionsImageOptions();

      DataContext = this;
    }

    private void InitializeContextMenu()
    {
      var contextMenu = new ContextMenu();

      var selectActionMenuItem = new MenuItem
      {
        Header = "Выбрать образ действий...",
        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/AIStudio;component/Resources/Select.ico")) }
      };
      selectActionMenuItem.Click += (s, e) =>
      {
        if (SelectedLink != null)
          OpenActionsImageEditor(SelectedLink);
      };

      var viewDetailsMenuItem = new MenuItem
      {
        Header = "Просмотреть детали...",
        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/AIStudio;component/Resources/Info.ico")) }
      };
      viewDetailsMenuItem.Click += (s, e) =>
      {
        if (SelectedLink != null && SelectedLink.ActionsImageId > 0)
          OpenActionsImageReference(SelectedLink.ActionsImageId);
      };

      var clearActionMenuItem = new MenuItem
      {
        Header = "Очистить выбор",
        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/AIStudio;component/Resources/Delete.ico")) }
      };
      clearActionMenuItem.Click += (s, e) =>
      {
        if (SelectedLink != null)
        {
          SelectedLink.ActionsImageId = 0;
          _hasUnsavedChanges = true;
          OnPropertyChanged(nameof(CanSave));
        }
      };

      contextMenu.Items.Add(selectActionMenuItem);
      contextMenu.Items.Add(viewDetailsMenuItem);
      contextMenu.Items.Add(new Separator());
      contextMenu.Items.Add(clearActionMenuItem);

      // Привязываем контекстное меню к DataGrid
      LinksDataGrid.ContextMenu = contextMenu;

      // Также привязываем контекстное меню к ячейкам
      LinksDataGrid.ContextMenuOpening += LinksDataGrid_ContextMenuOpening;
    }

    private void LinksDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
      var cell = e.OriginalSource as DataGridCell;
      if (cell != null && cell.Column?.Header?.ToString() == "Образ действий")
      {
        if (cell.DataContext is ChainLink link)
        {
          SelectedLink = link;
        }
      }
    }

    private void LoadTreeNodeConditionsInfo()
    {
      if (_automatizmsViewModel != null)
        TreeNodeConditionsInfo = _automatizmsViewModel.GetTreeNodeConditionsInfo(TreeNodeId);
      else
      {
        try
        {
          var treeSystem = AutomatizmTreeSystem.Instance;
          var node = treeSystem.GetNodeById(TreeNodeId);
          if (node != null)
          {
            // Простая расшифровка без конвертера
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ID узла: {node.ID}");
            sb.AppendLine($"Базовое состояние: {GetBaseStateText(node.BaseID)}");
            if (node.EmotionID > 0)
              sb.AppendLine($"Эмоция ID: {node.EmotionID}");
            if (node.ActivityID > 0)
              sb.AppendLine($"Активность ID: {node.ActivityID}");
            if (node.ToneMoodID > 0)
              sb.AppendLine($"Тон/Настроение ID: {node.ToneMoodID}");
            if (node.VerbID > 0)
              sb.AppendLine($"Вербальное ID: {node.VerbID}");
            if (node.SimbolID > 0)
              sb.AppendLine($"Символьное ID: {node.SimbolID}");

            TreeNodeConditionsInfo = sb.ToString();
          }
          else
          {
            TreeNodeConditionsInfo = "Узел дерева не найден";
          }
        }
        catch (Exception ex)
        {
          TreeNodeConditionsInfo = $"Ошибка загрузки: {ex.Message}";
        }
      }
    }

    private void LoadTreeNodeInfo()
    {
      try
      {
        var treeSystem = AutomatizmTreeSystem.Instance;
        var node = treeSystem.GetNodeById(TreeNodeId);
        if (node != null)
        {
          TreeNodeBaseState = GetBaseStateText(node.BaseID);
          TreeNodeEmotionId = node.EmotionID;
          TreeNodeActivityId = node.ActivityID;
        }
        else
        {
          TreeNodeBaseState = "Неизвестно";
          TreeNodeEmotionId = 0;
          TreeNodeActivityId = 0;
        }
      }
      catch
      {
        TreeNodeBaseState = "Ошибка загрузки";
        TreeNodeEmotionId = 0;
        TreeNodeActivityId = 0;
      }

      OnPropertyChanged(nameof(TreeNodeBaseState));
      OnPropertyChanged(nameof(TreeNodeEmotionId));
      OnPropertyChanged(nameof(TreeNodeActivityId));
    }

    private void ActionsImageCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is ChainLink link)
      {
        OpenActionsImageEditor(link);
        e.Handled = true;
      }
    }

    private void ActionsImageCell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is ChainLink link)
      {
        SelectedLink = link;
        e.Handled = true;
      }
    }

    private string GetBaseStateText(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"Неизвестно ({baseId})";
      }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
      string validationError = ValidateChain();

      if (!string.IsNullOrEmpty(validationError))
      {
        var result = MessageBox.Show(
            $"В цепочке есть ошибки:\n{validationError}\n\n" +
            "Вы можете:\n" +
            "• Исправить ошибки (Да)\n" +
            "• Закрыть без изменений (Нет)\n" +
            "• Закрыть с ошибками (Отмена)",
            "Ошибки в цепочке",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Yes);

        if (result == MessageBoxResult.Yes)
        {
          e.Cancel = true;
          return;
        }
        else if (result == MessageBoxResult.No)
        {
          _hasUnsavedChanges = false;
          DialogResult = false;
        }
        else if (result == MessageBoxResult.Cancel)
        {
          e.Cancel = true;
          return;
        }
      }
      else if (_hasUnsavedChanges)
      {
        var result = MessageBox.Show(
            "Есть несохраненные изменения. Сохранить перед закрытием?",
            "Подтверждение",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Yes);

        if (result == MessageBoxResult.Yes)
        {
          e.Cancel = true;
          Dispatcher.BeginInvoke(new Action(() =>
          {
            SaveButton_Click(this, new RoutedEventArgs());
          }));
          return;
        }
        else if (result == MessageBoxResult.No)
        {
          _hasUnsavedChanges = false;
          DialogResult = false;
        }
        else if (result == MessageBoxResult.Cancel)
        {
          e.Cancel = true;
          return;
        }
      }

      base.OnClosing(e);
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
      Regex regex = new Regex("[^0-9]+");
      e.Handled = regex.IsMatch(e.Text);
    }

    private void LoadExistingChain()
    {
      try
      {
        var chain = _automatizmChainsSystem.GetChain(_initialChainId);
        if (chain != null)
        {
          _editingChain = chain;
          _chainId = chain.ID;
          ChainName = chain.Name;
          ChainDescription = chain.Description;

          ChainLinks.Clear();
          foreach (var link in chain.Links.OrderBy(l => l.ID))
          {
            ChainLinks.Add(link);

            // Устанавливаем подсказки для загруженных образов
            if (link.ActionsImageId > 0)
            {
              UpdateActionsImageTooltip(link);
            }
          }
          _hasUnsavedChanges = false;
          OnPropertyChanged(nameof(CanSave));
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки цепочки: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }


    //private void LoadActionsImageOptions()
    //{
    //  ActionsImageOptions = new List<KeyValuePair<int, string>>();

    //  // Добавляем пустой вариант
    //  ActionsImageOptions.Add(new KeyValuePair<int, string>(0, "-- Выберите образ действий --"));

    //  if (_actionsImagesSystem != null)
    //  {
    //    try
    //    {
    //      var allImages = _actionsImagesSystem.GetAllActionsImagesList();
    //      foreach (var image in allImages.OrderBy(i => i.Id))
    //      {
    //        string description = GetActionsImageDescription(image);
    //        ActionsImageOptions.Add(new KeyValuePair<int, string>(image.Id, description));
    //      }
    //    }
    //    catch (Exception ex)
    //    {
    //      MessageBox.Show($"Ошибка загрузки образов действий: {ex.Message}",
    //          "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
    //    }
    //  }

    //  OnPropertyChanged(nameof(ActionsImageOptions));
    //}

    private string GetActionsImageDescription(ActionsImage image)
    {
      List<string> parts = new List<string>
      {
          $"ID: {image.Id}",
          $"Тип: {(image.Kind == 0 ? "Объект" : "Предполож")}"
      };

      if (image.ActIdList != null && image.ActIdList.Any())
        parts.Add($"Действий: {image.ActIdList.Count}");

      if (image.PhraseIdList != null && image.PhraseIdList.Any())
        parts.Add($"Фраз: {image.PhraseIdList.Count}");

      if (image.ToneId != 0)
        parts.Add($"Тон: {ActionsImagesSystem.GetToneText(image.ToneId)}");

      if (image.MoodId != 0)
        parts.Add($"Настроение: {ActionsImagesSystem.GetMoodText(image.MoodId)}");

      return string.Join(" | ", parts);
    }

    private string ValidateChain()
    {
      if (!ChainLinks.Any())
        return "Цепочка не содержит звеньев";

      StringBuilder errorMessage = new StringBuilder();

      foreach (var link in ChainLinks)
      {
        string validationError = ValidateLink(link);
        if (!string.IsNullOrEmpty(validationError))
        {
          errorMessage.AppendLine(validationError);
        }
      }

      var terminalLinks = ChainLinks.Where(l => l.SuccessNextLink == 0 && l.FailureNextLink == 0).ToList();
      if (terminalLinks.Count == 0)
      {
        errorMessage.AppendLine("Цепочка не содержит конечных звеньев (оба следующих звена = 0) - возможен бесконечный цикл");
      }

      // Проверяем образы действий
      foreach (var link in ChainLinks)
      {
        if (link.ActionsImageId == 0)
        {
          errorMessage.AppendLine($"Звено {link.ID}: не выбран образ действий");
        }
        else
        {
          var image = _actionsImagesSystem.GetActionsImage(link.ActionsImageId);
          if (image == null)
            errorMessage.AppendLine($"Звено {link.ID}: образ действий ID:{link.ActionsImageId} не найден");
        }
      }

      return errorMessage.ToString();
    }

    private void AddLinkButton_Click(object sender, RoutedEventArgs e)
    {
      int newLinkId = ChainLinks.Any() ? ChainLinks.Max(l => l.ID) + 1 : 1;

      var newLink = new ChainLink
      {
        ID = newLinkId,
        ActionsImageId = ActionsImageOptions.FirstOrDefault().Key,
        SuccessNextLink = 0,
        FailureNextLink = 0,
        SuccessThreshold = 1,
        Description = $"Звено {newLinkId}"
      };

      ChainLinks.Add(newLink);

      OnPropertyChanged(nameof(CanSave));
    }

    private void RemoveLinkButton_Click(object sender, RoutedEventArgs e)
    {
      if (SelectedLink == null)
      {
        MessageBox.Show("Выберите звено для удаления", "Внимание",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      var linkIdToRemove = SelectedLink.ID;
      var referencingLinks = ChainLinks.Where(l =>
          l.SuccessNextLink == linkIdToRemove ||
          l.FailureNextLink == linkIdToRemove).ToList();

      if (referencingLinks.Any())
      {
        var result = MessageBox.Show(
            $"На это звено ссылаются другие звенья: {string.Join(", ", referencingLinks.Select(l => l.ID))}\n" +
            "Удалить звено и обнулить ссылки?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
          foreach (var refLink in referencingLinks)
          {
            if (refLink.SuccessNextLink == linkIdToRemove)
              refLink.SuccessNextLink = 0;
            if (refLink.FailureNextLink == linkIdToRemove)
              refLink.FailureNextLink = 0;
          }
        }
        else
        {
          return;
        }
      }

      ChainLinks.Remove(SelectedLink);
      SelectedLink = null;

      OnPropertyChanged(nameof(CanSave));
    }

    private void UpdateLinkButton_Click(object sender, RoutedEventArgs e)
    {
      if (SelectedLink == null)
      {
        MessageBox.Show("Выберите звено для обновления", "Внимание",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      string validationError = ValidateLink(SelectedLink);

      if (!string.IsNullOrEmpty(validationError))
      {
        MessageBox.Show(validationError, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      _hasUnsavedChanges = true;
      OnPropertyChanged(nameof(CanSave));
      MessageBox.Show("Звено обновлено", "Успех",
          MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string ValidateLink(ChainLink link)
    {
      if (link.SuccessNextLink > 0)
      {
        if (link.SuccessNextLink == link.ID)
          return $"Звено {link.ID} ссылается само на себя (SuccessNextLink)";

        if (!ChainLinks.Any(l => l.ID == link.SuccessNextLink))
          return $"Следующее звено при успехе (ID:{link.SuccessNextLink}) не найдено";

        if (link.SuccessNextLink <= link.ID)
          return $"Следующее звено при успехе должно иметь ID больше текущего ({link.ID})";
      }

      if (link.FailureNextLink > 0)
      {
        if (link.FailureNextLink == link.ID)
          return $"Звено {link.ID} ссылается само на себя (FailureNextLink)";

        if (!ChainLinks.Any(l => l.ID == link.FailureNextLink))
          return $"Следующее звено при неудаче (ID:{link.FailureNextLink}) не найдено";

        if (link.FailureNextLink <= link.ID)
          return $"Следующее звено при неудаче должно иметь ID больше текущего ({link.ID})";
      }

      if (link.SuccessThreshold < 0)
        return $"Звено {link.ID}: порог успеха не может быть отрицательным";

      return null;
    }

    private void ValidateChainButton_Click(object sender, RoutedEventArgs e)
    {
      string validationError = ValidateChain();

      if (!string.IsNullOrEmpty(validationError))
      {
        MessageBox.Show($"Ошибки валидации:\n{validationError}", "Ошибка валидации",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      MessageBox.Show("Цепочка валидна", "Проверка пройдена",
          MessageBoxButton.OK, MessageBoxImage.Information);
    }

    //private void OpenActionsImageReference_Click(object sender, RoutedEventArgs e)
    //{
    //  if (sender is Button button && button.DataContext is ChainLink link)
    //  {
    //    OpenActionsImageReference(link.ActionsImageId);
    //  }
    //}

    private void LinksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (SelectedLink != null)
      {
        OpenActionsImageReference(SelectedLink.ActionsImageId);
      }
    }

    private void OpenActionsImageReferenceButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (SelectedLink != null)
        {
          OpenActionsImageEditor(SelectedLink);
        }
        else
        {
          // Создаем временное звено для выбора
          var tempLink = new ChainLink { ActionsImageId = 0 };
          var dialog = new ActionsImageSelectorDialog(_actionsImagesSystem, 0);

          if (dialog.ShowDialog() == true)
          {
            MessageBox.Show($"Выбран образ действий ID: {dialog.SelectedActionsImageId}\n\n" +
                           "Чтобы использовать этот образ, выберите звено в таблице и нажмите на ячейку с ID образа действий.",
                           "Образ выбран",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка открытия справочника: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void OpenActionsImageReference(int actionsImageId)
    {
      if (actionsImageId == 0)
      {
        MessageBox.Show("Образ действий не выбран", "Информация",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      var image = _actionsImagesSystem.GetActionsImage(actionsImageId);
      if (image == null)
      {
        MessageBox.Show($"Образ действий ID:{actionsImageId} не найден", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      // Показываем информацию об образе действий
      string info = GetActionsImageDetails(image);
      MessageBox.Show(info, $"Образ действий ID: {actionsImageId}",
          MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string GetActionsImageDetails(ActionsImage image)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine($"ID: {image.Id}");
      sb.AppendLine($"Тип: {(image.Kind == 0 ? "Объективное действие" : "Субъективное предположение")}");

      if (image.ActIdList != null && image.ActIdList.Any())
      {
        sb.AppendLine($"Действия: {string.Join(", ", image.ActIdList)}");
      }

      if (image.PhraseIdList != null && image.PhraseIdList.Any())
      {
        sb.AppendLine($"Фразы: {string.Join(", ", image.PhraseIdList)}");
      }

      if (image.ToneId != 0)
      {
        sb.AppendLine($"Тон: {ActionsImagesSystem.GetToneText(image.ToneId)} (ID: {image.ToneId})");
      }

      if (image.MoodId != 0)
      {
        sb.AppendLine($"Настроение: {ActionsImagesSystem.GetMoodText(image.MoodId)} (ID: {image.MoodId})");
      }

      return sb.ToString();
    }

    private void OpenActionsImageEditor_Click(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is ChainLink link)
      {
        OpenActionsImageEditor(link);
      }
    }

    private void OpenActionsImageEditor(ChainLink link)
    {
      try
      {
        var dialog = new ActionsImageSelectorDialog(_actionsImagesSystem, link.ActionsImageId);
        if (dialog.ShowDialog() == true)
        {
          link.ActionsImageId = dialog.SelectedActionsImageId;
          _hasUnsavedChanges = true;
          OnPropertyChanged(nameof(CanSave));

          UpdateActionsImageTooltip(link);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при выборе образа действий: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void UpdateActionsImageTooltip(ChainLink link)
    {
      try
      {
        if (link.ActionsImageId > 0)
        {
          var image = _actionsImagesSystem.GetActionsImage(link.ActionsImageId);
          if (image != null)
          {
            // Создаем подсказку с помощью конвертера
            var converter = new AutomatizmActionsToTooltipConverter();
            var display = new ActionsImageDisplay
            {
              ActIdList = image.ActIdList ?? new List<int>(),
              PhraseIdList = image.PhraseIdList ?? new List<int>(),
              ToneId = image.ToneId,
              MoodId = image.MoodId
            };

            var tooltipText = converter.Convert(display, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture) as string;

            // Находим ячейку и обновляем ToolTip
            var row = LinksDataGrid.ItemContainerGenerator.ContainerFromItem(link) as DataGridRow;
            if (row != null)
            {
              var cell = GetCell(LinksDataGrid, row, 1); // 1 - индекс колонки "Образ действий"
              if (cell != null)
              {
                var textBlock = FindVisualChild<TextBlock>(cell);
                if (textBlock != null)
                {
                  textBlock.ToolTip = tooltipText;
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка обновления подсказки: {ex.Message}");
      }
    }

    private DataGridCell GetCell(DataGrid dataGrid, DataGridRow row, int columnIndex)
    {
      if (row != null)
      {
        DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(row);
        if (presenter != null)
        {
          DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
          if (cell == null)
          {
            dataGrid.ScrollIntoView(row, dataGrid.Columns[columnIndex]);
            cell = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
          }
          return cell;
        }
      }
      return null;
    }

    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(parent, i);
        if (child is T typedChild)
          return typedChild;

        T childOfChild = FindVisualChild<T>(child);
        if (childOfChild != null)
          return childOfChild;
      }
      return null;
    }


    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!CanSave)
      {
        MessageBox.Show("Заполните название цепочки и добавьте хотя бы одно звено",
            "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      string validationError = ValidateChain();
      if (!string.IsNullOrEmpty(validationError))
      {
        MessageBox.Show($"Нельзя сохранить цепочку с ошибками:\n{validationError}",
            "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      try
      {
        var links = new List<ChainLink>(ChainLinks);

        if (_editingChain != null)
        {
          _editingChain.Name = ChainName;
          _editingChain.Description = ChainDescription;
          _editingChain.TreeNodeId = TreeNodeId;
          _editingChain.Links = links;

          var (success, error) = _automatizmChainsSystem.SaveAutomatizmChains();
          if (!success)
            throw new Exception($"Не удалось сохранить цепочку: {error}");

          _chainId = _editingChain.ID;
        }
        else
        {
          var (newChainId, warnings) = _automatizmChainsSystem.AddAutomatizmChain(
              ChainName,
              ChainDescription,
              links,
              TreeNodeId);

          if (warnings != null && warnings.Any())
          {
            MessageBox.Show($"Предупреждения при создании цепочки:\n{string.Join("\n", warnings)}",
                "Предупреждения", MessageBoxButton.OK, MessageBoxImage.Warning);
          }

          _chainId = newChainId;

          var (saveSuccess, error) = _automatizmChainsSystem.SaveAutomatizmChains();
          if (!saveSuccess)
            throw new Exception($"Не удалось сохранить цепочку: {error}");
        }

        _hasUnsavedChanges = false;
        DialogResult = true;
        Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка сохранения цепочки: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      CloseWithConfirmation();
    }

    private void CloseWithConfirmation()
    {
      string validationError = ValidateChain();

      if (!string.IsNullOrEmpty(validationError))
      {
        var result = MessageBox.Show(
            $"В цепочке есть ошибки:\n{validationError}\n\n" +
            "Вы уверены, что хотите закрыть форму с ошибками?",
            "Ошибки в цепочке",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.No)
          return;
      }
      else if (_hasUnsavedChanges)
      {
        var result = MessageBox.Show(
            "Есть несохраненные изменения. Закрыть без сохранения?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (result == MessageBoxResult.No)
          return;
      }

      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        CloseWithConfirmation();
        e.Handled = true;
      }
    }
  }
}