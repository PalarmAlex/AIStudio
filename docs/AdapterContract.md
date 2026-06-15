# Контракт платформы адаптеров среды (AIStudio)

**Версия контракта:** `3.0`  
**Дата:** 2026-06-13  
**Статус:** нормативный документ для AIStudio, авторов адаптеров и runtime host. AIStudio **не обязательна** для разработки host; пакет с `manifest.json` нужен только при интеграции со студией (§ 0.5). Кодек YAML и проверка пакета — **`SymbiontEnv.Contract.dll`** (общая библиотека студии и host).

Связанные документы:

| Документ | Назначение |
|----------|------------|
| [`SymbiontArchitecture_OperatorEnvironment_Spec.md`](../../../../VELUM/Velum/docs/SymbiontArchitecture_OperatorEnvironment_Spec.md) (v2.2) | **Архитектура:** operator vs environment, Expression/Command, `BaseExternalActions`, `PulseHomeostasisLedger`, `IHostMotorDispatcher` |
| [`Velum_RecipeReflexEditor_ImplementationPlan.md`](../../../../VELUM/Velum/docs/Velum_RecipeReflexEditor_ImplementationPlan.md) (v2.2) | Эталонная реализация Reactive Core (Velum) |
| [`AdapterAuthorGuide.md`](AdapterAuthorGuide.md) (v1.0) | Практика сборки и регистрации пакета (contract 3.0) |
| [`AdapterPlatform_ImplementationPlan.md`](AdapterPlatform_ImplementationPlan.md) | План реализации в студии |

**Синхронизация с архитектурой v2.2:** YAML contract 3.0 **не** использует `adaptive_action_id`, `influence_action_id`, `InfluenceActions.dat` как прокси среды. Рецепты привязаны к **`expression_pattern_id`** (Expression channel); триггеры — к **`homeostasis_deltas`** (mechanical path) и **`reflex_trigger_expression_pattern_id`** (`expr:env.*`). Legacy read/write contract 2.0 **не поддерживается**.

---

## 0. Термины и роли

### 0.1. Проект симбионта

**Корневой каталог данных одного симбионта** — то, что в меню «Проект → Создать / Открыть проект симбионта».

```
{projectRoot}\
  Settings\Settings.xml     — профиль проекта (пути Data, BootData)
  Data\Gomeostas\AgentProperties.dat — свойства симбионта, в т.ч. AdapterId
  Data\                     — ISIDA (.dat): гомеостаз, рефлексы, каналы…
  BootData\                 — boot-данные, в т.ч. Environment\*.yaml при работе со средой
```

| Действие в студии | Смысл |
|-------------------|--------|
| **Создать проект симбионта** | Каркас системы гомеостаза и настроек; тип среды (**AdapterId**) — **опция** |
| **Открыть проект симбионта** | Загрузить существующий каталог для редактирования |

**Без `AdapterId`** проект полностью работоспособен для гомеостаза, рефлексов, пульта оператора и виртуальных тестов. Меню «Среда» (рецепты/триггеры) недоступно.

**Студия не загружает runtime DLL** при открытии или редактировании проекта симбионта.

### 0.2. Пакет адаптера (зарегистрированный)

**Глобальный каталог описания среды**, не привязанный к одному проекту симбионта:

```
%ProgramData%\ISIDA\Adapters\{adapterId}\
  manifest.json
  schema\                   — описание capability (редакторы «Среда», ProbeKey)
  BootData\                 — образцы YAML для seed новых проектов
  runtime\                  — DLL host + isida.dll (дистрибуция среды; не для редактора)
```

