using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ISIDA.Sensors;

namespace AIStudio.Dialogs
{
  public partial class PhraseSelectorDialog : Window
  {
    public int SelectedPhraseId { get; private set; }
    private Dictionary<int, string> _allPhrases;
    private Dictionary<int, string> _filteredPhrases;

    public PhraseSelectorDialog(string title, int currentPhraseId = 0)
    {
      InitializeComponent();
      Title = title;
      SelectedPhraseId = currentPhraseId;
      LoadPhrases();
      ApplyFilter();
    }

    private void LoadPhrases()
    {
      try
      {
        if (SensorySystem.IsInitialized)
        {
          var sensorySystem = SensorySystem.Instance;
          _allPhrases = sensorySystem.VerbalChannel.GetAllPhrases();

          // Сортируем по тексту фразы для удобства
          _allPhrases = _allPhrases
              .OrderBy(p => p.Value)
              .ToDictionary(p => p.Key, p => p.Value);
        }
        else
        {
          _allPhrases = new Dictionary<int, string>();
        }
      }
      catch
      {
        _allPhrases = new Dictionary<int, string>();
      }
    }

    private void ApplyFilter()
    {
      var filterText = FilterTextBox.Text?.ToLower() ?? "";

      if (string.IsNullOrWhiteSpace(filterText))
      {
        _filteredPhrases = _allPhrases;
      }
      else
      {
        _filteredPhrases = _allPhrases
            .Where(p => p.Value.ToLower().Contains(filterText))
            .ToDictionary(p => p.Key, p => p.Value);
      }

      PhrasesListBox.ItemsSource = _filteredPhrases
          .Select(p => new { Id = p.Key, Text = p.Value })
          .ToList();

      StatusTextBlock.Text = $"Найдено фраз: {_filteredPhrases.Count}";
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void PhrasesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (PhrasesListBox.SelectedItem != null)
      {
        var selectedItem = (dynamic)PhrasesListBox.SelectedItem;
        SelectedPhraseId = selectedItem.Id;
        DialogResult = true;
        Close();
      }
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
      if (PhrasesListBox.SelectedItem != null)
      {
        var selectedItem = (dynamic)PhrasesListBox.SelectedItem;
        SelectedPhraseId = selectedItem.Id;
        DialogResult = true;
      }
      else
      {
        MessageBox.Show("Выберите фразу из списка", "Внимание",
            MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
      SelectedPhraseId = 0;
      DialogResult = true;
      Close();
    }
  }
}