using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ISIDA.Sensors;

namespace AIStudio.Dialogs
{
  public partial class WordSelectorDialog : Window
  {
    public int SelectedWordId { get; private set; }
    private Dictionary<int, string> _allWords;
    private Dictionary<int, string> _filteredWords;

    public WordSelectorDialog(string title, int currentWordId = 0)
    {
      InitializeComponent();
      Title = title;
      SelectedWordId = currentWordId;
      LoadWords();
      ApplyFilter();
    }

    private void LoadWords()
    {
      try
      {
        if (SensorySystem.IsInitialized)
        {
          var sensorySystem = SensorySystem.Instance;
          _allWords = sensorySystem.VerbalChannel.GetAllWords();

          // Сортируем по тексту слова для удобства
          _allWords = _allWords
              .OrderBy(p => p.Value)
              .ToDictionary(p => p.Key, p => p.Value);
        }
        else
        {
          _allWords = new Dictionary<int, string>();
        }
      }
      catch
      {
        _allWords = new Dictionary<int, string>();
      }
    }

    private void ApplyFilter()
    {
      var filterText = FilterTextBox.Text?.ToLower() ?? "";

      if (string.IsNullOrWhiteSpace(filterText))
      {
        _filteredWords = _allWords;
      }
      else
      {
        _filteredWords = _allWords
            .Where(p => p.Value.ToLower().Contains(filterText))
            .ToDictionary(p => p.Key, p => p.Value);
      }

      WordListBox.ItemsSource = _filteredWords
          .Select(p => new { Id = p.Key, Text = p.Value })
          .ToList();

      StatusTextBlock.Text = $"Найдено фраз: {_filteredWords.Count}";
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void WordListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (WordListBox.SelectedItem != null)
      {
        var selectedItem = (dynamic)WordListBox.SelectedItem;
        SelectedWordId = selectedItem.Id;
        DialogResult = true;
        Close();
      }
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
      if (WordListBox.SelectedItem != null)
      {
        var selectedItem = (dynamic)WordListBox.SelectedItem;
        SelectedWordId = selectedItem.Id;
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
      SelectedWordId = 0;
      DialogResult = true;
      Close();
    }
  }
}