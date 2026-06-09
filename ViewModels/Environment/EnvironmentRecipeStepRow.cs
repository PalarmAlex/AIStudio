using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Строка шага рецепта (<c>invoke</c> или <c>comment</c>).</summary>
  public sealed class EnvironmentRecipeStepRow : INotifyPropertyChanged
  {
    private string _stepKind = EnvironmentRecipeStepSchemaHelper.StepTypeInvoke;
    private string _handlerId = string.Empty;
    private string _commentText = string.Empty;
    private string _summary = string.Empty;
    private string _validationError = string.Empty;

    public EnvironmentRecipeStepRow()
    {
      Args = new Dictionary<string, string>();
    }

    /// <summary><c>invoke</c> или <c>comment</c>.</summary>
    public string StepKind
    {
      get => _stepKind;
      set
      {
        string normalized = value ?? string.Empty;
        if (_stepKind == normalized)
          return;
        _stepKind = normalized;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DisplayAction));
        OnPropertyChanged(nameof(IsComment));
        OnPropertyChanged(nameof(IsInvoke));
      }
    }

    /// <summary>ID handler из handlers-catalog.json.</summary>
    public string HandlerId
    {
      get => _handlerId;
      set
      {
        string normalized = value ?? string.Empty;
        if (_handlerId == normalized)
          return;
        _handlerId = normalized;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DisplayAction));
      }
    }

    /// <summary>Flat-аргументы handler.</summary>
    public Dictionary<string, string> Args { get; }

    /// <summary>Текст комментария.</summary>
    public string CommentText
    {
      get => _commentText;
      set
      {
        string normalized = value ?? string.Empty;
        if (_commentText == normalized)
          return;
        _commentText = normalized;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DisplayAction));
      }
    }

    /// <summary>Краткое описание для таблицы.</summary>
    public string Summary
    {
      get => _summary;
      set
      {
        string normalized = value ?? string.Empty;
        if (_summary == normalized)
          return;
        _summary = normalized;
        OnPropertyChanged();
      }
    }

    public string ValidationError
    {
      get => _validationError;
      set
      {
        string normalized = value ?? string.Empty;
        if (_validationError == normalized)
          return;
        _validationError = normalized;
        OnPropertyChanged();
        OnPropertyChanged(nameof(HasValidationError));
      }
    }

    public bool HasValidationError => !string.IsNullOrWhiteSpace(_validationError);

    public bool IsComment =>
        string.Equals(StepKind, EnvironmentRecipeStepSchemaHelper.StepTypeComment, System.StringComparison.OrdinalIgnoreCase);

    public bool IsInvoke => !IsComment;

    public string DisplayAction =>
        IsComment ? "💬 " + (string.IsNullOrWhiteSpace(CommentText) ? "комментарий" : CommentText) : HandlerId;

    public event PropertyChangedEventHandler PropertyChanged;

    internal void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
