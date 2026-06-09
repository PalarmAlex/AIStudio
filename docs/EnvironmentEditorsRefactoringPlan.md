# Рефакторинг редакторов среды ISIDA

Документ для реализации целевого UX редакторов рецептов и триггеров среды в AIStudio.
Статус: **фазы 1–2 реализованы** (2026-06-09); фазы 3–4 — в плане.

Связанные нормативные документы (источник правды — **md**, публикуемые копии — **html**):

| Markdown | HTML |
|----------|------|
| [AdapterContract.md](AdapterContract.md) | [AdapterContract.html](AdapterContract.html) |
| [AdapterAuthorGuide.md](AdapterAuthorGuide.md) | [AdapterAuthorGuide.html](AdapterAuthorGuide.html) |

При любых правках контракта или гайда **обновлять оба формата** в `docs\` (сначала `.md`, затем синхронизировать `.html`).

---

## Содержание

1. [Целевой дизайн экранов: редакторы среды ISIDA](#1-целевой-дизайн-экранов-редакторы-среды-isida)
2. [Спецификация ViewModel / событий](#2-спецификация-viewmodel--событий)
3. [План рефакторинга](#3-план-рефакторинга)

---

# 1. Целевой дизайн экранов: редакторы среды ISIDA

## 1.1. Принцип навигации: хаб + три редактора

**Не сливать** рецепты, триггеры и давление в один редактор макроса (как MS Access) — это сломает доменную модель ISIDA.

**Целевая структура меню:**

```
Среда
├── Обзор поведения          ← НОВЫЙ (хаб, read-only карта связей)
├── Триггеры среды           ← переработанный
├── Рецепты среды            ← переработанный
├── Давление на виталы       ← минимальные правки (тот же shell)
└── Адаптеры                 ← как сейчас
```

Хаб не редактирует данные — отвечает на вопрос *«как это всё связано?»*. Редактирование остаётся в специализированных экранах.

```
Событие host (триггер) → EA → рефлекс → G_AD → шаги рецепта (invoke) → Host runtime
```

## 1.2. Общая оболочка (Environment Editor Shell)

Все три редактора (и хаб) используют **один каркас**.

```
┌──────────────────────────────────────────────────────────────────────────┐
│ [← Меню]  Среда · {тип адаптера} · {имя агента} · стадия {N}            │
│ ⚠ {PulseWarningMessage}                                    [Сохранить]  │
├──────────────────────────────────────────────────────────────────────────┤
│ [Обзор] [Триггеры] [Рецепты] [Давление]     ← tab-bar внутри раздела    │
├──────────────────────────────────────────────────────────────────────────┤
│                     {содержимое активной вкладки}                        │
├──────────────────────────────────────────────────────────────────────────┤
│ Статус: изменено / сохранено · N ошибок валидации          [Отменить]   │
└──────────────────────────────────────────────────────────────────────────┘
```

**Правила shell:**

- Предупреждение о пульсации/стадии — всегда в шапке.
- Tab-bar переключает подстраницы без потери несохранённых данных (диалог «сохранить?»).
- Кнопка «Сохранить» сохраняет только активный файл (triggers / recipes / pressure).
- Единый стиль DataGrid как в `ScenarioEditorView`.

## 1.3. Переиспользуемый блок: SchemaActionPanel

Один UI-компонент на все «действие + параметры по schema»:

| Режим | Каталог слева | Параметры справа |
|-------|---------------|------------------|
| Шаг рецепта | `handlers-catalog.json` | `argsSchema` |
| Событие триггера | `trigger-detect.json` | `parameters` |

```
┌─ Каталог действий ─────┐ │ ┌─ Параметры ─────────────────────────────┐
│ 🔍 Поиск…              │ │ │ set_custom_property                      │
│ ● set_custom_property  │ │ │ Задать свойство документа                │
│   Запись в лог         │ │ ├──────────────────────────────────────────┤
│                        │ │ │ Имя *        [Обозначение        ]       │
│                        │ │ │ Шаблон       [{PROJECT}-{SEQ:4}  ] […]   │
│                        │ │ │ overwrite    [if_empty ▼]                │
│                        │ │ │ ℹ Подстановки: {PROJECT}, {DISCIPLINE}   │
└────────────────────────┘ │ └──────────────────────────────────────────┘
```

**Поведение:**

- Выбор в каталоге → перестроение формы параметров (без модального окна).
- Поля по `type`: `string` → TextBox, `bool` → CheckBox, `values[]` → ComboBox, `required` → inline-валидация.
- Кнопка `[…]` — lookup-диалог (фаза 3), если в schema указан lookup.
- Внизу — `description` handler/event из schema.
- **Никакого двойного щелчка** — выделение строки в master-списке.

## 1.4. Экран «Обзор поведения» (новый хаб)

**Задача:** показать цепочку целиком; клик → переход в нужный редактор с фильтром.

```
┌─ Фильтр ──────────────────────────────────────────────────────────────┐
│ Поиск: [save____________]   Показать: ☑ триггеры ☑ рецепты ☑ разрывы   │
└────────────────────────────────────────────────────────────────────────┘