| Действие в студии | Смысл |
|-------------------|--------|
| **Зарегистрировать пакет…** | Скопировать папку/ZIP в `Adapters\{id}\`, проверить структуру |

**«Зарегистрировать пакет» ≠ «подключиться к среде».** Студия использует **файлы пакета** (manifest, schema, образцы BootData). Runtime host и целевое приложение **не вызываются** из процесса AIStudio при редактировании.

Один зарегистрированный пакет (`my-adapter`) может обслуживать **много** проектов симбионта.

**Каркас для разработки** (шаблон, не регистрировать как адаптер):

```
%ProgramData%\ISIDA\AdapterPackageTemplates\demo\
```

### 0.3. Runtime host (исполнитель среды)

**`runtime\*.dll`** — программа, которая **вне студии** читает YAML проекта, исполняет рецепты/триггеры и реализует **`IHostMotorDispatcher`** (Spec v2.2 §3.7).

| Где нужен runtime | Когда |
|-------------------|--------|
| В пакете адаптера | Дистрибуция; проверка «Проверить» (наличие файлов, в т.ч. `isida.dll`) |
| На машине пользователя | Работа в реальной среде (целевое приложение и т.д.) |
| В студии | **Не загружается** |

### 0.4. Связь проекта симбионта и пакета

Поле **`AdapterId`** в `Data\Gomeostas\AgentProperties.dat` — метка:

> «Для этого симбионта используем описание среды из зарегистрированного пакета `{id}`».

От `AdapterId` зависят:

- доступность редакторов меню «Среда»;
- seed BootData при **создании** проекта (если тип среды выбран);
- combobox и подсказки из `schema\` — ProbeKey в `EnvironmentPressureRules.dat`, каталог рецептов из `recipe-catalog.json`.

### 0.5. Когда нужен пакет для студии

Ядро платформы для host — **`isida.dll`**, runtime contract YAML (§ 5–7), **`IHostMotorDispatcher`**. Формат **пакета** обязателен **только** при использовании AIStudio.

| Компонент | Только host (без студии) | С AIStudio |
|-----------|--------------------------|------------|
| `isida.dll` | **Обязательно** | **Обязательно** в `runtime\` |
| `manifest.json`, `Adapters\{id}\` | Не нужны | Нужны для регистрации |
| `BootData\Environment\*.yaml` в пакете | Не нужны | Нужны (seed проекта) |
| `schema\` | Не нужна | **Обязательна** для регистрации |

### 0.6. Operator path vs mechanical path (норматив Spec v2.2)

**Не смешивать** в YAML и runtime:

| Путь | Источник | YAML / ISIDA | Попадает в operator `PerceptionImage`? |
|------|----------|--------------|----------------------------------------|
| **Operator** | Пульт: Verbal, Command, Expression, Visual, `HomeostasisSignificance` | каналы + significance | **Да** |
| **Mechanical (среда)** | Триггеры SW, pressure rules, SessionHealth | `homeostasis_deltas` в triggers; **не** EA-прокси | **Нет** |
| **Наблюдаемая команда SW** | Command buffer host | `CommandPatternIdList` | **Да** (отдельно от mechanical delta) |

- Учительский сдвиг гомеостаза с пульта — **только** `HomeostasisSignificance` (не `InfluenceActions.dat`).
- Событие SW → гомеостаз — **только** `homeostasis_deltas` + запись в `PulseHomeostasisLedger` (`EnvironmentMechanical`).
- Рецепт исполняется по **`expression_pattern_id`** (`expr:velum.recipe.*`) через `IHostMotorDispatcher`, не по `AdaptiveActions.dat`.
- **Double dispatch запрещён:** одно событие SW — Command buffer **или** mechanical trigger, не оба с одной семантикой (Spec §4.2).

### 0.7. Роли пользователей

| Роль | Типичный поток |
|------|----------------|
| **Настройщик симбионта** | Зарегистрировать пакет → создать/открыть проект → настроить рецепты/триггеры v3 |
| **Автор адаптера (только host)** | Host + `isida.dll` + `IHostMotorDispatcher` → YAML contract 3.0 |
| **Автор адаптера + студия** | Пакет → регистрация → тестовый проект с `AdapterId` |

### 0.8. Запрещённые формулировки в UI

Избегать: «подключить адаптер», «адаптер активен», «запустить адаптер».

Использовать: **«зарегистрировать пакет»**, **«тип среды»**, **«описание пакета `{id}`»**.

---

## 1. Область действия

### 1.1. Что регулирует контракт

1. **Структура пакета адаптера** — каталог в `%ProgramData%\ISIDA\Adapters\{id}\`.
2. **`manifest.json`** — метаданные и `contractVersion`.
3. **Runtime contract YAML** — `EnvironmentRecipes.yaml`, `EnvironmentTriggers.yaml` (contract **3.0**).
4. **Нормализация** — канонические ключи, round-trip через `SymbiontEnv.Contract.dll`.
5. **UI schema** (`schema\`) — handler'ы, события, каталоги для редакторов «Среда».

### 1.2. Что контракт не регулирует

- Форматы ISIDA (`.dat`: genetic reflexes, `BaseExternalActions.dat`, Expression primaries, гомеостаз) — `isida.dll` и редакторы студии; норматив — **Spec v2.2**.
- Реализацию API целевой среды — только в runtime host.
- Загрузку DLL адаптера в AIStudio — **запрещена**.

### 1.3. Роли

| Роль | Ответственность |
|------|-----------------|
| **AIStudio** | Редактирование данных; регистрация пакетов; запись YAML v3; валидация binding expression patterns |
| **Пакет адаптера** | manifest, BootData-образцы, runtime (`isida.dll` + host), schema |
| **Runtime host** | `IHostMotorDispatcher`; mechanical triggers; исполнение рецептов |
| **Проект симбионта** | `AgentProperties.dat`; `BootData\Environment\*.yaml`; `Data\` |

---

## 2. Версионирование

### 2.1. `contractVersion` в manifest

| Значение | Поддержка AIStudio (текущая) |
|----------|------------------------------|
| `3.0` | **Да (текущая)** — expression patterns, mechanical deltas |
| `2.0` | **Нет** — `adaptive_action_id` / `influence_action_id` устарели |
| `1.0` | Нет |

- Студия **отклоняет** пакет с неизвестной major-версией.
- Contract 3.0 — **breaking change** относительно 2.0; миграция boot — утилита студии (Spec §2.5), не dual-read в codec.

### 2.2. `version` в manifest

Версия **пакета адаптера** (SemVer). Не путать с `contractVersion`.

### 2.3. Политика изменений

| Изменение | Действие |
|-----------|----------|
| Новый обязательный ключ YAML | Новая major `contractVersion` |
| Новый опциональный ключ | Документировать; runtime игнорирует неизвестные ключи |
| Смена канона записи | Обновить § 5–7 + round-trip тесты |

---

## 3. Регистрация и пути

### 3.1. Каталог адаптеров

```
%ProgramData%\ISIDA\Adapters\{adapterId}\
```

- `adapterId` = `id` в `manifest.json`; символы `[a-z0-9_-]+`.

### 3.2. Проект симбионта

```
{projectRoot}\
  Settings\Settings.xml
  Data\Gomeostas\AgentProperties.dat
  BootData\Environment\
    EnvironmentRecipes.yaml
    EnvironmentTriggers.yaml
  Data\                       — ISIDA (.dat), incl. BaseExternalActions.dat, Expression primaries
