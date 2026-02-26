using AIStudio.ViewModels;
using AIStudio.Dialogs;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages
{
  public partial class AgentPropertiesDialog : Window
  {
    private readonly GomeostasSystem _gomeostas;
    private List<int> _stressBehaviorIds = new List<int>();
    private List<int> _threatResponseIds = new List<int>();
    private List<int> _rewardResponseIds = new List<int>();
    private List<int> _punishmentResponseIds = new List<int>();

    private static readonly string[] DefaultBaseArchetype = { "Исследователь", "Социальный", "Агрессор", "Тревожный", "Игривый", "Ленивый", "Цикличный меланхолик" };
    private static readonly string[] DefaultKeyMotivation = { "Выживание", "Познание", "Безопасность", "Общение", "Доминирование", "Внутренний баланс" };
    private static readonly string[] DefaultTemperament = { "Низкая", "Средняя", "Высокая" };
    private static readonly string[] DefaultSociality = { "Одиночка", "Избирательный", "Стайный", "Зависимый" };
    private static readonly string[] DefaultSpecialTriggersTaboos = { "Резкая смена контекста", "Одиночество", "Принуждение" };

    public AgentPropertiesDialog(GomeostasSystem gomeostas)
    {
      InitializeComponent();
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));

      LoadStages();
      LoadData();
    }

    private void LoadStages()
    {
      var stages = new ObservableCollection<EvolutionStageItem>();
      for (int i = 0; i <= 5; i++)
        stages.Add(new EvolutionStageItem { StageNumber = i, Description = EvolutionStageItem.GetDescription(i) });
      ComboStage.ItemsSource = stages;
    }

    private void ComboStage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count == 0) return;

      int newStage = (e.AddedItems[0] as EvolutionStageItem)?.StageNumber ?? 0;
      var state = _gomeostas.GetAgentState();
      if (state == null) return;
      int currentStage = state.EvolutionStage;

      if (newStage == currentStage) return;

      if (newStage > currentStage + 1)
      {
        MessageBox.Show(
          "Недопустимый переход! Можно переходить только на следующую стадию.",
          "Ошибка",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
        ComboStage.Dispatcher.InvokeAsync(() => { ComboStage.SelectedValue = currentStage; });
        return;
      }

      bool isBackward = newStage < currentStage;
      string title = isBackward ? "ВНИМАНИЕ: Возврат на предыдущую стадию" : "Подтверждение перехода";
      string message = isBackward
        ? $"Возврат на стадию {newStage} очистит все данные последующих стадий. Продолжить?"
        : $"Перейти со стадии {currentStage} на стадию {newStage}?";

      var result = MessageBox.Show(message, title,
        MessageBoxButton.YesNo,
        isBackward ? MessageBoxImage.Warning : MessageBoxImage.Question);

      if (result != MessageBoxResult.Yes)
      {
        ComboStage.Dispatcher.InvokeAsync(() => { ComboStage.SelectedValue = currentStage; });
        return;
      }

      var stageResult = _gomeostas.SetEvolutionStage(newStage, isBackward, false);

      if (stageResult.Success)
      {
        LoadData();

        var (saveSuccess, error) = _gomeostas.SaveAgentProperties();
        if (!saveSuccess)
        {
          MessageBox.Show($"Ошибка сохранения: {error}",
            "Предупреждение",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        }
        if (stageResult.Success && saveSuccess)
          MessageBox.Show(stageResult.Message,
            "Изменение стадии развития агента",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
      else
      {
        MessageBox.Show(stageResult.Message, "Ошибка",
          MessageBoxButton.OK, MessageBoxImage.Error);
        ComboStage.Dispatcher.InvokeAsync(() => { ComboStage.SelectedValue = currentStage; });
      }
    }

    private void LoadData()
    {
      var state = _gomeostas.GetAgentState();
      if (state == null) return;

      TextBoxAgentName.Text = state.Name ?? string.Empty;
      TextBoxDescription.Text = state.Description ?? string.Empty;
      ComboStage.SelectedValue = state.EvolutionStage;

      LoadCombo(ComboBaseArchetype, state.BaseArchetype, state.BaseArchetypeValues, DefaultBaseArchetype);
      LoadCombo(ComboKeyMotivation, state.KeyMotivation, state.KeyMotivationValues, DefaultKeyMotivation);
      ComboTemperamentActivity.ItemsSource = new List<string>(DefaultTemperament);
      ComboTemperamentReactivity.ItemsSource = new List<string>(DefaultTemperament);
      if (!string.IsNullOrEmpty(state.TemperamentActivity))
        ComboTemperamentActivity.SelectedItem = state.TemperamentActivity;
      else
        ComboTemperamentActivity.SelectedIndex = 1; // Средняя
      if (!string.IsNullOrEmpty(state.TemperamentReactivity))
        ComboTemperamentReactivity.SelectedItem = state.TemperamentReactivity;
      else
        ComboTemperamentReactivity.SelectedIndex = 1;

      _stressBehaviorIds = state.StressBehaviorIds?.ToList() ?? new List<int>();
      _threatResponseIds = state.ThreatResponseIds?.ToList() ?? new List<int>();
      _rewardResponseIds = state.RewardResponseIds?.ToList() ?? new List<int>();
      _punishmentResponseIds = state.PunishmentResponseIds?.ToList() ?? new List<int>();

      UpdateActionsDisplay();

      LoadCombo(ComboSociality, state.Sociality, state.SocialityValues, DefaultSociality);
      LoadCombo(ComboSpecialTriggers, state.SpecialTriggers, state.SpecialTriggersValues, DefaultSpecialTriggersTaboos);
      LoadCombo(ComboSpecialTaboos, state.SpecialTaboos, state.SpecialTaboosValues, DefaultSpecialTriggersTaboos);
      TextBoxAdditionalWishes.Text = state.AdditionalWishes ?? string.Empty;
      TextBoxPromptSuffix.Text = state.PromptSuffix ?? string.Empty;
    }

    private static void LoadCombo(ComboBox combo, string selected, IReadOnlyList<string> values, string[] defaults)
    {
      var list = (values != null && values.Count > 0) ? values.ToList() : new List<string>(defaults);
      combo.ItemsSource = list;
      if (!string.IsNullOrEmpty(selected))
      {
        if (list.Contains(selected))
          combo.SelectedItem = selected;
        else
        {
          list.Add(selected);
          combo.ItemsSource = list;
          combo.SelectedItem = selected;
        }
      }
    }

    private void ComboEditable_LostFocus(object sender, RoutedEventArgs e)
    {
      if (!(sender is ComboBox combo) || !combo.IsEditable) return;
      var text = (combo.Text ?? string.Empty).Trim();
      if (string.IsNullOrEmpty(text)) return;
      var items = combo.ItemsSource as IList<string>;
      if (items == null) return;
      if (items.Contains(text)) return;
      var result = MessageBox.Show(
        $"Значение \"{text}\" отсутствует в списке. Добавить его в список?",
        "Добавить значение",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);
      if (result != MessageBoxResult.Yes) return;
      var list = items as List<string>;
      if (list == null)
      {
        list = new List<string>(items);
        combo.ItemsSource = list;
      }
      list.Add(text);
      combo.SelectedItem = text;
    }

    private void UpdateActionsDisplay()
    {
      TextStressBehavior.Text = GetActionNames(_stressBehaviorIds);
      TextThreatResponse.Text = GetActionNames(_threatResponseIds);
      TextRewardResponse.Text = GetActionNames(_rewardResponseIds);
      TextPunishmentResponse.Text = GetActionNames(_punishmentResponseIds);
    }

    private static string GetActionNames(List<int> ids)
    {
      if (ids == null || ids.Count == 0) return "";
      if (!AdaptiveActionsSystem.IsInitialized) return string.Join(", ", ids);
      var all = AdaptiveActionsSystem.Instance.GetAllAdaptiveActions();
      var names = ids.Select(id => all.FirstOrDefault(a => a.Id == id)?.Name ?? id.ToString()).ToList();
      return string.Join(", ", names);
    }

    private void OpenActionsSelectionDialog(List<int> currentIds, Action<List<int>> onOk)
    {
      var dialog = new AdaptiveActionsSelectionDialog(currentIds);
      dialog.Owner = this;
      if (dialog.ShowDialog() == true && dialog.SelectedAdaptiveActions != null)
        onOk(dialog.SelectedAdaptiveActions);
    }

    private void ButtonSelectStressBehavior_Click(object sender, RoutedEventArgs e)
    {
      OpenActionsSelectionDialog(_stressBehaviorIds, list => { _stressBehaviorIds = list; UpdateActionsDisplay(); });
    }

    private void ButtonSelectThreatResponse_Click(object sender, RoutedEventArgs e)
    {
      OpenActionsSelectionDialog(_threatResponseIds, list => { _threatResponseIds = list; UpdateActionsDisplay(); });
    }

    private void ButtonSelectRewardResponse_Click(object sender, RoutedEventArgs e)
    {
      OpenActionsSelectionDialog(_rewardResponseIds, list => { _rewardResponseIds = list; UpdateActionsDisplay(); });
    }

    private void ButtonSelectPunishmentResponse_Click(object sender, RoutedEventArgs e)
    {
      OpenActionsSelectionDialog(_punishmentResponseIds, list => { _punishmentResponseIds = list; UpdateActionsDisplay(); });
    }

    private List<string> GetComboValues(ComboBox combo)
    {
      return (combo.ItemsSource as IEnumerable<string>)?.ToList() ?? new List<string>();
    }

    private int GetStage()
    {
      if (ComboStage.SelectedValue is int stage) return stage;
      if (ComboStage.SelectedItem is EvolutionStageItem item) return item.StageNumber;
      return 0;
    }

    private void ButtonSave_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var name = (TextBoxAgentName.Text ?? string.Empty).Trim();
        var description = TextBoxDescription?.Text ?? string.Empty;
        var stage = GetStage();
        var baseArchetype = (ComboBaseArchetype.Text ?? string.Empty).Trim();
        var keyMotivation = (ComboKeyMotivation.Text ?? string.Empty).Trim();
        var temperamentActivity = ComboTemperamentActivity.SelectedItem as string ?? string.Empty;
        var temperamentReactivity = ComboTemperamentReactivity.SelectedItem as string ?? string.Empty;
        var sociality = (ComboSociality.Text ?? string.Empty).Trim();
        var specialTriggers = (ComboSpecialTriggers.Text ?? string.Empty).Trim();
        var specialTaboos = (ComboSpecialTaboos.Text ?? string.Empty).Trim();
        var additionalWishes = TextBoxAdditionalWishes?.Text ?? string.Empty;
        var promptSuffix = TextBoxPromptSuffix?.Text ?? string.Empty;

        _gomeostas.SetExtendedAgentProperties(
          name, description, stage,
          baseArchetype, GetComboValues(ComboBaseArchetype),
          keyMotivation, GetComboValues(ComboKeyMotivation),
          temperamentActivity, temperamentReactivity,
          _stressBehaviorIds, sociality, GetComboValues(ComboSociality),
          _threatResponseIds, _rewardResponseIds, _punishmentResponseIds,
          specialTriggers, GetComboValues(ComboSpecialTriggers),
          specialTaboos, GetComboValues(ComboSpecialTaboos),
          additionalWishes, promptSuffix);

        var (success, error) = _gomeostas.SaveAgentProperties();
        if (success)
          MessageBox.Show("Свойства агента сохранены.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
        else
          MessageBox.Show($"Ошибка сохранения: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ButtonCreatePrompt_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var baseArchetype = (ComboBaseArchetype.Text ?? string.Empty).Trim();
        var keyMotivation = (ComboKeyMotivation.Text ?? string.Empty).Trim();
        var temperamentActivity = ComboTemperamentActivity.SelectedItem as string ?? string.Empty;
        var temperamentReactivity = ComboTemperamentReactivity.SelectedItem as string ?? string.Empty;
        var stressBehavior = GetActionNames(_stressBehaviorIds);
        var sociality = (ComboSociality.Text ?? string.Empty).Trim();
        var threatResponse = GetActionNames(_threatResponseIds);
        var rewardResponse = GetActionNames(_rewardResponseIds);
        var punishmentResponse = GetActionNames(_punishmentResponseIds);
        var specialTriggers = (ComboSpecialTriggers.Text ?? string.Empty).Trim();
        var specialTaboos = (ComboSpecialTaboos.Text ?? string.Empty).Trim();
        var additionalWishes = TextBoxAdditionalWishes?.Text ?? string.Empty;
        var promptSuffixTemplate = TextBoxPromptSuffix?.Text ?? string.Empty;

        var content = $@"БАЗОВЫЕ ПАРАМЕТРЫ:
Базовый архетип (Базовый психологический архетип, определяющий фундаментальные паттерны поведения): [{baseArchetype}]
Ключевая мотивация (Главный движущий мотив агента, определяет приоритеты в принятии решений): [{keyMotivation}]

ТЕМПЕРАМЕНТ:
Активность (Уровень общей активности: Низкая - флегматичность, экономия энергии; Средняя - сбалансированность; Высокая - гиперактивность, постоянное движение): [{temperamentActivity}]
Реактивность (Скорость и интенсивность реакции на внешние стимулы: Низкая - замедленные реакции; Средняя - адекватные; Высокая - мгновенные, импульсивные): [{temperamentReactivity}]

ПОВЕДЕНЧЕСКИЕ ХАРАКТЕРИСТИКИ:
Поведение в стрессе (Набор возможных реакций на стрессовые ситуации. Может быть выбрано несколько вариантов): [{stressBehavior}]
Социальность (Стиль социального взаимодействия: Одиночка - избегает контактов; Избирательный - выбирает узкий круг; Стайный - комфортно в группе; Зависимый - нуждается в постоянном общении): [{sociality}]
Реакция на угрозу (Первичная, инстинктивная реакция при обнаружении угрозы): [{threatResponse}]
Реакция на поощрение (Типичная реакция на получение поощрения, ресурса или положительной обратной связи): [{rewardResponse}]
Реакция на наказание (Типичная реакция на наказание, порицание или лишение ресурса): [{punishmentResponse}]

ОСОБЕННОСТИ:
Особые триггеры (Факторы, которые могут вызвать нестабильность или неадекватную реакцию. Важно для ИИ при моделировании поведения): [{specialTriggers}]
Особые табу (Действия или ситуации, которых агент избегает даже в хорошем состоянии. Критически важно для избегания неконсистентного поведения): [{specialTaboos}]

ДОПОЛНИТЕЛЬНЫЕ ПОЖЕЛАНИЯ (Дополнительные замечания, особенности или пожелания по поведению агента, которые нужно учесть при генерации):
[{additionalWishes}]
";

        if (!string.IsNullOrWhiteSpace(promptSuffixTemplate))
        {
          var suffix = ReplacePromptSuffixPlaceholders(promptSuffixTemplate);
          content = content.TrimEnd() + "\r\n\r\n" + suffix;
        }

        var bootDataPath = System.IO.Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "ISIDA", "BootData");
        if (!System.IO.Directory.Exists(bootDataPath))
          System.IO.Directory.CreateDirectory(bootDataPath);
        var filePath = System.IO.Path.Combine(bootDataPath, "prompt_genetic_reflex_generate.txt");
        System.IO.File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
        MessageBox.Show($"Файл промпта сохранён:\n{filePath}", "Промпт для ИИ", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка создания промпта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ButtonClose_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    /// <summary>
    /// Подставляет в шаблон текста вставки промпта плейсхолдеры:
    /// [stileCombination] — комбинации стилей реагирования;
    /// [AdaptiveActionList] — список адаптивных действий;
    /// [InfluenceActionList] — список воздействий с пульта.
    /// </summary>
    private string ReplacePromptSuffixPlaceholders(string template)
    {
      if (string.IsNullOrEmpty(template)) return string.Empty;

      var text = template;

      // [stileCombination] — тот же перечень комбинаций, что в StyleCombinations.comb (используется для подстановок). Загружаем из файла — один источник данных.
      var styleCombinationStrings = new List<string>();
      try
      {
        var combinations = _gomeostas.LoadStyleCombinations();
        foreach (var combo in combinations)
        {
          var names = combo
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => s.Name.Trim())
            .ToList();
          if (names.Count > 0)
            styleCombinationStrings.Add(string.Join("+", names));
        }
      }
      catch { /* игнорируем ошибки загрузки */ }
      text = text.Replace("[stileCombination]", string.Join(", ", styleCombinationStrings));

      // [AdaptiveActionList] — названия адаптивных действий через запятую
      var adaptiveNames = new List<string>();
      if (AdaptiveActionsSystem.IsInitialized)
      {
        var actions = AdaptiveActionsSystem.Instance.GetAllAdaptiveActions();
        adaptiveNames = actions.OrderBy(x => x.Id).Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
      }
      text = text.Replace("[AdaptiveActionList]", string.Join(", ", adaptiveNames));

      // [InfluenceActionList] — названия воздействий с пульта через запятую
      var influenceNames = new List<string>();
      if (InfluenceActionSystem.IsInitialized)
      {
        var influences = InfluenceActionSystem.Instance.GetAllInfluenceActions();
        influenceNames = influences.OrderBy(x => x.Id).Select(i => i.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
      }
      text = text.Replace("[InfluenceActionList]", string.Join(", ", influenceNames));

      return text;
    }
  }
}