┌─ Карточки связей ─────────────────────────────────────────────────────┐
│  ТРИГГЕР: save_active_document                                        │
│  └─ event: document_saved                                              │
│  └─ EA 101 «Сохранение документа»                                      │
│       └─ РЕФЛЕКС: [открыть дерево рефлексов →]                         │
│            └─ G_AD 37 «Заполнить свойства»                             │
│                 └─ РЕЦЕПТ: kb_name_on_save  (2 шага invoke)  [→]       │
│  ⚠ РАЗРЫВ: триггер before_rebuild (EA 102) — нет рецепта с G_AD…      │
└────────────────────────────────────────────────────────────────────────┘
```

**Компактная таблица:**

| Триггер | Событие | EA | G_AD | Рецепт | Статус |
|---------|---------|-----|------|--------|--------|
| save_active… | document_saved | 101 | 37 | kb_name_on_save | ✓ |
| before_rebuild | command_before | 102 | — | — | ⚠ |

Данные read-only; сборка из triggers.yaml + recipes.yaml + рефлексов.

## 1.5. Экран «Триггеры среды»

**Паттерн:** master-detail (40% / 60%).

```
┌─ Список триггеров (master) ──────────┐ ┌─ Детали (detail) ───────────────────┐
│ [+ Новый]  [🗑]  🔍 ID / Название    │ │ ID: [save_active_doc ▼] [Каталог]   │
│ ▶ save_active_document               │ │ Название: […]                       │
│   document_saved → EA 101            │ │ EA: [101 — … ▼]                     │
│   before_rebuild                     │ │ {SchemaActionPanel — event}         │
│                                      │ │ Связи: рефлексы 3 [→]  рецепты 1 [→]│
└──────────────────────────────────────┘ └─────────────────────────────────────┘
```

**Убрать:** двойной щелчок по ячейкам, модальный `EnvironmentTriggerEventEditorDialog`.

## 1.6. Экран «Рецепты среды»

Реестр → редактор с вкладками.

### Реестр

- Один клик / Enter открывает редактор (не только double-click).
- Колонки: ID, Название, G_AD, Шагов, ⚠.

### Редактор рецепта

```
[← К реестру]   РЕЦЕПТ: kb_name_on_save

[Основное] [Шаги] [Связи] [Тест]
```

**Вкладка «Основное»:** ID, название, описание, G_AD (ComboBox), reactive_eligible, рекомендуемый EA.

**Вкладка «Шаги»** — центральный макро-редактор:

```
[+ Шаг] [+ Комментарий] [▲] [▼] [🗑] [▶ Выполнить выбранный]

│ № │ Действие              │ Кратко                    │
│ 1 │ set_custom_property   │ Обозначение: {PROJECT}…   │
│ 2 │ set_custom_property   │ Наименование: …           │
│ — │ 💬 Заполнить по КБ    │ (комментарий)             │

