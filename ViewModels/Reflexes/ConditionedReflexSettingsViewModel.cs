using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ISIDA.Common;
using ISIDA.Reflexes;

namespace AIStudio.ViewModels
{
  public class ConditionedReflexSettingsViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ConditionedReflexesSystem _conditionedReflexesSystem;
    private bool _isSaving;

    private bool _hasChanges;
    public bool HasChanges
    {
      get => _hasChanges;
      set
      {
        if (_hasChanges != value)
        {
          _hasChanges = value;
          OnPropertyChanged(nameof(HasChanges));
        }
      }
    }

    public ConditionedReflexesSystem.ConditionedReflexSettings Settings { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ApplyCommand { get; }

    public event EventHandler SettingsSaved;
    public event EventHandler SettingsCancelled;

    public ConditionedReflexSettingsViewModel(ConditionedReflexesSystem conditionedReflexesSystem)
    {
      _conditionedReflexesSystem = conditionedReflexesSystem ?? throw new ArgumentNullException(nameof(conditionedReflexesSystem));

      LoadSettings();

      SaveCommand = new RelayCommand(SaveSettings);
      CancelCommand = new RelayCommand(Cancel);
      ApplyCommand = new RelayCommand(ApplySettings);

      PropertyChanged += (s, e) =>
      {
        if (e.PropertyName.StartsWith("Settings."))
        {
          HasChanges = true;
        }
      };
    }

    private void LoadSettings()
    {
      Settings = new ConditionedReflexesSystem.ConditionedReflexSettings
      {
        LearningRate = _conditionedReflexesSystem.Settings.LearningRate,
        DecayRate = _conditionedReflexesSystem.Settings.DecayRate,
        ActivationThreshold = _conditionedReflexesSystem.Settings.ActivationThreshold,
        TimeWindowPulses = _conditionedReflexesSystem.Settings.TimeWindowPulses,
        MinAssociationStrength = _conditionedReflexesSystem.Settings.MinAssociationStrength,
        MaxAssociationStrength = _conditionedReflexesSystem.Settings.MaxAssociationStrength,
        MaxInactivationTime = _conditionedReflexesSystem.Settings.MaxInactivationTime
      };

      OnPropertyChanged(nameof(Settings));
    }

    private void SaveSettings(object parameter)
    {
      if (_isSaving) return;

      _isSaving = true;
      try
      {
        if (!ValidateSettings())
        {
          return;
        }

        // Применяем настройки к системе
        ApplySettingsToSystem();

        // Сохраняем настройки в файл
        var (success, error) = _conditionedReflexesSystem.SaveConditionedReflexSettings();
        if (success)
        {
          MessageBox.Show("Настройки успешно сохранены!", "Успех",
              MessageBoxButton.OK, MessageBoxImage.Information);
          SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        else
        {
          MessageBox.Show($"Ошибка при сохранении настроек:\n{error}",
              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при сохранении настроек:\n{ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      finally
      {
        _isSaving = false;
      }
    }

    private void ApplySettings(object parameter)
    {
      if (!ValidateSettings())
      {
        return;
      }

      ApplySettingsToSystem();
      HasChanges = false;
      MessageBox.Show("Настройки применены!", "Успех",
          MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool ValidateSettings()
    {
      var errors = new List<string>();

      // Валидация коэффициента обучения
      var learningRateValidation = SettingsValidator.ValidateLearningRate(Settings.LearningRate);
      if (!learningRateValidation.isValid)
        errors.Add(learningRateValidation.errorMessage);

      // Валидация коэффициента затухания
      var decayRateValidation = SettingsValidator.ValidateDecayRate(Settings.DecayRate);
      if (!decayRateValidation.isValid)
        errors.Add(decayRateValidation.errorMessage);

      // Валидация порога активации
      var activationThresholdValidation = SettingsValidator.ValidateActivationThreshold(Settings.ActivationThreshold);
      if (!activationThresholdValidation.isValid)
        errors.Add(activationThresholdValidation.errorMessage);

      // Валидация временного окна корреляции
      var timeWindowValidation = SettingsValidator.ValidateTimeWindowPulses(Settings.TimeWindowPulses);
      if (!timeWindowValidation.isValid)
        errors.Add(timeWindowValidation.errorMessage);

      // Валидация минимальной крепости связи
      var minStrengthValidation = SettingsValidator.ValidateMinAssociationStrength(Settings.MinAssociationStrength);
      if (!minStrengthValidation.isValid)
        errors.Add(minStrengthValidation.errorMessage);

      // Валидация максимального времени жизни
      var maxInactivationValidation = SettingsValidator.ValidateMaxInactivationTime(Settings.MaxInactivationTime);
      if (!maxInactivationValidation.isValid)
        errors.Add(maxInactivationValidation.errorMessage);

      // Дополнительная валидация для MaxAssociationStrength
      if (Settings.MaxAssociationStrength != 1.0f)
        errors.Add("Максимальная крепость связи (MaxAssociationStrength) должна быть равна 1.0");

      if (errors.Any())
      {
        MessageBox.Show($"Ошибки валидации:\n{string.Join("\n", errors)}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      return true;
    }

    private void ApplySettingsToSystem()
    {
      _conditionedReflexesSystem.Settings.LearningRate = Math.Max(0.1f, Math.Min(Settings.LearningRate, 0.3f));
      _conditionedReflexesSystem.Settings.DecayRate = Math.Max(0.95f, Math.Min(Settings.DecayRate, 0.99f));
      _conditionedReflexesSystem.Settings.ActivationThreshold = Math.Max(0.5f, Math.Min(Settings.ActivationThreshold, 0.7f));
      _conditionedReflexesSystem.Settings.TimeWindowPulses = Math.Max(1, Math.Min(Settings.TimeWindowPulses, 20));
      _conditionedReflexesSystem.Settings.MinAssociationStrength = Math.Max(0.01f, Math.Min(Settings.MinAssociationStrength, 0.3f));
      _conditionedReflexesSystem.Settings.MaxInactivationTime = Math.Max(100, Math.Min(Settings.MaxInactivationTime, 10000));
    }

    private void Cancel(object parameter)
    {
      SettingsCancelled?.Invoke(this, EventArgs.Empty);
    }
  }
}