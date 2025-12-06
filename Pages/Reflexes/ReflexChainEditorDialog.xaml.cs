using AIStudio.Converters;
using ISIDA.Actions;
using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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
    private int _chainPriority;
    private ReflexChain _editingChain;
    private readonly int _initialChainId;
    private readonly List<int> _reflexAdaptiveActions;

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
        _chainName = value;
        OnPropertyChanged(nameof(ChainName));
        OnPropertyChanged(nameof(CanSave));
      }
    }

    public string ChainDescription
    {
      get => _chainDescription;
      set
      {
        _chainDescription = value;
        OnPropertyChanged(nameof(ChainDescription));
      }
    }

    public int ChainPriority
    {
      get => _chainPriority;
      set
      {
        _chainPriority = value;
        OnPropertyChanged(nameof(ChainPriority));
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
    public List<KeyValuePair<int, string>> LinkOptions { get; private set; }

    public ReflexChainEditorDialog(int reflexId, int reflexLevel1,
    List<int> reflexLevel2, List<int> reflexLevel3,
    List<int> reflexAdaptiveActions, int chainId,
    ReflexChainsSystem reflexChainsSystem,
    AdaptiveActionsSystem actionsSystem)
    {
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

      // Загружаем существующую цепочку если она есть
      if (_initialChainId > 0)
      {
        LoadExistingChain();
      }
      else
      {
        // Создаем новую цепочку с дефолтными значениями
        ChainName = $"Цепочка для рефлекса {ReflexId}";
        ChainDescription = $"Автоматически созданная цепочка для рефлекса {ReflexId}";
        ChainPriority = 5;

        // Добавляем стартовое звено с первым действием из рефлекса
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
      UpdateLinkOptions();

      DataContext = this;
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
          ChainPriority = chain.Priority;

          ChainLinks.Clear();
          foreach (var link in chain.Links.OrderBy(l => l.ID))
          {
            ChainLinks.Add(link);
          }
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

      // Загружаем действия из системы адаптивных действий
      var allActions = _actionsSystem.GetAllAdaptiveActions();
      foreach (var action in allActions.OrderBy(a => a.Id))
      {
        ActionOptions.Add(new KeyValuePair<int, string>(action.Id, $"{action.Name} (ID:{action.Id})"));
      }

      OnPropertyChanged(nameof(ActionOptions));
    }

    private void UpdateLinkOptions()
    {
      LinkOptions = new List<KeyValuePair<int, string>>();
      LinkOptions.Add(new KeyValuePair<int, string>(0, "Нет следующего"));

      foreach (var link in ChainLinks.OrderBy(l => l.ID))
      {
        if (SelectedLink != null && link.ID == SelectedLink.ID) continue;
        LinkOptions.Add(new KeyValuePair<int, string>(link.ID, $"Звено {link.ID}"));
      }

      OnPropertyChanged(nameof(LinkOptions));
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
        MaxCyclicRepetitions = 3, // Значение по умолчанию
        Description = $"Звено {newLinkId}"
      };

      ChainLinks.Add(newLink);
      UpdateLinkOptions();
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

      // Проверяем, не ссылаются ли другие звенья на удаляемое
      var referencingLinks = ChainLinks.Where(l =>
          l.SuccessNextLink == SelectedLink.ID ||
          l.FailureNextLink == SelectedLink.ID).ToList();

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
            if (refLink.SuccessNextLink == SelectedLink.ID)
              refLink.SuccessNextLink = 0;
            if (refLink.FailureNextLink == SelectedLink.ID)
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
      UpdateLinkOptions();
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

      // Проверяем валидность ссылок
      if (SelectedLink.SuccessNextLink > 0 && !ChainLinks.Any(l => l.ID == SelectedLink.SuccessNextLink))
      {
        MessageBox.Show($"Следующее звено при успехе (ID:{SelectedLink.SuccessNextLink}) не найдено",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      if (SelectedLink.FailureNextLink > 0 && !ChainLinks.Any(l => l.ID == SelectedLink.FailureNextLink))
      {
        MessageBox.Show($"Следующее звено при неудаче (ID:{SelectedLink.FailureNextLink}) не найдено",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      MessageBox.Show("Звено обновлено", "Успех",
          MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ValidateChainButton_Click(object sender, RoutedEventArgs e)
    {
      if (!ChainLinks.Any())
      {
        MessageBox.Show("Цепочка не содержит звеньев", "Ошибка валидации",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      // Проверяем MaxCyclicRepetitions
      foreach (var link in ChainLinks)
      {
        if (link.MaxCyclicRepetitions < 0)
        {
          MessageBox.Show($"Звено {link.ID}: Максимальное количество повторений не может быть отрицательным",
              "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (link.MaxCyclicRepetitions == 0 && (link.SuccessNextLink == link.ID || link.FailureNextLink == link.ID))
        {
          MessageBox.Show($"Звено {link.ID}: Для циклической ссылки нужно установить MaxCyclicRepetitions > 0",
              "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }
      }

      // Проверяем наличие конечных звеньев
      var terminalLinks = ChainLinks.Where(l => l.SuccessNextLink == 0 && l.FailureNextLink == 0).ToList();
      if (terminalLinks.Count == 0)
      {
        var result = MessageBox.Show("Цепочка не содержит конечных звеньев (оба следующих звена = 0).\n" +
            "Это может привести к бесконечному циклу.\n" +
            "Продолжить?",
            "Предупреждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
          return;
      }

      // Проверяем циклические ссылки
      var visited = new HashSet<int>();
      foreach (var link in ChainLinks)
      {
        if (link.SuccessNextLink > 0 && link.SuccessNextLink == link.ID)
        {
          MessageBox.Show($"Звено {link.ID} ссылается само на себя (SuccessNextLink)",
              "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if (link.FailureNextLink > 0 && link.FailureNextLink == link.ID)
        {
          MessageBox.Show($"Звено {link.ID} ссылается само на себя (FailureNextLink)",
              "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }
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

      try
      {
        var links = new List<ChainLink>(ChainLinks);

        if (_editingChain != null)
        {
          _editingChain.Name = ChainName;
          _editingChain.Description = ChainDescription;
          _editingChain.Priority = ChainPriority;
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
              ChainPriority,
              links,
              3);

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
      DialogResult = false;
      Close();
    }

    private void LinksDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      UpdateLinkOptions();
    }
  }
}