{SchemaActionPanel — handler, привязан к выделенной строке}
```

**Вкладка «Связи»:** G_AD → рефлексы; recommended EA → триггеры; handlers в шагах vs catalog.

**Вкладка «Тест»** (фаза 3): прогон шагов через adapter host.

## 1.7. Экран «Давление на виталы»

Только оболочка shell + tooltips из `metric-probes.json`. Логика данных без изменений.

## 1.8. Сравнение: сейчас → целевое

| Аспект | Сейчас | Целевое |
|--------|--------|---------|
| Редактирование шага/события | Double-click → модалка | Выбор строки → панель |
| Триггеры | Плоская таблица | Master-detail |
| Рецепты | Read-only шаги | Шаги = главный экран |
| Связи EA↔G_AD↔рецепт | Текст внизу | Вкладка «Связи» + хаб |
| Enum в schema | Игнорируется | ComboBox |
| Порядок шагов | Порядок добавления | ▲▼ / drag |
| Навигация | 3 пункта меню | Shell + tab-bar + хаб |

## 1.9. Состояния и affordances

**Только чтение** (`!IsEditingEnabled`): поля disabled, навигация и связи работают; Save/New/Delete скрыты.

**Валидация:** inline при изменении + сводка при Save; ⚠ в master-списке.

**Клавиатура:** Enter, Delete, Ctrl+S, Ctrl+N, F1 (справка по handler/event).

**Пустые состояния:** явные тексты + кнопка «Создать из шаблона адаптера».

## 1.10. Пропорции окна

| Зона | Мин. ширина |
|------|-------------|
| Master-список | 320 px |
| Каталог SchemaActionPanel | 240 px (сворачиваемый) |
| Параметры | 360 px |
| Панель связей | 280 px или отдельная вкладка |

---

# 2. Спецификация ViewModel / событий

## 2.1. Изменения форматов данных (без обратной совместимости)

### Шаги рецепта в YAML

**Убрать** строковое поле `args`. Аргументы handler'а — **отдельные scalar-ключи** на уровне шага:

```yaml
steps:
  - type: invoke
    handler: set_custom_property
    config: active
    name: Обозначение
    template: "{PROJECT}-{DISCIPLINE}-{SEQ:4}"
    overwrite: if_empty
  - type: comment
    text: "Заполнить оба поля по КБ"