```

### 3.3. AdapterId

```
AdapterId|my-adapter
```

- Пустой `AdapterId` → меню «Среда» заблокировано; гомеостаз и пульт доступны.

---

## 4. Структура пакета адаптера

### 4.1. MVP

```
{packageRoot}\
  manifest.json                         ОБЯЗАТЕЛЬНО (contractVersion 3.0)
  BootData\Environment\*.yaml           ОБЯЗАТЕЛЬНО (допускается пустые списки)
  runtime\                              ОБЯЗАТЕЛЬНО для DLL-host
  schema\                               ОБЯЗАТЕЛЬНО для регистрации в студии
```

### 4.2. Имена файлов BootData

| Файл | Строго |
|------|--------|
| Рецепты | `BootData\Environment\EnvironmentRecipes.yaml` |
| Триггеры | `BootData\Environment\EnvironmentTriggers.yaml` |

---

## 5. Нормализация YAML (round-trip)

### 5.1. Принцип

- **Канон записи** — ключи из § 6–7.
- **Dual round-trip** через `EnvironmentYamlCodec` (`SymbiontEnv.Contract.dll`).

### 5.2. Таблица алиасов

| Область | Канон (запись) | Алиасы (только чтение) |
|---------|----------------|------------------------|
| ID рецепта | `id` | `recipe_id` |
| ID триггера | `id` | `trigger_key` |

**Удалены (contract 3.0):** `adaptive_action_id`, `influence_action_id`, `recommended_trigger_influence_ids` — при чтении **игнорируются** с Warning в валидаторе.

### 5.3. Внутренняя модель после чтения

- Шаг рецепта: `type: invoke` + `handler` + flat args; `type: comment` + `text`.
- Триггер: `event` + scalar-параметры события + `homeostasis_deltas` + `reflex_trigger_expression_pattern_id`.

### 5.4. Пустые коллекции

```yaml
homeostasis_deltas: []
command_ids: []
recommended_trigger_keys: []
```

### 5.5. Кодировка

UTF-8 без BOM; подмножество YAML 1.1.

---

## 6. Runtime contract: `EnvironmentRecipes.yaml`

### 6.1. Корневая структура (contract 3.0)

```yaml
recipes:
  - id: <string>                         # обязательно, уникален в файле
    expression_pattern_id: <int>         # обязательно, > 0; ID в Expression PhraseTree
    display_name: <string>               # опционально
    description: <string>                # опционально
    reactive_eligible: <bool>            # опционально; default true
    recommended_trigger_keys: [<string>] # опционально, справочно (id триггеров)
    steps: [ ... ]                       # опционально
