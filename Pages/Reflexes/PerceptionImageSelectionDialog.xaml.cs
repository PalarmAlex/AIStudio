using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
            Description = CreateImageDescription(image),
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
            UpdateSelectedImageDetails(selectedItem);
          }
        }

        // Подписываемся на событие выбора
        PerceptionImagesList.SelectionChanged += (s, e) =>
        {
          if (PerceptionImagesList.SelectedItem is PerceptionImageItem selectedItem)
          {
            UpdateSelectedImageDetails(selectedItem);
          }
        };
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки образов восприятия: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private string CreateImageDescription(PerceptionImagesSystem.PerceptionImage image)
    {
      return $"Образ #{image.Id}";
    }

    private string CreateInfluenceActionsDescription(PerceptionImagesSystem.PerceptionImage image)
    {
      if (image.InfluenceActionsList == null || !image.InfluenceActionsList.Any())
        return "Нет воздействий";

      return $"Воздействий: {image.InfluenceActionsList.Count}";
    }

    private string CreatePhrasesDescription(PerceptionImagesSystem.PerceptionImage image)
    {
      if (image.PhraseIdList == null || !image.PhraseIdList.Any())
        return "Нет фраз";

      return $"Фраз: {image.PhraseIdList.Count}";
    }

    private void UpdateSelectedImageDetails(PerceptionImageItem item)
    {
      var details = new System.Text.StringBuilder();
      details.AppendLine($"ID: {item.Id}");

      if (item.InfluenceActionsList.Any())
      {
        details.AppendLine($"Воздействия: {string.Join(", ", item.InfluenceActionsList)}");
      }
      else
      {
        details.AppendLine("Воздействия: отсутствуют");
      }

      if (item.PhraseIdList.Any())
      {
        details.AppendLine($"Фразы: {string.Join(", ", item.PhraseIdList)}");
      }
      else
      {
        details.AppendLine("Фразы: отсутствуют");
      }

      SelectedImageDetails.Text = details.ToString();
      SelectedPerceptionImageId = item.Id;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      if (PerceptionImagesList.SelectedItem is PerceptionImageItem selectedItem)
      {
        SelectedPerceptionImageId = selectedItem.Id;
        DialogResult = true;
      }
      else
      {
        SelectedPerceptionImageId = 0;
        DialogResult = true;
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
    }
  }

  public class PerceptionImageItem
  {
    public int Id { get; set; }
    public string Description { get; set; }
    public string InfluenceActionsDescription { get; set; }
    public string PhrasesDescription { get; set; }
    public List<int> InfluenceActionsList { get; set; }
    public List<int> PhraseIdList { get; set; }
    public bool IsSelected { get; set; }
  }
}