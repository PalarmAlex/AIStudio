using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace AIStudio.Dialogs
{
  public partial class ActionInfluenceStileEditor : Window
  {
    private readonly ObservableCollection<ActionInfluenceItem> _actionItems;
    private readonly GomeostasSystem.BehaviorStyle _behaviorStyle;
    private bool IsWarningShown = false;

    public Dictionary<int, int> ActionInfluences =>
        _actionItems.Where(x => x.Influence != 0)
                   .ToDictionary(x => x.ActionId, x => x.Influence);

    public ActionInfluenceStileEditor(string title, GomeostasSystem.BehaviorStyle behaviorStyle)
    {
      InitializeComponent();
      Title = title;
      _behaviorStyle = behaviorStyle;
      _actionItems = new ObservableCollection<ActionInfluenceItem>();

      LoadActions();
      DataContext = this;
    }

    public ObservableCollection<ActionInfluenceItem> ActionItems => _actionItems;

    private void LoadActions()
    {
      try
      {
        var allActions = AdaptiveActionsSystem.Instance.GetAllAdaptiveActions();

        foreach (var action in allActions.OrderBy(a => a.Id))
        {
          _behaviorStyle.StileActionInfluence.TryGetValue(action.Id, out int currentInfluence);

          _actionItems.Add(new ActionInfluenceItem
          {
            ActionId = action.Id,
            ActionName = action.Name,
            Influence = currentInfluence
          });
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки действий: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void InfluenceTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
      var textBox = (TextBox)sender;
      string currentText = textBox.Text;
      int selectionStart = textBox.SelectionStart;
      int selectionLength = textBox.SelectionLength;

      // Удаляем выделенный текст
      string newText = currentText.Remove(selectionStart, selectionLength);
      newText = newText.Insert(selectionStart, e.Text);

      // Разрешаем только цифры и минус (только в начале)
      if (e.Text == "-")
      {
        // Минус разрешен только в начале строки
        if (selectionStart != 0 || currentText.Contains("-"))
        {
          e.Handled = true;
          return;
        }
      }
      else if (!char.IsDigit(e.Text, 0))
      {
        e.Handled = true;
        return;
      }

      // Проверяем диапазон -5..+5
      if (int.TryParse(newText, out int value))
      {
        if (value < -5 || value > 5)
        {
          e.Handled = true;
          ShowRangeWarning();
        }
      }
      else if (newText == "-") // Разрешаем минус как промежуточное значение
      {
        // Пропускаем проверку
      }
      else
      {
        e.Handled = true;
      }
    }

    private void InfluenceTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      var textBox = (TextBox)sender;

      if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text == "-")
      {
        textBox.Text = "0";
        return;
      }

      if (int.TryParse(textBox.Text, out int value))
      {
        // Корректируем значение в допустимый диапазон
        if (value < -5)
          value = -5;
        else if (value > 5)
          value = 5;

        textBox.Text = value.ToString();
      }
      else
      {
        textBox.Text = "0";
      }
    }

    private void ShowRangeWarning()
    {
      // Показываем сообщение только если оно еще не показано
      if (!IsWarningShown)
      {
        MessageBox.Show("Значение должно быть в диапазоне от -5 до +5", "Ошибка ввода",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        IsWarningShown = true;

        // Сбрасываем флаг после небольшой задержки
        Dispatcher.BeginInvoke(new Action(() =>
        {
          IsWarningShown = false;
        }), DispatcherPriority.ApplicationIdle);
      }
    }

    private void InfluenceTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      // Запрещаем пробел
      if (e.Key == Key.Space)
      {
        e.Handled = true;
      }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close(); // Закрываем окно при нажатии Esc
        e.Handled = true;
      }
    }
  }

  public class ActionInfluenceItem : INotifyPropertyChanged
  {
    private int _actionId;
    private string _actionName;
    private int _influence;

    public int ActionId
    {
      get => _actionId;
      set => SetProperty(ref _actionId, value);
    }

    public string ActionName
    {
      get => _actionName;
      set => SetProperty(ref _actionName, value);
    }

    public int Influence
    {
      get => _influence;
      set => SetProperty(ref _influence, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(field, value)) return false;
      field = value;
      OnPropertyChanged(propertyName);
      return true;
    }
  }
}
