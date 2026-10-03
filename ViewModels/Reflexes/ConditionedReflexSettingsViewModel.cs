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
        InitialLifetimePulses = _conditionedReflexesSystem.Settings.InitialLifetimePulses,
        ActiveExtinctionRate = _conditionedReflexesSystem.Settings.ActiveExtinctionRate,
        PassiveDecayPeriodPulses = _conditionedReflexesSystem.Settings.PassiveDecayPeriodPulses,
        PassiveDecayFallbackPeriodPulses = _conditionedReflexesSystem.Settings.PassiveDecayFallbackPeriodPulses,
        HigherOrderStrengthReductionCoefficient = _conditionedReflexesSystem.Settings.HigherOrderStrengthReductionCoefficient,
        CompetitionStrengthRatioThreshold = _conditionedReflexesSystem.Settings.CompetitionStrengthRatioThreshold,
        TieBreakPreferSmallerReflexId = _conditionedReflexesSystem.Settings.TieBreakPreferSmallerReflexId,
        EnableCompetitiveLearning = _conditionedReflexesSystem.Settings.EnableCompetitiveLearning,
        CompetitionSuppressionCoefficient = _conditionedReflexesSystem.Settings.CompetitionSuppressionCoefficient,
        InitialStrengthBonus = _conditionedReflexesSystem.Settings.InitialStrengthBonus,
        AuthoritativeStrength = _conditionedReflexesSystem.Settings.AuthoritativeStrength,
        EstablishedStrengthThreshold = _conditionedReflexesSystem.Settings.EstablishedStrengthThreshold,
        ActivationReinforcementFraction = _conditionedReflexesSystem.Settings.ActivationReinforcementFraction,
        MaxLifetimePulsesCap = _conditionedReflexesSystem.Settings.MaxLifetimePulsesCap,
        SensoryDecayPeriodPulses = _conditionedReflexesSystem.Settings.SensoryDecayPeriodPulses,
        SensoryStrengthFloor = _conditionedReflexesSystem.Settings.SensoryStrengthFloor,
        SensoryHighStrengthThreshold = _conditionedReflexesSystem.Settings.SensoryHighStrengthThreshold,
        SensoryHighStrengthDecayRate = _conditionedReflexesSystem.Settings.SensoryHighStrengthDecayRate,
        SensoryMidStrengthThreshold = _conditionedReflexesSystem.Settings.SensoryMidStrengthThreshold
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

      var learningRateValidation = SettingsValidator.ValidateLearningRate(Settings.LearningRate);
      if (!learningRateValidation.isValid)
        errors.Add(learningRateValidation.errorMessage);

      var activationThresholdValidation = SettingsValidator.ValidateActivationThreshold(Settings.ActivationThreshold);
      if (!activationThresholdValidation.isValid)
        errors.Add(activationThresholdValidation.errorMessage);

      var decayRateValidation = SettingsValidator.ValidateDecayRate(Settings.DecayRate);
      if (!decayRateValidation.isValid)
        errors.Add(decayRateValidation.errorMessage);

      var initialLifetimeValidation = SettingsValidator.ValidateInitialLifetimePulses(Settings.InitialLifetimePulses);
      if (!initialLifetimeValidation.isValid)
        errors.Add(initialLifetimeValidation.errorMessage);

      var activeExtinctionValidation = SettingsValidator.ValidateActiveExtinctionRate(Settings.ActiveExtinctionRate);
      if (!activeExtinctionValidation.isValid)
        errors.Add(activeExtinctionValidation.errorMessage);

      var timeWindowValidation = SettingsValidator.ValidateTimeWindowPulses(Settings.TimeWindowPulses);
      if (!timeWindowValidation.isValid)
        errors.Add(timeWindowValidation.errorMessage);

      var minStrengthValidation = SettingsValidator.ValidateMinAssociationStrength(Settings.MinAssociationStrength);
      if (!minStrengthValidation.isValid)
        errors.Add(minStrengthValidation.errorMessage);

      var reductionCoeffValidation = SettingsValidator.ValidateHigherOrderStrengthReductionCoefficient(Settings.HigherOrderStrengthReductionCoefficient);
      if (!reductionCoeffValidation.isValid)
        errors.Add(reductionCoeffValidation.errorMessage);

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
      _conditionedReflexesSystem.Settings.ActivationThreshold = Math.Max(0.5f, Math.Min(Settings.ActivationThreshold, 0.7f));
      _conditionedReflexesSystem.Settings.InitialLifetimePulses =
          Math.Max(3600, Math.Min(Settings.InitialLifetimePulses, 604800));
      _conditionedReflexesSystem.Settings.ActiveExtinctionRate =
          Math.Max(0.01f, Math.Min(Settings.ActiveExtinctionRate, 0.2f));
      _conditionedReflexesSystem.Settings.TimeWindowPulses = Math.Max(1, Math.Min(Settings.TimeWindowPulses, 20));
      _conditionedReflexesSystem.Settings.MinAssociationStrength = Math.Max(0.01f, Math.Min(Settings.MinAssociationStrength, 0.3f));
      _conditionedReflexesSystem.Settings.HigherOrderStrengthReductionCoefficient = Math.Max(1.2f, Math.Min(Settings.HigherOrderStrengthReductionCoefficient, 3.0f));
      _conditionedReflexesSystem.Settings.CompetitionStrengthRatioThreshold =
          Math.Max(0.5f, Math.Min(Settings.CompetitionStrengthRatioThreshold, 0.9f));
      _conditionedReflexesSystem.Settings.TieBreakPreferSmallerReflexId = Settings.TieBreakPreferSmallerReflexId;

      // Коэффициент затухания λ сенсорных ассоциаций (0.95-0.99)
      _conditionedReflexesSystem.Settings.DecayRate = Math.Max(0.95f, Math.Min(Settings.DecayRate, 0.99f));

      // Периоды пассивного угасания: резервный — строго положительный
      _conditionedReflexesSystem.Settings.PassiveDecayPeriodPulses = Math.Max(0, Settings.PassiveDecayPeriodPulses);
      _conditionedReflexesSystem.Settings.PassiveDecayFallbackPeriodPulses = Math.Max(1, Settings.PassiveDecayFallbackPeriodPulses);

      // Конкурентное обучение
      _conditionedReflexesSystem.Settings.EnableCompetitiveLearning = Settings.EnableCompetitiveLearning;
      _conditionedReflexesSystem.Settings.CompetitionSuppressionCoefficient =
          Math.Max(0f, Math.Min(Settings.CompetitionSuppressionCoefficient, 1f));

      // Начальная крепость и установление
      _conditionedReflexesSystem.Settings.InitialStrengthBonus =
          Math.Max(0f, Math.Min(Settings.InitialStrengthBonus, 1f));
      _conditionedReflexesSystem.Settings.AuthoritativeStrength =
          Math.Max(0f, Math.Min(Settings.AuthoritativeStrength, 1f));
      _conditionedReflexesSystem.Settings.EstablishedStrengthThreshold =
          Math.Max(0f, Math.Min(Settings.EstablishedStrengthThreshold, 1f));
      _conditionedReflexesSystem.Settings.ActivationReinforcementFraction =
          Math.Max(0f, Math.Min(Settings.ActivationReinforcementFraction, 1f));

      // Потолок TTL не может быть меньше начального лимита простоя
      _conditionedReflexesSystem.Settings.MaxLifetimePulsesCap =
          Math.Max(_conditionedReflexesSystem.Settings.InitialLifetimePulses, Settings.MaxLifetimePulsesCap);

      // Сенсорные ассоциации CS↔CS
      _conditionedReflexesSystem.Settings.SensoryDecayPeriodPulses = Math.Max(1, Settings.SensoryDecayPeriodPulses);
      _conditionedReflexesSystem.Settings.SensoryStrengthFloor =
          Math.Max(0f, Math.Min(Settings.SensoryStrengthFloor, 1f));
      _conditionedReflexesSystem.Settings.SensoryHighStrengthThreshold =
          Math.Max(0f, Math.Min(Settings.SensoryHighStrengthThreshold, 1f));
      _conditionedReflexesSystem.Settings.SensoryHighStrengthDecayRate =
          Math.Max(0.9f, Math.Min(Settings.SensoryHighStrengthDecayRate, 1f));
      _conditionedReflexesSystem.Settings.SensoryMidStrengthThreshold =
          Math.Max(0f, Math.Min(Settings.SensoryMidStrengthThreshold, 1f));
    }

    private void Cancel(object parameter)
    {
      SettingsCancelled?.Invoke(this, EventArgs.Empty);
    }
  }
}
