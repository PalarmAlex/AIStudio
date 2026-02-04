using AIStudio.ViewModels;
using ISIDA.Psychic.Automatism;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class ActionsImageSelectorDialog : Window, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ActionsImagesSystem _actionsImagesSystem;
    private int _selectedActionsImageId;

    public int SelectedActionsImageId => _selectedActionsImageId;

    // Коллекции для отображения
    private ObservableCollection<ActionsImageDisplayItem> _allActionsImages = new ObservableCollection<ActionsImageDisplayItem>();
    private ICollectionView _actionsImagesView;
    public ICollectionView ActionsImagesView => _actionsImagesView;

    // Фильтры
    private int? _selectedTypeFilter;
    private bool? _hasActionsFilter;
    private bool? _hasPhrasesFilter;

    // Выбранный элемент
    private ActionsImageDisplayItem _selectedActionsImage;
    public ActionsImageDisplayItem SelectedActionsImage
    {
      get => _selectedActionsImage;
      set
      {
        _selectedActionsImage = value;
        OnPropertyChanged(nameof(SelectedActionsImage));
        OnPropertyChanged(nameof(CanSelect));
        OnPropertyChanged(nameof(SelectionInfo));
      }
    }

    // Команды
    public ICommand SelectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand ShowDetailsCommand { get; }

    public bool CanSelect => SelectedActionsImage != null;

    public string SelectionInfo
    {
      get
      {
        if (SelectedActionsImage == null)
          return "Выберите образ действий из списка";

        return $"Выбран образ ID: {SelectedActionsImage.Id}";
      }
    }

    // Опции фильтров
    public List<KeyValuePair<int?, string>> TypeFilterOptions { get; } = new List<KeyValuePair<int?, string>>
        {
            new KeyValuePair<int?, string>(null, "Все типы"),
            new KeyValuePair<int?, string>(0, "Объективное действие"),
            new KeyValuePair<int?, string>(1, "Субъективное предположение")
        };

    public int? SelectedTypeFilter
    {
      get => _selectedTypeFilter;
      set
      {
        _selectedTypeFilter = value;
        OnPropertyChanged(nameof(SelectedTypeFilter));
        ApplyFilters();
      }
    }

    public bool? HasActionsFilter
    {
      get => _hasActionsFilter;
      set
      {
        _hasActionsFilter = value;
        OnPropertyChanged(nameof(HasActionsFilter));
        ApplyFilters();
      }
    }

    public bool? HasPhrasesFilter
    {
      get => _hasPhrasesFilter;
      set
      {
        _hasPhrasesFilter = value;
        OnPropertyChanged(nameof(HasPhrasesFilter));
        ApplyFilters();
      }
    }

    public ActionsImageSelectorDialog(ActionsImagesSystem actionsImagesSystem, int preselectedId = 0)
    {
      if (actionsImagesSystem == null)
        throw new ArgumentNullException(nameof(actionsImagesSystem));

      _actionsImagesSystem = actionsImagesSystem;
      _selectedActionsImageId = preselectedId;

      InitializeComponent();

      // Инициализация команд
      SelectCommand = new RelayCommand(Select, _ => CanSelect);
      CancelCommand = new RelayCommand(_ => Cancel());
      ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
      ShowDetailsCommand = new RelayCommand(ShowDetails);

      // Настройка представления
      _actionsImagesView = CollectionViewSource.GetDefaultView(_allActionsImages);
      _actionsImagesView.Filter = FilterActionsImages;

      // Загрузка данных
      LoadActionsImages();

      // Предварительный выбор
      if (preselectedId > 0)
      {
        var preselected = _allActionsImages.FirstOrDefault(x => x.Id == preselectedId);
        if (preselected != null)
        {
          SelectedActionsImage = preselected;
          // Прокручиваем к выбранному элементу
          Dispatcher.BeginInvoke(new Action(() =>
          {
            ActionsImagesGrid.ScrollIntoView(preselected);
          }));
        }
      }

      DataContext = this;
    }

    private void LoadActionsImages()
    {
      try
      {
        _allActionsImages.Clear();

        var allImages = _actionsImagesSystem.GetAllActionsImagesList();
        foreach (var image in allImages.OrderBy(x => x.Id))
        {
          var displayItem = new ActionsImageDisplayItem
          {
            Id = image.Id,
            Kind = image.Kind,
            KindText = image.Kind == 0 ? "Объективное действие" : "Субъективное предположение",
            ActIdList = image.ActIdList ?? new List<int>(),
            PhraseIdList = image.PhraseIdList ?? new List<int>(),
            ToneId = image.ToneId,
            MoodId = image.MoodId,
            ToneText = ActionsImagesSystem.GetToneText(image.ToneId),
            MoodText = ActionsImagesSystem.GetMoodText(image.MoodId),
            ActIdListString = image.ActIdList != null && image.ActIdList.Any()
                  ? string.Join(", ", image.ActIdList)
                  : "Нет",
            PhraseIdListString = image.PhraseIdList != null && image.PhraseIdList.Any()
                  ? string.Join(", ", image.PhraseIdList)
                  : "Нет"
          };

          _allActionsImages.Add(displayItem);
        }

        ApplyFilters();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки образов действий: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private bool FilterActionsImages(object item)
    {
      if (!(item is ActionsImageDisplayItem image))
        return false;

      // Фильтр по типу
      bool typeMatch = !SelectedTypeFilter.HasValue || image.Kind == SelectedTypeFilter.Value;

      // Фильтр по наличию действий
      bool actionsMatch = !HasActionsFilter.HasValue ||
                         (HasActionsFilter.Value && image.ActIdList.Any()) ||
                         (!HasActionsFilter.Value && !image.ActIdList.Any());

      // Фильтр по наличию фраз
      bool phrasesMatch = !HasPhrasesFilter.HasValue ||
                         (HasPhrasesFilter.Value && image.PhraseIdList.Any()) ||
                         (!HasPhrasesFilter.Value && !image.PhraseIdList.Any());

      return typeMatch && actionsMatch && phrasesMatch;
    }

    private void ApplyFilters()
    {
      _actionsImagesView.Refresh();
      OnPropertyChanged(nameof(SelectionInfo));
    }

    private void ClearFilters()
    {
      SelectedTypeFilter = null;
      HasActionsFilter = null;
      HasPhrasesFilter = null;
    }

    private void ShowDetails(object parameter)
    {
      if (parameter is ActionsImageDisplayItem image)
      {
        string details = $"Подробная информация об образе действий:\n\n" +
                       $"ID: {image.Id}\n" +
                       $"Тип: {image.KindText}\n" +
                       $"Действия: {image.ActIdListString}\n" +
                       $"Фразы: {image.PhraseIdListString}\n" +
                       $"Тон: {image.ToneText} (ID: {image.ToneId})\n" +
                       $"Настроение: {image.MoodText} (ID: {image.MoodId})\n\n" +
                       $"Всего элементов: {image.ActIdList.Count + image.PhraseIdList.Count}";

        MessageBox.Show(details, $"Образ действий ID: {image.Id}",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
    }

    private void Select(object parameter)
    {
      if (SelectedActionsImage != null)
      {
        _selectedActionsImageId = SelectedActionsImage.Id;
        DialogResult = true;
        Close();
      }
    }

    private void Cancel()
    {
      DialogResult = false;
      Close();
    }

    private void ActionsImagesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (SelectedActionsImage != null && CanSelect)
      {
        Select(null);
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      if (SelectedActionsImage != null)
      {
        ActionsImagesGrid.Focus();
      }
    }

    // Класс для отображения
    public class ActionsImageDisplayItem
    {
      public int Id { get; set; }
      public int Kind { get; set; }
      public string KindText { get; set; }
      public List<int> ActIdList { get; set; } = new List<int>();
      public List<int> PhraseIdList { get; set; } = new List<int>();
      public int ToneId { get; set; }
      public int MoodId { get; set; }
      public string ToneText { get; set; }
      public string MoodText { get; set; }
      public string ActIdListString { get; set; }
      public string PhraseIdListString { get; set; }
    }
  }
}