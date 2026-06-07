# План рефакторинга: разделение стимулов и давления среды

**Версия:** 1.0  
**Дата:** 2026-06-07  
**Статус:** проект  
**Область:** Velum (`D:\VELUM\Velum`), AIStudio (`D:\ISIDA\Programms\app\AIStudio`), ISIDA (`D:\ISIDA\Programms\isida`), данные проектов (`%ProgramData%\VELUM`, `%ProgramData%\ISIDA\Adapters\`), пакеты адаптеров.

---

## 1. Проблема

Сейчас один справочник `InfluenceActions.dat` совмещает **три разные сущности**:

| Сущность | Пример ID | Как применяется | Роль |
|----------|-----------|-----------------|------|
| Операторский стимул | 1–12 | `ApplyMultipleInfluenceActions` | рефлексы, пульт |
| Давление метрики SW | 13–21 | пульсовой composer → запись виталов | только гомеостаз |
| Прокси-триггер SW | 101+ | `ApplyMultipleInfluenceActions` (из YAML) | рефлексы, события SW |

Разделение опирается только на поле `EnvironmentMetricProbeKey`, которое:

- не отличает оператора (1–12) от прокси-триггера (101);
- помечает метрики как «среда» на пульте AIStudio, хотя при ручной отправке они идут по **другому** контуру, чем при опросе метрик;
- создаёт ложное впечатление, что «среда» на пульте = метрики, тогда как Save (101) — тоже «среда», но без probe key.

**Цель рефакторинга:** две независимые модели данных и два runtime-контура без смешения смыслов.

---

## 2. Целевая архитектура

### 2.1. Два справочника

```
┌─────────────────────────────────────┐     ┌──────────────────────────────────┐
│  InfluenceActions.dat               │     │  EnvironmentPressureRules.dat     │
│  (стимулы / дискретные воздействия) │     │  (давление метрик на виталы)      │
├─────────────────────────────────────┤     ├──────────────────────────────────┤
│  ApplyMultipleInfluenceActions      │     │  VelumSolidEnvironmentInfluence   │
│  → PerceptionImage                  │     │  Composer → HostBatchUpdate       │
│  → TriggerStimulusActivated         │     │  (только на пульсе)               │
│  → GeneticReflex Level3             │     │  без рефлексов                    │
└─────────────────────────────────────┘     └──────────────────────────────────┘
         ▲                                              ▲
         │                                              │
   Оператор / YAML-триггеры                    Опрос probe_key в SW
   (EnvironmentTriggers.yaml)                  (metric-probes.json)
```

**InfluenceActions.dat** (имя файла сохраняем для совместимости с ISIDA) содержит **только стимулы**.  
**EnvironmentPressureRules.dat** — новый файл; строки 13–21 из текущего VELUM переезжают сюда.

### 2.2. Третий слой — привязка событий (без изменения сути)

`EnvironmentTriggers.yaml` по-прежнему связывает событие SW → `influence_action_id` (стимул).  
Это **не** справочник стимулов, а каталог детекторов.

### 2.3. Пульт: две колонки стимулов (не метрик)

На пульте AIStudio и Velum Task Pane — **две колонки только из `InfluenceActions.dat`**, разбивка по наличию ссылки в `EnvironmentTriggers.yaml`:

| Колонка | Содержимое | Смысл для оператора |
|---------|------------|---------------------|
| **Прямое воздействие** | стимулы, **не** указанные ни в одном триггере YAML | только оператор шлёт вручную |
| **Через среду** | стимулы, на которые есть запись в `EnvironmentTriggers.yaml` | оператор может послать вручную **и** среда может имитировать то же (Save, команда SW и т.д.) |

Обе колонки при отправке вызывают один API: `ApplyMultipleInfluenceActions`.  
**EnvironmentPressureRules** на пульте **не показываются** — это фоновое давление, не кнопки.

Классификация колонок — **вычисляемая** при загрузке:

```csharp
var yamlBoundStimulusIds = SwTriggerCatalog.GetAllReferencedInfluenceActionIds();
directOnly   = stimuli.Where(s => !yamlBoundStimulusIds.Contains(s.Id));
viaEnvironment = stimuli.Where(s =>  yamlBoundStimulusIds.Contains(s.Id));
```

Явное поле `StimulusKind` в `.dat` **не обязательно** (привязка к YAML — источник истины для UI). Опционально — для валидации и подсказок в Studio.

### 2.4. Правила использования

| Вопрос | Ответ |
|--------|-------|
| Можно ли указать pressure rule в Level3 рефлекса? | **Нет** (валидация в Studio) |
| Сработает ли рефлекс при изменении метрики? | **Нет** — только через явный стимул |
| Может ли оператор вручную послать стимул из колонки «Через среду»? | **Да** |
| Может ли Velum послать стимул без YAML? | **Нет** — только ID из триггеров |
| Антагонисты между pressure rules и стимулами? | **Нет** — только внутри одного справочника |

---

## 3. Форматы данных

### 3.1. `InfluenceActions.dat` (после рефакторинга)

```
# Формат: ID|Имя|Описание|Воздействие|Антагонисты
# Воздействие: paramId1:effect1;paramId2:effect2  (пусто — чистый пусковой стимул)
# Антагонисты: id1,id2,id3
# Только дискретные стимулы (оператор и/или YAML-триггеры). Метрики среды — в EnvironmentPressureRules.dat
```

**Удаляется столбец:** `EnvironmentMetricProbeKey`.

**Рекомендуемые диапазоны ID** (валидация в Studio, не в ISIDA):

| Диапазон | Назначение |
|----------|------------|
| 1–99 | операторские стимулы (обычно не в YAML) |
| 101–199 | прокси для YAML-триггеров |
| 200+ | резерв |

### 3.2. `EnvironmentPressureRules.dat` (новый)

Путь по умолчанию: `{DataActionsFolderPath}\EnvironmentPressureRules.dat`

```
# Формат: RuleId|ProbeKey|Имя|Описание|Influences|Antagonists
# ProbeKey: ключ из schema/metric-probes.json пакета адаптера
# Influences: paramId1:effect1;paramId2:effect2  (шаблон при ухудшении метрики)
# Antagonists: id1,id2  (только между правилами давления)
13|Velum.Solid.Material|Материал в модели|...|1:-1;2:1;...|2,10
```

### 3.3. `EnvironmentTriggers.yaml` (уточнение контракта)

Поле `influence_action_id` остаётся; в документации явно: **ссылка на строку `InfluenceActions.dat`, не на pressure rule**.

Опционально (фаза 2): алиас `stimulus_action_id` с тем же смыслом для читаемости.

### 3.4. Настройки проекта

Добавить в `Settings.xml` / `VelumAppConfig` / `SettingsValidator`:

```xml
<EnvironmentPressureRulesFilePath>{ProjectRoot}\Data\Actions\EnvironmentPressureRules.dat</EnvironmentPressureRulesFilePath>
```

---

## 4. Фазы внедрения

| Фаза | Содержание | Критерий готовности |
|------|------------|---------------------|
| **0** | Документация, ADR, миграционный скрипт данных | этот документ + обновлённые AdapterContract |
| **1** | Новый файл pressure rules; Velum composer читает его; fallback на старый `.dat` | VELUM: метрики работают из нового файла |
| **2** | AIStudio: два редактора; пульт — две колонки по YAML | оператор видит разделение стимулов |
| **3** | Убрать probe key из InfluenceActions; миграция ProgramData | один столбец меньше в `.dat` |
| **4** | ISIDA: валидация «pressure ID не в ApplyMultiple» (опционально) | защита от регрессий |
| **5** | Удалить fallback; обновить пакет `sldworks_19` | чистая схема |

---

## 5. Velum — затрагиваемые файлы и изменения

### 5.1. Новые модули

| Файл | Назначение |
|------|------------|
| `SolidHomeostasis\EnvironmentPressureRuleSystem.cs` | загрузка/сохранение `EnvironmentPressureRules.dat` |
| `SolidHomeostasis\EnvironmentPressureRule.cs` | модель строки правила |
| `SolidHomeostasis\EnvironmentPressureRuleValidator.cs` | probe key, antagonists, ID |
| `Configuration\VelumAppConfig.cs` | `EnvironmentPressureRulesFilePath` |

### 5.2. Изменяемые файлы

| Файл | Изменение |
|------|-----------|
| `SolidHomeostasis\VelumSolidEnvironmentInfluenceComposer.cs` | источник — `EnvironmentPressureRuleSystem`, не `InfluenceActionSystem` + probe key |
| `SolidHomeostasis\VelumSolidEnvironmentBridge.cs` | `EnumerateDistinctEnvironmentProbeKeys` из pressure rules |
| `SolidHomeostasis\VelumSolidWorksHomeostasisMetrics.cs` | без изменения семплеров; ключи сверяются с pressure rules |
| `Common\VelumAgentTaskPane.cs` | две колонки стимулов: `GetDirectStimuli()` / `GetEnvironmentBoundStimuli()`; tooltip pressure rules в деталях параметра — из нового справочника |
| `Common\VelumOperatorInfluencesPickerForm.cs` | две вкладки/списка или две секции по YAML-привязке |
| `Isida\VelumSolidInfluenceTriggerApplier.cs` | проверка: ID ∈ stimuli и (опционально) ∈ YAML для event path |
| `ReactiveCore\SwTriggerCatalog.cs` | метод `GetAllReferencedInfluenceActionIds()` |
| `ReactiveCore\SwEventDetector.cs` | без смены логики; логировать «stimulus via environment» |
| `ReactiveCore\VelumReactiveCoreBootstrap.cs` | загрузка pressure rules; предупреждение если файл пуст |
| `ReactiveCore\EnvironmentContractMapper.cs` | комментарии / `recommended_trigger_influence_ids` → только stimulus IDs |
| `Configuration\VelumAppConfig.cs` | путь к pressure rules; seed в Settings.xml |
| `Isida\VelumIsidaHost.cs` | инициализация pressure rules после ISIDA |
| `velum.csproj` | новые `.cs` |

### 5.3. Данные VELUM (`C:\ProgramData\VELUM`)

| Файл | Действие |
|------|----------|
| `Data\Actions\InfluenceActions.dat` | удалить строки 13–21; убрать 6-й столбец |
| `Data\Actions\EnvironmentPressureRules.dat` | **создать** из бывших 13–21 |
| `BootData\Environment\EnvironmentTriggers.yaml` | без изменений (`influence_action_id: 101`) |
| `Settings\Settings.xml` | добавить `EnvironmentPressureRulesFilePath` |

### 5.4. Документация Velum

| Файл | Действие |
|------|----------|
| `docs\Velum_RecipeReflexEditor_ImplementationPlan.md` | исправить §3.4: InfluenceActions ≠ только прокси; добавить ссылку на этот план |

---

## 6. AIStudio — затрагиваемые файлы и изменения

### 6.1. Пульт (две колонки стимулов)

| Файл | Изменение |
|------|-----------|
| `ViewModels\AgentPultViewModel.cs` | заменить split по `EnvironmentMetricProbeKey` на split по YAML: `DirectStimulusActions` / `EnvironmentBoundStimulusActions`; оба набора → `ApplyMultipleInfluenceActions` |
| `Pages\AgentPultView.xaml` | заголовки: «Прямое воздействие» / «Через среду (и вручную)»; убрать смысл «метрики среды» |
| `Pages\AgentPultView.xaml.cs` | при необходимости — подсказки по триггерам для колонки 2 |

**Логика загрузки колонок:**

1. Загрузить все строки `InfluenceActionSystem`.
2. Загрузить `EnvironmentTriggers.yaml` проекта → множество `influence_action_id`.
3. Колонка 1: ID ∉ множества. Колонка 2: ID ∈ множества.
4. Pressure rules не участвуют.

### 6.2. Редакторы

| Файл | Изменение |
|------|-----------|
| `Pages\ExterInalInfluencesView.xaml` | убрать колонку «Ключ метрики среды»; заголовок «Стимулы (воздействия на агента)» |
| `ViewModels\ExterInalInfluencesViewModel.cs` | убрать `MetricProbeKeyOptions`; валидация ID-диапазонов |
| **Новый** `Pages\EnvironmentPressureRulesView.xaml` (+ `.cs`) | таблица pressure rules |
| **Новый** `ViewModels\Environment\EnvironmentPressureRulesViewModel.cs` | CRUD, combobox probe key из `metric-probes.json` |
| `ViewModels\MainViewModel.cs` | пункт меню «Давление среды на виталы» |
| `ViewModels\Reflexes\GeneticReflexesViewModel.cs` | Level3 combobox — только stimulus IDs (не pressure rule IDs) |
| `Pages\Reflexes\InfluenceActionsSelectionDialog.xaml(.cs)` | фильтр: только стимулы |
| `ViewModels\Environment\EnvironmentTriggersViewModel.cs` | combobox `influence_action_id` — только стимулы; подсказка «появится в колонке „Через среду“ на пульте» |
| `ViewModels\Environment\EnvironmentRecipeEditorViewModel.cs` | `recommended_trigger_influence_ids` — только стимулы |
| `Common\ProjectBootstrap.cs` | seed `EnvironmentPressureRules.dat`; обновить `MinimalInfluenceActionsContent` (без probe key) |
| `Common\Adapters\AdapterSchemaLoader.cs` | probe keys только для pressure editor |
| `Common\Adapters\AdapterSchemaModels.cs` | переименовать/уточнить `OperatorOnly` → не использовать для стимулов |
| `AIStudio.csproj` | новые Page/Compile |

### 6.3. Прочие ссылки на InfluenceActions

| Файл | Изменение |
|------|-----------|
| `ViewModels\Automatizm\AutomatizmsViewModel.cs` | фильтры — только стимулы |
| `ViewModels\Understanding\SituationTypesViewModel.cs` | списки воздействий — стимулы |
| `ViewModels\Research\ScenarioEditorViewModel.cs` | сценарии — стимулы |
| `Converters\*.cs` (Perception, Episodic, IdListToNames) | без смены API; данные — только стимулы |
| `Common\AgentLogCellTooltipProvider.cs` | различать stimulus vs pressure в подсказках (фаза 2) |
| `tools\generate_pult_user_manual_docx.py` | две колонки стимулов |

### 6.4. Demo-данные Studio

| Файл | Действие |
|------|----------|
| `docs\Data\Actions\InfluenceActions.dat` | убрать probe key; только стимулы |
| `docs\Data\Actions\EnvironmentPressureRules.dat` | **создать** образец |

---

## 7. ISIDA — затрагиваемые файлы и изменения

### 7.1. Минимальный вариант (фаза 1–3)

ISIDA **не обязательно** менять: Velum и Studio сами разделяют файлы; `InfluenceActionSystem` продолжает грузить только стимулы.

| Файл | Изменение |
|------|-----------|
| `Actions\InfluenceActionSystem.cs` | **фаза 3:** чтение/запись 5 столбцов (без probe key); свойство `EnvironmentMetricProbeKey` — `[Obsolete]` или удалить |
| `Common\FileValidator.cs` | обновить заголовок формата InfluenceActions |
| `Common\SettingsValidator.cs` | `ValidateEnvironmentMetricProbeKey` → перенести в Studio/Velum для pressure rules или оставить для миграции |

### 7.2. Расширенный вариант (фаза 4)

| Файл | Изменение |
|------|-----------|
| `Actions\InfluenceActionSystem.cs` | `ApplyMultipleInfluenceActions`: опциональный guard «ID не из pressure rules registry» (инъекция через host callback) |
| `docs\План внедрения триады симбионт-среда-оператор.md` | § EnvironmentMetricProbeKey → EnvironmentPressureRules |
| `docs\Архитектура движка isida.txt` | раздел InfluenceActions: только стимулы |

### 7.3. SymbiontEnv.Contract (если используется из ISIDA)

| Файл | Изменение |
|------|-----------|
| `SymbiontEnv.Contract` (NuGet/проект) | документировать: `influence_action_id` в triggers = stimulus only |

---

## 8. Пакеты адаптеров и документация

### 8.1. AIStudio `docs\`

| Файл | Действие |
|------|----------|
| **`StimulusAndEnvironmentPressure_RefactoringPlan.md`** | этот документ |
| `AdapterContract.md` | § InfluenceActions → только стимулы; новый § EnvironmentPressureRules; §7 triggers: influence_action_id = stimulus; пульт: две колонки по YAML |
| `AdapterContract.html` | синхронизировать с `.md` |
| `AdapterAuthorGuide.md` | глава «Два справочника»; как автору не смешивать probe и stimulus |
| `AdapterAuthorGuide.html` | синхронизировать |

### 8.2. `docs\AdapterPackageTemplates\demo\`

| Файл | Действие |
|------|----------|
| `AdapterContract.md` | как основной AdapterContract |
| `README.md` | упомянуть EnvironmentPressureRules.dat в BootData/Data |
| `manifest.json` | опционально: `"supportsEnvironmentPressureRules": true` |
| `schema\README.txt` | metric-probes.json → только для pressure rules, не для InfluenceActions |
| `schema\metric-probes.json` | без изменения ключей; комментарий в README |
| `schema\trigger-detect.json` | без изменения |
| `schema\trigger-filter.json` | без изменения |
| `schema\recipe-steps.json` | без изменения |
| `schema\recipe-preconditions.json` | без изменения |
| `BootData\Environment\README.txt` | triggers → stimulus IDs; pressure rules — отдельный файл в Data/Actions |
| `BootData\Environment\EnvironmentTriggers.yaml` | пример с `influence_action_id: 101` |
| `BootData\Environment\EnvironmentRecipes.yaml` | `recommended_trigger_influence_ids: [101]` |
| `runtime\README.txt` | host читает pressure rules из Data проекта |

**Новый образец:**

| Файл | Содержание |
|------|------------|
| `BootData\Actions\EnvironmentPressureRules.dat` | 1–2 demo-правила (или в шаблоне seed Studio) |

### 8.3. Установленный пакет `%ProgramData%\ISIDA\Adapters\sldworks_19\`

| Файл | Действие |
|------|----------|
| `AdapterContract.md` | синхронизировать с AIStudio |
| `README.md` | два справочника |
| `schema\README.txt` | probe → pressure only |
| `schema\metric-probes.json` | без изменения (9 ключей Velum) |
| `BootData\Environment\README.txt` | обновить |
| `BootData\Environment\EnvironmentTriggers.yaml` | образец save → 101 |
| `BootData\Environment\EnvironmentRecipes.yaml` | без изменения |

---

## 9. Миграция данных (скрипт / Studio wizard)

### 9.1. Алгоритм для существующего VELUM

```
1. Прочитать InfluenceActions.dat (6 столбцов).
2. Строки с непустым EnvironmentMetricProbeKey → EnvironmentPressureRules.dat
   (RuleId=старый ID, ProbeKey=столбец 6, Name, Description, Influences, Antagonists).
3. Остальные строки → InfluenceActions.dat (5 столбцов).
4. Проверить GeneticReflexes Level3 — все ID ∈ stimuli.
5. Проверить EnvironmentTriggers.yaml — все influence_action_id ∈ stimuli.
6. Удалить cross-antagonists между бывшими 1–12 и 13–21 или перенести внутри pressure rules.
```

### 9.2. Обратная совместимость (фаза 1)

Velum composer:

```csharp
if (EnvironmentPressureRuleSystem.HasRules)
  use pressure rules;
else
  fallback: InfluenceActions with non-empty EnvironmentMetricProbeKey  // deprecated
```

Логировать warning при fallback.

---

## 10. UI/UX — пульт (итоговый макет)

```
┌──────────────────────┬─────────────────────────────┐
│ Прямое воздействие   │ Через среду                 │
│ (только оператор)    │ (оператор + события SW)     │
├──────────────────────┼─────────────────────────────┤
│ ☐ Наказать           │ ☐ SW: Save документа        │
│ ☐ Поощрить           │   ↳ триггер: save_active…   │
│ ☐ Напугать           │                             │
│ ...                  │                             │
└──────────────────────┴─────────────────────────────┘
```

Подсказка для колонки «Через среду»: «Эти стимулы также отправляет среда при событиях из EnvironmentTriggers.yaml (например, Save в SolidWorks)».

**Velum Task Pane** — аналогичное разделение (две секции чекбоксов или picker с вкладками).

---

## 11. Тест-план

| # | Сценарий | Ожидание |
|---|----------|----------|
| 1 | Save в SW, пульсация вкл | стимул 101, рефлекс Level3=101, рецепт G_AD=37 |
| 2 | Оператор: «Наказать» (не в YAML) | рефлекс по Level3=1, колонка «Прямое» |
| 3 | Оператор: вручную «SW: Save» (101) | тот же рефлекс, что и п.1 |
| 4 | Изменение метрики материала | сдвиг виталов, **без** TriggerStimulusActivated |
| 5 | Level3=13 (pressure rule ID) после миграции | Studio: ошибка валидации |
| 6 | AIStudio: выбор pressure rule на пульте | невозможен (нет в UI) |
| 7 | Пустой EnvironmentPressureRules.dat | fallback или предупреждение; метрики не ломают стимулы |
| 8 | Антагонист 3 vs 14 (старый) | после миграции удалён или только внутри одного справочника |

---

## 12. Риски и mitigations

| Риск | Mitigation |
|------|------------|
| Сломанные проекты со старым 6-столбцовым `.dat` | fallback + миграционный wizard в Studio |
| Дублирование ID pressure rule и stimulus | разные файлы; валидация уникальности в Studio |
| GeneticReflexes ссылаются на 13–21 | миграция Level3 → новые stimulus-прокси или смена рефлексов |
| SymbiontEnv.Contract / YAML сторонние адаптеры | версия contract v2; алиас `stimulus_action_id` |
| Velum hardcoded probe keys | без изменений; keys в pressure rules должны совпадать |

---

## 13. Чеклист задач (сводный)

### Velum
- [ ] `EnvironmentPressureRuleSystem` + путь в config
- [ ] Composer → pressure rules
- [ ] Task Pane: две колонки по YAML
- [ ] `SwTriggerCatalog.GetAllReferencedInfluenceActionIds()`
- [ ] Миграция ProgramData VELUM
- [ ] Обновить `Velum_RecipeReflexEditor_ImplementationPlan.md`

### AIStudio
- [ ] Пульт: две колонки стимулов
- [ ] Редактор pressure rules
- [ ] ExterInalInfluencesView без probe key
- [ ] GeneticReflexes / EnvironmentTriggers — фильтр stimulus IDs
- [ ] ProjectBootstrap seed
- [ ] Миграционный wizard (опционально)

### ISIDA
- [ ] 5-столбцовый InfluenceActions (фаза 3)
- [ ] Obsolete EnvironmentMetricProbeKey
- [ ] Обновить docs isida

### Документация
- [ ] AdapterContract.md + html
- [ ] AdapterAuthorGuide.md + html
- [ ] demo template (все README + BootData)
- [ ] sldworks_19 installed package

---

## 14. Глоссарий

| Термин | Определение |
|--------|-------------|
| **Стимул** | дискретная запись в `InfluenceActions.dat`; проходит через `ApplyMultipleInfluenceActions` |
| **Правило давления** | запись в `EnvironmentPressureRules.dat`; probe → виталы на пульсе |
| **YAML-привязка** | наличие `influence_action_id` в `EnvironmentTriggers.yaml` |
| **Прямое воздействие** | стимул без YAML-привязки |
| **Через среду** | стимул с YAML-привязкой (ручная отправка + события host) |

---

*Конец документа.*
