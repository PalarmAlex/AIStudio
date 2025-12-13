using AIStudio.Converters;
using ISIDA.Actions;
using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static ISIDA.Reflexes.ReflexChainsSystem;

namespace AIStudio.Dialogs
{
  public partial class ReflexChainEditorDialog : Window, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ReflexChainsSystem _reflexChainsSystem;
    private readonly AdaptiveActionsSystem _actionsSystem;

    private int _chainId;
    private string _chainName;
    private string _chainDescription;
    private ReflexChain _editingChain;
    private readonly int _initialChainId;
    private readonly List<int> _reflexAdaptiveActions;
    private bool _hasUnsavedChanges = false;

    public int ChainId => _chainId;
    public int ReflexId { get; }
    public int ReflexLevel1 { get; }
    public string ReflexLevel2Text { get; }
    public string ReflexLevel3Text { get; }
    public string ReflexAdaptiveActionsText { get; }

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

    public List<KeyValuePair<int, string>> ActionOptions { get; private set; }

    public ReflexChainEditorDialog(int reflexId, int reflexLevel1,
    List<int> reflexLevel2, List<int> reflexLevel3,
    List<int> reflexAdaptiveActions, int chainId,
    ReflexChainsSystem reflexChainsSystem,
    AdaptiveActionsSystem actionsSystem)
    {
      if (reflexChainsSystem == null)
        throw new ArgumentNullException(nameof(reflexChainsSystem));

      if (actionsSystem == null)
        throw new ArgumentNullException(nameof(actionsSystem));

      ReflexId = reflexId;
      ReflexLevel1 = reflexLevel1;
      ReflexLevel2Text = ConvertIdsToText(reflexLevel2, "Level2");
      ReflexLevel3Text = ConvertIdsToText(reflexLevel3, "Level3");
      _reflexAdaptiveActions = reflexAdaptiveActions ?? new List<int>();
      ReflexAdaptiveActionsText = string.Join(", ", _reflexAdaptiveActions);
      _initialChainId = chainId;
      _reflexChainsSystem = reflexChainsSystem;
      _actionsSystem = actionsSystem;

      InitializeComponent();

      ChainLinksView = CollectionViewSource.GetDefaultView(ChainLinks);
      ChainLinks.CollectionChanged += (s, e) =>
      {
        _hasUnsavedChanges = true;
        OnPropertyChanged(nameof(CanSave));
      };

      if (_initialChainId > 0)
        LoadExistingChain();
      else
      {
        ChainName = $"Цепочка для рефлекса {ReflexId}";
        ChainDescription = $"Автоматически созданная цепочка для рефлекса {ReflexId}";

        if (_reflexAdaptiveActions.Any())
        {
          var startLink = new ChainLink
          {
            ID = 1,
            ActionId = _reflexAdaptiveActions.First(),
            SuccessNextLink = 0,
            FailureNextLink = 0,
            Description = "Стартовое звено"
          };
          ChainLinks.Add(startLink);
        }
      }

      LoadActionOptions();

      DataContext = this;
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

    private string ConvertIdsToText(List<int> ids, string converterParameter)
    {
      if (ids == null || !ids.Any())
        return "Нет";

      try
      {
        var converter = new IdListToNamesConverter();
        return converter.Convert(ids, typeof(string), converterParameter, System.Globalization.CultureInfo.CurrentCulture) as string ?? string.Empty;
      }
      catch
      {
        return string.Join(", ", ids);
      }
    }

    private void LoadExistingChain()
    {
      try
      {
        var chain = _reflexChainsSystem.GetChain(_initialChainId);
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

    private void LoadActionOptions()
    {
      ActionOptions = new List<KeyValuePair<int, string>>();

      var allActions = _actionsSystem.GetAllAdaptiveActions();
      foreach (var action in allActions.OrderBy(a => a.Id))
      {
        ActionOptions.Add(new KeyValuePair<int, string>(action.Id, $"{action.Name} (ID:{action.Id})"));
      }

      OnPropertyChanged(nameof(ActionOptions));
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

      return errorMessage.ToString();
    }

    private void AddLinkButton_Click(object sender, RoutedEventArgs e)
    {
      int newLinkId = ChainLinks.Any() ? ChainLinks.Max(l => l.ID) + 1 : 1;

      var newLink = new ChainLink
      {
        ID = newLinkId,
        ActionId = ActionOptions.FirstOrDefault().Key,
        SuccessNextLink = 0,
        FailureNextLink = 0,
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
          _editingChain.Links = links;

          var (success, _) = _reflexChainsSystem.SaveReflexChains();
          if (!success)
            throw new Exception("Не удалось сохранить цепочку");

          _chainId = _editingChain.ID;
        }
        else
        {
          var (newChainId, warnings) = _reflexChainsSystem.AddReflexChain(
              ChainName,
              ChainDescription,
              links);

          if (warnings != null && warnings.Any())
          {
            MessageBox.Show($"Предупреждения при создании цепочки:\n{string.Join("\n", warnings)}",
                "Предупреждения", MessageBoxButton.OK, MessageBoxImage.Warning);
          }

          _chainId = newChainId;

          var (saveSuccess, error) = _reflexChainsSystem.SaveReflexChains();
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