```

**Удалено относительно contract 2.0:** `adaptive_action_id`, `recommended_trigger_influence_ids`.

Условия запуска рецепта задаются **GeneticReflex Level3** (`expr:velum.recipe.*`) и **`IHostMotorDispatcher`**, не полями G_AD / AdaptiveActions.

### 6.2. Связь с ISIDA (Spec v2.2)

| Поле | Справочник ISIDA | Назначение |
|------|------------------|------------|
| `expression_pattern_id` | Expression channel (`DefaultExpressionPrimaries.tmp`, `ExpressionPhrases.dat`) | Host ищет рецепт при dispatch моторного образа с этим pattern ID |
| `recommended_trigger_keys` | `EnvironmentTriggers.yaml` (`id`) | Подсказка редактору; **не** исполняется runtime из YAML |

Runtime **не** проверяет существование pattern ID в boot при чтении YAML; валидация — студия (§ 12) и smoke-тесты host.

**`BaseExternalActions.dat`:** motor metadata (antagonists, vigor) для того же ID — **не** поле рецепта; arbitration в `isida` до dispatch.

### 6.3. Блок `steps`

Без изменений принципа: `invoke` (host) и `comment` (пропуск).

```yaml
- type: invoke
  handler: <string>
  <arg_key>: <scalar>
```

Handler'ы — в `schema/handlers-catalog.json`.

### 6.4. Пример (эталон contract 3.0)

```yaml
# Рецепты среды: моторика host. Запуск — genetic L3 expr:velum.recipe.* → IHostMotorDispatcher.
recipes:
  - id: kb_name_on_save
    display_name: "Наименование по КБ после сохранения"
    description: "Заполнить Обозначение и Наименование по шаблону политики КБ"
    expression_pattern_id: 42001
    reactive_eligible: true
    recommended_trigger_keys: [save_active_document]
    steps:
      - type: invoke
        handler: set_custom_property
        config: active
        name: Обозначение
        template: "{PROJECT}-{DISCIPLINE}-{SEQ:4}"
        overwrite: if_empty
      - type: comment
        text: "Привязка: expr:velum.recipe.kb_apply_on_save (ID 42001 в boot проекта)"
```

---

## 7. Runtime contract: `EnvironmentTriggers.yaml`

### 7.1. Корневая структура (contract 3.0)

```yaml
triggers:
  - id: <string>                                    # обязательно, уникален
    display_name: <string>                          # опционально
    event: <string>                                 # обязательно; kind из trigger-detect.json
    homeostasis_deltas:                             # опционально; mechanical path
      - parameter_id: <int>
        delta: <float>
    reflex_trigger_expression_pattern_id: <int>     # опционально; expr:env.* для Genetic L3
    <event_param>: <scalar>                         # опционально; см. § 7.3
```

**Удалено относительно contract 2.0:** `influence_action_id`.

**Правила mechanical path (Spec §4.1, §5.4):**

- `homeostasis_deltas` → `PulseHomeostasisLedger` (`EnvironmentMechanical`); **не** operator `PerceptionImage`.
- `reflex_trigger_expression_pattern_id` → пусковой паттерн `expr:env.*` для genetic reflex; **не** заменяет `homeostasis_deltas`.
- Наблюдаемая команда (`command_before`) → **только** Command buffer host; **не** дублировать mechanical apply на том же событии (§ 0.6).

### 7.2. Связь с ISIDA

| Поле | Назначение |
|------|------------|
| `homeostasis_deltas` | Явные дельты параметров гомеостаза от среды |
| `reflex_trigger_expression_pattern_id` | ID паттерна `expr:env.*` в Expression channel для Genetic Level3 |

**Не использовать:** `InfluenceActions.dat`, EA-прокси 101+, `ApplyMultipleInfluenceActions` для YAML-триггеров.

### 7.3. События и параметры

Допустимые `event` — в `schema/trigger-detect.json`.

| `event` (канон) | Параметры | Поведение host (contract 3.0) |
|-----------------|-----------|-------------------------------|
| `command_before` | `command_ids: [<int>, …]` | **Только** запись в Command buffer → CommandChannel |
| `document_saved` | — | Mechanical: `homeostasis_deltas` + optional `reflex_trigger_expression_pattern_id` |

Reserved keys триггера: `id`, `display_name`, `event`, `homeostasis_deltas`, `reflex_trigger_expression_pattern_id`.

### 7.4. Пример (эталон contract 3.0)

```yaml
# Триггеры среды: mechanical path + expr:env.* (не influence_action_id).
triggers:
  - id: save_active_document
    display_name: "Сохранение активного документа"
    event: document_saved
    homeostasis_deltas:
      - parameter_id: 3
        delta: 1.0
    reflex_trigger_expression_pattern_id: 41001
  - id: before_rebuild
    display_name: "Перед Rebuild (наблюдаемая команда)"
    event: command_before
    command_ids: [57603]
