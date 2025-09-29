using System;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class RelayCommand : ICommand
  {
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;
    private EventHandler _canExecuteChanged;

    public event EventHandler CanExecuteChanged
    {
      add
      {
        _canExecuteChanged += value;
        CommandManager.RequerySuggested += value;
      }
      remove
      {
        _canExecuteChanged -= value;
        CommandManager.RequerySuggested -= value;
      }
    }

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
      _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
      try
      {
        return _canExecute == null || _canExecute(parameter);
      }
      catch
      {
        return false;
      }
    }

    public void Execute(object parameter)
    {
      if (CanExecute(parameter))
      {
        _execute(parameter);
      }
    }

    public void RaiseCanExecuteChanged()
    {
      _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
  }
}
