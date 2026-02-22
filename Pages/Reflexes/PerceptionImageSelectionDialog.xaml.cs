using ISIDA.Actions;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class PerceptionImageSelectionDialog : Window
  {
    public int SelectedPerceptionImageId { get; private set; }
    private List<PerceptionImageItem> _perceptionImages;
    private readonly PerceptionImagesSystem _perceptionImagesSystem;

    public PerceptionImageSelectionDialog(int initiallySelectedId, PerceptionImagesSystem perceptionImagesSystem)
    {
      InitializeComponent();
      _perceptionImagesSystem = perceptionImagesSystem;
      SelectedPerceptionImageId = initiallySelectedId;
      LoadPerceptionImages();
    }

    private void LoadPerceptionImages()
    {
      _perceptionImages = new List<PerceptionImageItem>();

      try
      {
        if (_perceptionImagesSystem == null) return;

        var images = _perceptionImagesSystem.GetAllPerceptionImagesList();

        foreach (var image in images.OrderBy(img => img.Id))
        {
          _perceptionImages.Add(new PerceptionImageItem
          {
            Id = image.Id,
            InfluenceActionsDescription = CreateInfluenceActionsDescription(image),
            PhrasesDescription = CreatePhrasesDescription(image),
            InfluenceActionsList = image.InfluenceActionsList ?? new List<int>(),
            PhraseIdList = image.PhraseIdList ?? new List<int>(),
            IsSelected = image.Id == SelectedPerceptionImageId
          });
        }

        PerceptionImagesList.ItemsSource = _perceptionImages;

        // Выбираем изначально выбранный элемент
        if (SelectedPerceptionImageId > 0)
        {
          var selectedItem = _perceptionImages.FirstOrDefault(item => item.Id == SelectedPerceptionImageId);
          if (selectedItem != null)
          {
            PerceptionImagesList.SelectedItem = selectedItem;
            // Прокручиваем к выбранному элементу
            PerceptionImagesList.ScrollIntoView(selectedItem);
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки образов восприятия: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private string CreateInfluenceActionsDescription(PerceptionImagesSystem.PerceptionImage image)
    {
      if (image.InfluenceActionsList == null || !image.InfluenceActionsList.Any())
        return "Нет воздействий";

      if (InfluenceActionSystem.IsInitialized)
      {
        var influenceSystem = InfluenceActionSystem.Instance;
        var allActions = influenceSystem.GetAllInfluenceActions();
        var names = image.InfluenceActionsList
            .Where(id => allActions.Any(a => a.Id == id))
            .Select(id => allActions.First(a => a.Id == id).Name)
            .ToList();
        return string.Join(", ", names);
      }
      return "InfluenceActionSystem не инициализирован";
    }

    private string CreatePhrasesDescription(PerceptionImagesSystem.PerceptionImage image)
    {
      if (image.PhraseIdList == null || !image.PhraseIdList.Any())
        return "Нет фраз";

      if (SensorySystem.IsInitialized)
      {
        var sensorySystem = SensorySystem.Instance;
        var names = new List<string>();

        foreach (var phraseId in image.PhraseIdList)
        {
          string phraseText = sensorySystem.VerbalChannel.GetPhraseFromPhraseId(phraseId);
          if (!string.IsNullOrEmpty(phraseText))
            names.Add(phraseText);
          else
            names.Add($"[ID:{phraseId}]");
        }

        return string.Join(", ", names);
      }
      return "Нет фраз";
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is RadioButton radioButton && radioButton.DataContext is PerceptionImageItem item)
      {
        // Сбрасываем выбор у всех остальных элементов
        foreach (var perceptionItem in _perceptionImages)
        {
          perceptionItem.IsSelected = perceptionItem.Id == item.Id;
        }

        // Обновляем привязки
        PerceptionImagesList.Items.Refresh();

        // Устанавливаем выбранный ID
        SelectedPerceptionImageId = item.Id;
      }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      // Находим выбранный элемент через радиокнопку
      var selectedItem = _perceptionImages.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        SelectedPerceptionImageId = selectedItem.Id;
        DialogResult = true;
      }
      else
      {
        // Если ничего не выбрано, но был первоначальный выбор - сохраняем его
        if (SelectedPerceptionImageId > 0)
        {
          DialogResult = true;
        }
        else
        {
          SelectedPerceptionImageId = 0;
          DialogResult = true;
        }
      }
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
        e.Handled = true;
      }
      else if (e.Key == Key.Enter)
      {
        OkButton_Click(sender, e);
        e.Handled = true;
      }
    }

    // Обработчик двойного клика по строке для быстрого выбора
    private void PerceptionImagesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (PerceptionImagesList.SelectedItem is PerceptionImageItem item)
      {
        // Устанавливаем выбор через радиокнопку
        foreach (var perceptionItem in _perceptionImages)
        {
          perceptionItem.IsSelected = perceptionItem.Id == item.Id;
        }
        PerceptionImagesList.Items.Refresh();
        SelectedPerceptionImageId = item.Id;

        DialogResult = true;
        Close();
      }
    }
  }

  public class PerceptionImageItem
  {
    public int Id { get; set; }
    public string InfluenceActionsDescription { get; set; }
    public string PhrasesDescription { get; set; }
    public List<int> InfluenceActionsList { get; set; }
    public List<int> PhraseIdList { get; set; }
    public bool IsSelected { get; set; }
  }
}