```

---

## 8. Runtime closure (каталог `runtime\`)

### 8.1. Требование

В `runtime\` — полный closure host, включая:

- основную DLL host;
- **`isida.dll`** (обязательно);
- реализацию **`IHostMotorDispatcher`**;
- interop целевой среды.

### 8.2. Чеклист

| Категория | Примеры |
|-----------|---------|
| Host | `velum.dll`, `my-host.dll` |
| ISIDA | `isida.dll` |
| Contract | `SymbiontEnv.Contract.dll` |

---

## 9. `manifest.json`

### 9.1. Обязательные поля (contract 3.0)

```json
{
  "id": "my-adapter",
  "displayName": "Мой адаптер",
  "version": "1.0.0",
  "contractVersion": "3.0",
  "author": "Example Author",
  "bootDataRelativePath": "BootData"
}
```

| Поле | Правила |
|------|---------|
| `contractVersion` | `"3.0"` для этого документа |

### 9.2. Опциональные поля

| Поле | Назначение |
|------|------------|
| `schemaVersion` | Версия JSON в `schema\` (для contract 3.0: `"3.0"`) |
| `architectureSpecVersion` | Ссылка на Spec (рекомендуется: `"2.2"`) |

### 9.3. Пример

```json
{
  "id": "velum-solidworks",
  "displayName": "Velum — SolidWorks",
  "version": "2.0.0",
  "contractVersion": "3.0",
  "schemaVersion": "3.0",
  "architectureSpecVersion": "2.2",
  "author": "VELUM",
  "bootDataRelativePath": "BootData",
  "description": "Reactive Core: expression_pattern_id, mechanical triggers"
}
```

---

## 10. UI schema (`schema\`)

### 10.1. Имена файлов (schemaVersion 3.0)

| Файл | Содержание |
|------|------------|
| `handlers-catalog.json` | Handler'ы шагов `invoke` |
| `trigger-detect.json` | Допустимые `event` |
| `trigger-catalog.json` | Каталог `id` триггеров |
| `recipe-catalog.json` | Каталог `id` рецептов |
| `expression-pattern-catalog.json` | **Новый:** `expr:velum.*` / `expr:env.*` — id, token, label для редакторов |
| `recipe-template-catalog.json` | **Опционально:** маски `{PLACEHOLDER}` и имена свойств для редактора шагов рецепта |
| `metric-probes.json` | ProbeKey в `EnvironmentPressureRules.dat` |

### 10.2. `expression-pattern-catalog.json` (фрагмент)

```json
{
  "schemaVersion": "3.0",
  "patterns": [
    {
      "id": 42001,
      "token": "expr:velum.recipe.kb_apply_on_save",
      "label": "Рецепт: КБ при Save",
      "kind": "velum_recipe"
    },
    {
      "id": 41001,
      "token": "expr:env.document_saved",
      "label": "Событие: документ сохранён",
      "kind": "env_trigger"
    }
  ]
}
```

Редактор рецепта: combobox / picker по `expression_pattern_id` из каталога и boot проекта.

### 10.3. `handlers-catalog.json`, `trigger-detect.json`

Формат без изменений принципа (см. contract 2.0); `schemaVersion`: `"3.0"`.

В `argsSchema[]` опционально поле **`editorHint`** — подсказка UI студии (не влияет на runtime):

| `editorHint` | Редактор в `SchemaActionPanel` |
|--------------|--------------------------------|
| `template_placeholder` | Кнопка «Вставить…» — справочник из `recipe-template-catalog.json` → `placeholders[]` |
| `property_name` | Кнопка «Справочник…» — `recipe-template-catalog.json` → `propertyNames[]` |

Если `editorHint` не задан, студия использует эвристику по ключу: `template` → маски, `name` → имена свойств.

Пример фрагмента `handlers-catalog.json`:

```json
{
  "schemaVersion": "3.0",
  "handlers": [
    {
      "id": "set_custom_property",
      "argsSchema": [
        { "key": "name", "label": "Имя свойства", "type": "string", "required": true, "editorHint": "property_name" },
        { "key": "template", "label": "Шаблон значения", "type": "string", "editorHint": "template_placeholder" }
      ]
    }
  ]
}
```

### 10.4. `recipe-template-catalog.json`

**Назначение:** машиночитаемый справочник для редактора шагов рецепта (кнопки «Вставить…» / «Справочник…»). Студия читает файл из `schema\`; **runtime host не обязан** загружать его — подстановка в SW выполняется кодом host (например `VelumRecipeTemplateResolver` в Velum).

**Не путать:** каталог schema описывает *допустимые маски в UI*; runtime строит *контекст значений* из сессии и документа и разрешает шаблон по синтаксису `{NAME}` / `{SEQ:n}`.

```json
{
  "schemaVersion": "3.0",
  "placeholders": [
    {
      "token": "{PROJECT}",
      "label": "Каталог проекта",
      "description": "Имя родительского каталога пути документа"
    },
    {
      "token": "{SEQ:4}",
      "label": "Порядковый номер (4 цифры)",
      "description": "SEQ с ведущими нулями, ширина n в фигурных скобках"
    }
  ],
  "propertyNames": [
    {
      "name": "Обозначение",
      "label": "Обозначение",
      "description": "Пользовательское свойство документа КБ"
    }
  ]
}
```

Составной шаблон в YAML: пользователь вставляет маски из справочника и вручную добавляет разделители (пробел, дефис) в поле «Шаблон значения».

В диалоге «Вставить…» / «Справочник…» студия показывает колонки **Значение** (token или name) и **Описание** (текст из `label` и `description` каталога).

| Поле | Обязательно | Описание |
|------|-----------|----------|
| `placeholders[].token` | да | Строка маски, как в YAML (`{PROJECT}`, `{SEQ:4}`) |
| `placeholders[].label` | нет | Краткая подпись; в UI объединяется с `description` |
| `placeholders[].description` | рекомендуется | Пояснение для пользователя в справочнике студии |
| `propertyNames[].name` | да | Имя свойства для записи в host |
| `propertyNames[].label` | нет | Краткая подпись в справочнике |
| `propertyNames[].description` | рекомендуется | Пояснение назначения свойства в справочнике |

Файл **опционален** для «Проверить»; без него кнопки справочника скрыты или неактивны (пустой список).

### 10.5. ProbeKey и оператор

Студия заполняет combobox **ProbeKey** в `EnvironmentPressureRules.dat` из `probes[].key` (плюс «оператор, не среда»).

**Operator path (Spec v2.2):** дискретные стимулы оператора — каналы Expression/Verbal/Command + **`HomeostasisSignificance`**; **не** `InfluenceActions.dat`. YAML-триггеры — **только** mechanical path (`homeostasis_deltas`).

---

## 11. Согласование парсеров (AIStudio ↔ runtime host)

AIStudio и host **должны** использовать одну `SymbiontEnv.Contract.dll` (codec contract 3.0).

| # | Тест |
|---|------|
| T1 | Read(fixture v3) → Write → Read — эквивалент канону |
| T2 | Host Read после Write студии |
| T3 | Студия Read после Write host |
| T4 | Recipe `expression_pattern_id` ↔ `expression-pattern-catalog.json` |
| T5 | Trigger `homeostasis_deltas` + `reflex_trigger_expression_pattern_id` round-trip |

---

## 12. Валидация «Проверить» (AIStudio)

| # | Проверка | Severity |
|---|----------|----------|
| V1 | `manifest.json` валиден | Error |
| V2 | `contractVersion` = `"3.0"` | Error |
| V3 | `id` валиден | Error |
| V4 | `schema\` с JSON (incl. `expression-pattern-catalog.json`) | Error |
| V5 | Структура schema | Error |
| V6 | YAML парсится codec v3 | Warning |
| V7 | `expression_pattern_id` / `reflex_trigger_expression_pattern_id` в каталоге boot | Warning |
| V8 | Legacy ключи `adaptive_action_id`, `influence_action_id` в YAML | **Error** (contract 3.0) |

---

## 13. Разделение уровней данных

| Уровень | Владелец | Формат |
|---------|----------|--------|
| Гомеостаз, рефлексы, каналы, `BaseExternalActions` | ISIDA / AIStudio | `.dat` (boot v2, Spec §8) |
| Каркас рецепта (`id`, `expression_pattern_id`, `steps`) | Contract 3.0 | YAML |
| Каркас триггера (`event`, `homeostasis_deltas`, `reflex_trigger_expression_pattern_id`) | Contract 3.0 | YAML |
| Operator stimulus | ISIDA | PerceptionImage + `HomeostasisSignificance` |
| Исполнение рецепта | Host | `IHostMotorDispatcher` → COM/API |

---

## 14. `adapter-settings` (опционально)

Без изменений принципа (пути к YAML на машине host). Ключи `EnvironmentRecipesFilePath`, `EnvironmentTriggersFilePath` и т.д.

---

## 15. Чеклист соответствия contract 3.0

**Автор host:**

- [ ] `isida.dll`, `SymbiontEnv.Contract.dll`, `IHostMotorDispatcher`
- [ ] YAML v3 через `EnvironmentYamlCodec`
- [ ] Mechanical triggers → ledger `EnvironmentMechanical`; без `InfluenceActionId`
- [ ] Double dispatch исправлен (Command **или** mechanical)

**Автор пакета для AIStudio:**

- [ ] `contractVersion: "3.0"`; `architectureSpecVersion: "2.2"` (рекомендуется)
- [ ] `schema\` incl. `expression-pattern-catalog.json`
- [ ] BootData YAML **без** legacy ключей
- [ ] «Проверить» без Error

**Разработчик AIStudio:**

- [ ] Редакторы «Среда»: `expression_pattern_id`, `homeostasis_deltas`, `reflex_trigger_expression_pattern_id`
- [ ] Отклонение YAML с `adaptive_action_id` / `influence_action_id`
- [ ] `AdapterId` в `AgentProperties.dat`

**Разработчик runtime host:**

- [ ] `RecipeCatalog.FindByExpressionPatternId`
- [ ] `SwEventDetector` v3 (mechanical deltas, expr:env trigger)
- [ ] Не вызывать `ApplyMultipleInfluenceActions` для YAML-триггеров

---

## 16. Миграция contract 2.0 → 3.0

Одноразовая утилита студии (не dual-read в production codec):

| Было (2.0) | Стало (3.0) |
|------------|-------------|
| `adaptive_action_id` | `expression_pattern_id` (маппинг boot-таблицы) |
| `influence_action_id` | `homeostasis_deltas` + `reflex_trigger_expression_pattern_id` |
| `recommended_trigger_influence_ids` | `recommended_trigger_keys` |

Поломка тестовых сценариев и boot v1 **допустима** (Spec v2.2).

---

## 17. История версий документа

| Версия | Дата | Изменения |
|--------|------|-----------|
| 3.0 | 2026-06-13 | Синхронизация со Spec v2.2: `expression_pattern_id`, `homeostasis_deltas`, `reflex_trigger_expression_pattern_id`; удалены `adaptive_action_id`, `influence_action_id`; `expression-pattern-catalog.json`; operator/mechanical path §0.6; `IHostMotorDispatcher`; миграция §16 |
| 3.0.1 | 2026-06-15 | `recipe-template-catalog.json` (маски шаблона и имена свойств); `editorHint` в `argsSchema` |
| 2.0 | 2026-06-09 | `invoke`/`comment`; `event`; schema 2.0 (**устарел**) |
| 1.0 | 2026-06-03 | Первый release (**устарел**) |

---

*Контракт 3.0 соответствует архитектуре [`SymbiontArchitecture_OperatorEnvironment_Spec.md`](../../../../VELUM/Velum/docs/SymbiontArchitecture_OperatorEnvironment_Spec.md) v2.2 и плану [`Velum_RecipeReflexEditor_ImplementationPlan.md`](../../../../VELUM/Velum/docs/Velum_RecipeReflexEditor_ImplementationPlan.md) v2.2. Изменения codec или schema без обновления этого документа не допускаются.*