```

| `type` | Ключи | Runtime |
|--------|-------|---------|
| `invoke` | `handler` + ключи из `argsSchema` | исполняется host |
| `comment` | `text` | пропускается |

Зарезервированные ключи шага: `type`, `handler`, `text`.

### Новый файл schema: `trigger-catalog.json`

```json
{
  "schemaVersion": "2.0",
  "triggers": [
    {
      "id": "save_active_document",
      "label": "Сохранение активного документа",
      "description": "Событие document_saved → EA 101"
    }
  ]
}
```

Пятый обязательный файл schema. Формат YAML триггеров **не меняется**.

## 2.2. Модели данных

### `EnvironmentRecipeStepData` (Contract)

```csharp
public sealed class EnvironmentRecipeStepData
{
  public string Type { get; set; }           // "invoke" | "comment"
  public string Handler { get; set; }        // invoke
  public string Text { get; set; }           // comment
  public Dictionary<string, string> Args { get; set; }  // invoke
}
```

### `EnvironmentRecipeStepRow` (редактор)

| Поле | Тип | INPC |
|------|-----|------|
| `StepKind` | `string` | да |
| `HandlerId` | `string` | да |
| `Args` | словарь / `SchemaParamRow` | да |
| `CommentText` | `string` | да |
| `Summary` | `string` | да |
| `ValidationError` | `string` | да |

### `SchemaParamRow` (новый)

`Key`, `Label`, `Type`, `Required`, `AllowedValues`, `Value`, `ValidationError`.

### `EnvironmentLinkItem` / `EnvironmentNavigationRequest`

Для панели связей и навигации между вкладками / деревом рефлексов.

## 2.3. Иерархия ViewModel

```
EnvironmentShellViewModel
├── EnvironmentBehaviorOverviewViewModel
├── EnvironmentTriggersViewModel
├── EnvironmentRecipesRegistryViewModel → EnvironmentRecipeEditorViewModel
├── EnvironmentPressureRulesViewModel
└── SchemaActionEditorViewModel (вложенный в Triggers / RecipeEditor)
```

## 2.4. EnvironmentShellViewModel (новый)

**Файлы:** `ViewModels/Environment/EnvironmentShellViewModel.cs`, `Pages/Environment/EnvironmentShellView.xaml`

### Свойства

| Свойство | Тип |
|----------|-----|
| `CurrentAgentTitle` | `string` |
| `AdapterDisplayName` | `string` |
| `IsEditingEnabled` | `bool` |
| `PulseWarningMessage` | `string` |
| `WarningMessageColor` | `Brush` |
| `SelectedTab` | `EnvironmentShellTab` |
| `ActiveChild` | `object` |
| `HasUnsavedChanges` | `bool` |
| `ValidationIssueCount` | `int` |
| `StatusMessage` | `string` |

```csharp
public enum EnvironmentShellTab { Overview, Triggers, Recipes, Pressure }
```

### Команды

| Команда | Действие |
|---------|----------|
| `SaveCommand` | делегирует активной вкладке |
| `DiscardCommand` | перезагрузка активной вкладки |
| `NavigateTabCommand` | `EnvironmentShellTab` |
| `NavigateRequestCommand` | `EnvironmentNavigationRequest` |

### События

| Событие | Назначение |
|---------|------------|
| `RequestOpenRecipeEditor` | открыть редактор рецепта |
| `RequestCloseRecipeEditor` | вернуться к реестру |
| `ChildDirtyStateChanged` | `bool` |
| `ChildValidationChanged` | `int` |

### MainViewModel

Пункты меню 45/46/48 → `ShowEnvironmentShell(tab)`. Новый пункт → `ShowEnvironmentShell(Overview)`.

## 2.5. SchemaActionEditorViewModel (новый)

**Файлы:** `ViewModels/Environment/SchemaActionEditorViewModel.cs`, `Controls/Environment/SchemaActionPanel.xaml`

```csharp
public enum SchemaActionEditorMode { RecipeHandler, TriggerEvent }
```

### Свойства

`Mode`, `IsEditingEnabled`, `CatalogItems`, `CatalogSearchText`, `SelectedCatalogId`, `Parameters`, `ValidationError`, `SelectedDescription`.

### Команды

`ApplyCatalogFilterCommand`, `PickLookupValueCommand` (фаза 3), `ShowHelpCommand`.

### Методы

`LoadFromHandler(...)`, `LoadFromEvent(...)`, `TryCommit(out error)`.

### События

`SelectedCatalogChanged`, `ParameterValueChanged`, `ValuesCommitted`.

**Заменяет:** `RecipeStepHandlerEditorDialog`, `EnvironmentTriggerEventEditorDialog`.

## 2.6. EnvironmentTriggersViewModel (переработка)

### Свойства

`Triggers`, `SelectedTrigger`, `FilterIdText`, `FilterTitleText`, `TriggerIdCatalog`, `EventSchema` (вложенный `SchemaActionEditorViewModel`), `Links`, `Dirty`, `ValidationIssues`.

### Команды

`SaveCommand`, `AddTriggerCommand`, `DeleteSelectedCommand`, `ApplyFiltersCommand`, `ResetFiltersCommand`, `RemoveAllCommand`, `PickTriggerIdFromCatalogCommand`, `PickInfluenceActionCommand`, `NavigateToReflexesCommand`, `NavigateToRecipesCommand`.

### События / реакции

- `SelectedTrigger` changed → `EventSchema.LoadFromEvent`, `RefreshLinks`.
- `EventSchema.ValuesCommitted` → запись в row, `Dirty = true`.

## 2.7. EnvironmentRecipeEditorViewModel (переработка)

```csharp
public enum RecipeEditorTab { General, Steps, Links, Test }
```

### Свойства

`Model`, `SelectedTab`, `Steps`, `SelectedStep`, `StepHandlerSchema`, `Links`, `Dirty`, `ValidationIssues`, прокси полей G_AD / EA / ID.

### Команды

`SaveCommand`, `CancelCommand`, `AddInvokeStepCommand`, `AddCommentStepCommand`, `DeleteSelectedStepsCommand`, `MoveStepUpCommand`, `MoveStepDownCommand`, `NavigateToTriggerCommand`, `NavigateToReflexesCommand`, `RunSelectedStepCommand` (фаза 3).

### События

`RequestClose`, `CloseAction`; `SelectedStep` changed → загрузка `StepHandlerSchema`; `ValuesCommitted` → обновление шага.

**Удалить:** `TryEditStep(Window, row)`.

## 2.8. EnvironmentBehaviorOverviewViewModel (фаза 2)

`Chains`, `FilterText`, `ShowGapsOnly`, `GapCount`, `RefreshCommand`, `OpenTriggerCommand`, `OpenRecipeCommand`, `OpenReflexTreeCommand`.

**Сервис:** `EnvironmentLinksService` — read-only сборка связей из YAML + рефлексов.

## 2.9. IEnvironmentChildViewModel

```csharp
public interface IEnvironmentChildViewModel : IDisposable
{
  bool Dirty { get; }
  int ValidationIssueCount { get; }
  bool CanSave { get; }
  void Save();
  void Reload();
  event Action DirtyChanged;
  event Action<int> ValidationIssueCountChanged;
}
```

Реализуют: Triggers, Pressure, Overview; Registry — частично.

---

# 3. План рефакторинга

## 3.0. Обязательное чтение правил (перед любой работой)

### AIStudio — `D:\ISIDA\Programms\app\AIStudio\.cursor\rules\`

| Файл | Соблюдать |
|------|-----------|
| `aistudio-client-standards.mdc` | C# 7.3, MVVM, `RelayCommand`, `IDisposable` |
| `aistudio-build-verify.mdc` | MSBuild Debug после каждой фазы; isida → AIStudio |
| `aistudio-xaml-formatting.mdc` | пустые строки только между логическими блоками |
| `aistudio-xaml-safety.mdc` | **не** `ItemTemplate` у ComboBox; ToolTip через `ItemContainerStyle` |

### Velum — `D:\VELUM\Velum\.cursor\rules\` (runtime исполнения шагов)

| Файл | Соблюдать |
|------|-----------|
| `velum-csproj-includes.mdc` | новые `.cs` вручную в `velum.csproj` |
| `velum-csharp.mdc` | стиль C# |

---

## Фаза 1 — Contract + формат + SchemaActionPanel

**Цель:** новый формат шагов, `trigger-catalog.json`, убрать модальные диалоги.

### SymbiontEnv.Contract

| Файл | Действие |
|------|----------|
| `ContractModels.cs` | новая модель шага |
| `EnvironmentYamlCodec.cs` | invoke: flat keys; comment: `text`; убрать строковый `args` |
| `EnvironmentYamlCodec.Writer.cs` | запись нового формата |
| `AdapterPackageValidator.cs` | `trigger-catalog.json`, шаги comment/invoke |

### Velum

| Файл | Действие |
|------|----------|
| `ReactiveCore/RecipeExecutor.cs` | `comment` → skip; invoke из flat keys |
| `ReactiveCore/RecipeStepHandlerRegistry.cs` | args как `IDictionary` |
| `ReactiveCore/EnvironmentContractMapper.cs` | новый маппинг |
| `velum.csproj` | при новых файлах |

### AIStudio

| Файл | Действие |
|------|----------|
| `AdapterSchemaModels.cs` | `AdapterSchemaTriggerCatalogEntry` |
| `AdapterSchemaLoader.cs` | `LoadTriggerCatalog` |
| `EnvironmentRecipeMapper.cs`, `EnvironmentRecipeStepSchemaHelper.cs` | flat args, comment |
| `SchemaActionEditorViewModel.cs`, `SchemaActionPanel.xaml` | **новые** |
| `EnvironmentTriggersView.xaml(.cs)`, `EnvironmentTriggersViewModel.cs` | master-detail |
| `EnvironmentRecipeEditorView.xaml(.cs)`, `EnvironmentRecipeEditorViewModel.cs` | вкладки |
| `AIStudio.csproj` | синхронизация |

### Удалить

- `Dialogs/RecipeStepHandlerEditorDialog.xaml(.cs)`
- `Dialogs/EnvironmentTriggerEventEditorDialog.xaml(.cs)`

### Подключить

- `RecipeStepValueSelectionDialog` — из `SchemaActionPanel` для `values[]`

### Нормативная документация (md + html)

**Обязательно** в конце фазы 1 (и при последующих изменениях контракта — в той же фазе, где меняется кодек/schema):

| Файл | Действие |
|------|----------|
| `docs\AdapterContract.md` | обновить норму полей и примеры |
| `docs\AdapterContract.html` | **синхронизировать** с `.md` (те же разделы и примеры) |
| `docs\AdapterAuthorGuide.md` | обновить практику авторов и потоки в студии |
| `docs\AdapterAuthorGuide.html` | **синхронизировать** с `.md` |

**Разделы `AdapterContract.md` / `.html` для правки:**

- § Runtime contract: `EnvironmentRecipes.yaml` — формат `steps`: flat-ключи invoke, `type: comment`, убрать строковый `args`.
- § Блок `steps` — таблица параметров invoke; зарезервированные ключи `type`, `handler`, `text`.
- § Schema JSON — добавить `trigger-catalog.json` (5-й обязательный файл); уточнить `handlers-catalog.json` (`values`, `required`).
- § Примеры YAML (эталон) — переписать под новый формат; явно: legacy single-recipe в корне и `args: "k=v; …"` **не поддерживаются**.
- § Проверка пакета (`AdapterPackageValidator`) — упоминание `trigger-catalog.json` и шагов `comment`.

**Разделы `AdapterAuthorGuide.md` / `.html` для правки:**

- § Подготовьте `schema\` — таблица файлов: + `trigger-catalog.json`.
- § Примеры `handlers-catalog.json`, `trigger-detect.json`, `trigger-catalog.json`.
- § Подготовьте `BootData\Environment\` — примеры `EnvironmentRecipes.yaml` (flat args + comment).
- § Типичные потоки в студии — редакторы «Среда», ссылка на [EnvironmentEditorsRefactoringPlan.md](EnvironmentEditorsRefactoringPlan.md) (целевой UX).
- § Типичные ошибки — убрать/заменить упоминания строкового `args`.

**Копии в пакетах адаптеров** (содержание должно соответствовать актуальному `docs\AdapterContract.md`):

| Путь | Файлы |
|------|-------|
| `docs\AdapterPackageTemplates\demo\` | `AdapterContract.md` (указатель на `docs\`) |
| `C:\ProgramData\ISIDA\Adapters\sldworks_19\` | `AdapterContract.md` |

HTML в пакетах адаптеров не дублируется — ссылка на `docs\AdapterContract.html` в demo `AdapterContract.md` при необходимости.

**Порядок обновления документации:** правка `.md` → проверка примеров YAML/JSON → синхронизация `.html` → README в schema/BootData.

### Обновление источников данных и README

Синхронно обновить:

| Каталог | Файлы |
|---------|-------|
| `D:\ISIDA\Programms\app\AIStudio\docs\` | `AdapterContract.md`, `AdapterContract.html`, `AdapterAuthorGuide.md`, `AdapterAuthorGuide.html` |
| `...\docs\AdapterPackageTemplates\demo\schema\` | `README.txt`, **новый** `trigger-catalog.json` |
| `...\docs\AdapterPackageTemplates\demo\BootData\Environment\` | `README.txt`, `EnvironmentRecipes.yaml` |
| `C:\ProgramData\ISIDA\AdapterPackageTemplates\demo\` | зеркало demo |
| `C:\ProgramData\ISIDA\Adapters\sldworks_19\` | schema, BootData, README, `AdapterContract.md` |
| `C:\ProgramData\VELUM\BootData\Environment\` | `README.txt`, `EnvironmentRecipes.yaml` |

**В README указать:**

- flat-ключи invoke вместо `args: "k=v; …"`;
- `type: comment`;
- `trigger-catalog.json` как 5-й обязательный schema-файл;
- legacy single-recipe YAML и строковый `args` **не поддерживаются**.

### Проверка

SymbiontEnv.Contract → Velum → AIStudio Debug (MSBuild по `aistudio-build-verify.mdc`).

---

## Фаза 2 — Shell + Обзор + Связи

### Новые файлы

- `EnvironmentShellViewModel.cs`, `EnvironmentShellView.xaml`
- `EnvironmentBehaviorOverviewViewModel.cs`, `EnvironmentBehaviorOverviewView.xaml`
- `EnvironmentLinksService.cs`, `IEnvironmentChildViewModel.cs`
- `EnvironmentLinkItem.cs`, `EnvironmentBehaviorChainRow.cs`, `ValidationIssueRow.cs`

### Изменения

- `MainViewModel.cs` — `ShowEnvironmentShell`, меню «Обзор поведения»
- Вкладки «Связи» в редакторе рецепта и триггеров
- `EnvironmentPressureRulesView` — в shell

### Документация (md + html)

- `AdapterAuthorGuide.md` / `.html` — § «Типичные потоки»: Environment Shell, вкладка «Обзор поведения», навигация между триггерами и рецептами.
- `AdapterContract.md` / `.html` — при необходимости уточнить, что UI студии использует `trigger-catalog.json` для выбора ID триггера (если не описано в фазе 1).

---

## Фаза 3 — Polish + тест шагов

- Drag-reorder шагов
- Lookup в schema (`command_ids` и т.п.)
- Вкладка «Тест» + IPC к host
- Подсветка последнего срабатывания триггера

---

## Фаза 4 — Зачистка

- Удалить мёртвый code-behind (`MouseDown` handlers)
- Унифицировать заголовки через Shell
- `AdapterPackageValidator` на demo и sldworks_19
- Обновить YAML во всех активных `BootData\Environment\` проектов
- Финальная сверка: `AdapterContract` и `AdapterAuthorGuide` — **md и html** содержат одинаковые примеры и не ссылаются на удалённые диалоги / строковый `args`

---

## Сводка файлов

### Новые (AIStudio)

```
ViewModels/Environment/EnvironmentShellViewModel.cs
ViewModels/Environment/SchemaActionEditorViewModel.cs
ViewModels/Environment/SchemaParamRow.cs
ViewModels/Environment/EnvironmentBehaviorOverviewViewModel.cs
ViewModels/Environment/IEnvironmentChildViewModel.cs
ViewModels/Environment/EnvironmentLinkItem.cs
ViewModels/Environment/EnvironmentBehaviorChainRow.cs
ViewModels/Environment/ValidationIssueRow.cs
Common/Environment/EnvironmentLinksService.cs
Common/Environment/SchemaCatalogItemRow.cs
Controls/Environment/SchemaActionPanel.xaml(.cs)
Pages/Environment/EnvironmentShellView.xaml(.cs)
Pages/Environment/EnvironmentBehaviorOverviewView.xaml(.cs)
```

### Удаляемые (AIStudio)

```
Dialogs/RecipeStepHandlerEditorDialog.xaml(.cs)
Dialogs/EnvironmentTriggerEventEditorDialog.xaml(.cs)
```

### Velum

```
ReactiveCore/RecipeExecutor.cs
ReactiveCore/RecipeStepHandlerRegistry.cs
ReactiveCore/EnvironmentContractMapper.cs
```

### Документация (md + html)

```
docs/AdapterContract.md
docs/AdapterContract.html
docs/AdapterAuthorGuide.md
docs/AdapterAuthorGuide.html
docs/EnvironmentEditorsRefactoringPlan.md
```

---

## Чеклист разработчика

1. Прочитать правила AIStudio и Velum.
2. Contract + codec + validator + эталонные YAML.
3. Velum `RecipeExecutor`.
4. `AdapterSchemaLoader` + `trigger-catalog.json` во всех пакетах.
5. `SchemaActionEditorViewModel` + `SchemaActionPanel`.
6. Triggers VM/View → Recipe Editor VM/View.
7. Удалить старые диалоги; обновить `AIStudio.csproj`.
8. MSBuild AIStudio Debug.
9. Обновить `AdapterContract` и `AdapterAuthorGuide` (**md + html**), затем README и YAML (таблица в фазе 1).
10. Фаза 2: Shell + Overview + Links; снова сверить md/html гайдов.
11. Финальная сборка: isida → Contract → Velum → AIStudio.
12. Фаза 4: финальная сверка md/html документации с реализованным UI.

---

## Риски

| Риск | Митигация |
|------|-----------|
| Сломать Velum при смене формата args | Фаза 1 включает Velum до UI |
| ComboBox ItemTemplate crash | `DisplayMemberPath` + `ItemContainerStyle` |
| C# 8+ | только C# 7.3 |
| Забытый Include в csproj | чеклист в конце фазы |
| Дублирование конвертеров | расширять существующие в `Converters/` |
