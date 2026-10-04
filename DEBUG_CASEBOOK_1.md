# DEBUG_CASEBOOK_1.md — Протокол отладки AIStudio

Журнал разобранных ошибок проекта AIStudio: симптом → проверенные гипотезы → корень → исправление → эвристики.

## Как пользоваться

**Прежде чем искать причину бага — прочитать раздел «Эвристики».** Если симптом похож на описанный — начинать с уже известного корня.

## Случай 1. Сценарный прогон: б/у рефлекс не запускается при наличии воздействия среды в той же строке

- **Дата:** 2026-10-04
- **Симптом:** при прогоне сценария в AIStudio, если в строке сценария вместе с триггерным действием (например, «Запуск экспорта pdf» — действие 26) указано воздействие среды (EnvProbeSpecs=60:+1), б/у безусловный рефлекс 5 не срабатывает. Без воздействия среды в той же строке рефлекс запускается нормально.
- **Область:** `ViewModels/AgentPultViewModel.cs` (`TryApplyScenarioStimulus`), `Common/Environment/VirtualProbePressureApplier.cs` (`ApplyExplicit`), `isida/Actions/InfluenceActionSystem.cs` (`ApplyMultipleInfluenceActions`, `TriggerStimulusActivated`), `isida/Reflexes/ReflexesActivator.cs` (`ActiveFromAction`, `IsReflexConditionsMet`, `UpdateCurrentStates`).
- **Гипотезы и проверки:**
  1. *Среда меняет гомеостаз → матчинг рефлекса ломается* — гипотеза, но синхронный `TriggerStimulusActivated?.Invoke` внутри `ApplyMultipleInfluenceActions` шёл **до** `ApplyExplicit` в старом порядке, поэтому probe в той же строке не мог повлиять на матчинг текущего стимула.
  2. *Разные коды цвета в логе* — в логе колонка «Триггер» идёт из `_activeCurReflexTriggerStimulusID` (без цвета, всегда White=0), тогда как в строке сценария указан `VisualColorId=3` (чертёж). Но б/у рефлекс матчится **не по цвету**, а по дереву: Level1 (база гомеостаза), Level2 (стили), Level3/EA (действие). Цвет для б/у роли не играет.
  3. *Probe — отдельный гомеостатический канал* — `ApplyExplicit` делает прямую запись в гомеостаз (`VirtualBadMetric=0`), не создаёт PerceptionImage и не вызывает `ApplyMultipleInfluenceActions`. Отдельный канал, не стимул-образ.
  4. *Синхронный Invoke шёл до ApplyExplicit* — **подтверждено**: `TriggerStimulusActivated?.Invoke` (стр. 497 `isida/Actions/InfluenceActionSystem.cs`) срабатывал внутри `ApplyMultipleInfluenceActions`, который шел **перед** `ApplyExplicit`. Матчинг шёл по состоянию до probe.
  5. *Но probe всё равно влияет косвенно* — хотя матчинг шёл до probe, probe применялся **раньше** следующего тика. При прогоне нескольких строк probe от предыдущей строки мог изменить гомеостаз к моменту матчинга следующей. В случае с одной строкой probe не влияет, но порядок важен при множественных строках и при эмуляции поведения Velum.
- **Корень:** порядок применения probe и операторного стимула в `TryApplyScenarioStimulus` не соответствовал реальной фазовой последовательности Velum: probe должен применяться **до** матчинга рефлексов, чтобы гомеостаз обновлялся к моменту срабатывания.
- **Доказательство:** `TryApplyScenarioStimulus` (AgentPultViewModel.cs) до правки: probe (`ApplyExplicit`) → операторный стимул (`ApplyMultipleInfluenceActions`). После правки: probe → операторный стимул (probe перенесён ПЕРЕД операторным стимулом).
- **Исправление:**
  - `TryApplyScenarioStimulus` в `ViewModels/AgentPultViewModel.cs`: блок `if (hasProbeAction) ApplyExplicit(...)` перенесён ПЕРЕД блоком `if (hasOperatorStimulus) ApplyMultipleInfluenceActions(...)`.
  - Бэкап: `ViewModels/AgentPultViewModel.cs.bak`.
- **Проверка:** прогон Scenario_2 (StimulusID=26, VisualColorId=3, EnvProbeSpecs=60:+1) → в логе `AgentLogs.jsonl` на StimulusID=26 присутствует `Б/У рефлекс: 5`. Прогон Scenario_1 (без среды) → рефлекс 5 также срабатывает. Симптом устранён.
- **Эвристики:** → E1

## Эвристики

- **E1. Probe (воздействие среды) и операторный стимул — разные фазы, не один стимул-образ.** `VirtualProbePressureApplier.ApplyExplicit` делает прямую запись в гомеостаз (`VirtualBadMetric`), не создаёт PerceptionImage и не вызывает `ApplyMultipleInfluenceActions`. Б/у рефлекс матчится не по цвету (лог может показывать код 0 при заданном 3), а по дереву: Level1 → Level2 → Level3/EA. При прогоне сценариев probe должен применяться **до** операторного стимула, чтобы гомеостаз обновлялся к моменту матчинга рефлексов. См. случай 1.
