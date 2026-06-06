# Контракт платформы адаптеров среды (AIStudio)

**Версия контракта:** `1.0`  
**Дата:** 2026-06-06  
**Статус:** нормативный документ для AIStudio, авторов адаптеров и runtime host (эталонная реализация v1 — ориентир для авторов). AIStudio **не обязательна** для разработки host; пакет с `manifest.json` нужен только при интеграции со студией (§ 0.5).

Связанные документы:

| Документ | Назначение |
|----------|------------|
| [`AdapterAuthorGuide.md`](AdapterAuthorGuide.md) | Практика сборки и регистрации пакета |
| [`AdapterPlatform_ImplementationPlan.md`](AdapterPlatform_ImplementationPlan.md) | План реализации в студии |

---

## 0. Термины и роли

Этот раздел фиксирует **три разные сущности**, которые не следует смешивать в UI и документации.

### 0.1. Проект симбионта

**Корневой каталог данных одного симбионта** — то, что в меню «Проект → Создать / Открыть проект симбионта».

```
{projectRoot}\
  Settings\Settings.xml     — профиль проекта (пути Data, BootData)
  Data\Gomeostas\AgentProperties.dat — свойства симбионта, в т.ч. AdapterId
  Data\                     — ISIDA (.dat): гомеостаз, рефлексы, действия…
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
  schema\                   — описание capability (post-MVP: подсказки в редакторах)
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

Студия **не копирует** каркас при работе: каталог появляется **при установке AIStudio**. Исходник каркаса — `docs/adapter-package-example/demo/`. Автор копирует каталог, меняет `id`, заполняет schema и runtime.

### 0.3. Runtime host (исполнитель среды)

**`runtime\*.dll`** — программа, которая **вне студии** читает YAML проекта и исполняет рецепты/триггеры (эталонная реализация v1 — ориентир).

| Где нужен runtime | Когда |
|-------------------|--------|
| В пакете адаптера | Дистрибуция; проверка «Проверить» (наличие файлов, в т.ч. `isida.dll`) |
| На машине пользователя | Работа в реальной среде (целевое приложение и т.д.) |
| В студии | **Не загружается** |

### 0.4. Связь проекта симбионта и пакета

Поле **`AdapterId`** в `Data\Gomeostas\AgentProperties.dat` (свойства симбионта) — **не подключение к DLL**, а метка:

> «Для этого симбионта используем описание среды из зарегистрированного пакета `{id}`».

От `AdapterId` зависят:

- доступность редакторов меню «Среда»;
- seed BootData при **создании** проекта (если тип среды выбран);
- (post-MVP) combobox и подсказки из `schema\` — в т.ч. **`EnvironmentMetricProbeKey`** в таблице воздействий EA.

### 0.5. Когда нужен пакет для студии

Ядро платформы для host — **`isida.dll`** и runtime contract YAML (§ 5–7). Формат **пакета** (`manifest.json`, `Adapters\{id}\`, BootData, schema) обязателен **только** если используется AIStudio.

| Компонент | Только host (без студии) | С AIStudio |
|-----------|--------------------------|------------|
| `isida.dll` | **Обязательно** (ссылка в host) | **Обязательно** в `runtime\` |
| `manifest.json`, `Adapters\{id}\` | Не нужны | Нужны для регистрации |
| `BootData\Environment\*.yaml` в пакете | Не нужны | Нужны (seed проекта) |
| `schema\` | Не нужна | Рекомендуется (редакторы «Среда») |

Автор host может использовать любой редактор данных симбионта; контракт YAML и `SymbiontEnv.Contract.dll` доступны без установки AIStudio.

### 0.6. Роли пользователей

| Роль | Типичный поток |
|------|----------------|
| **Настройщик симбионта** | Получить пакет от автора → **зарегистрировать** в студии → создать/открыть проект → настроить данные (гомеостаз, рецепты/триггеры) |
| **Автор адаптера (только host)** | Host + `isida.dll` → YAML по контракту → свой деплой у конечника |
| **Автор адаптера + студия** | Собрать пакет → **зарегистрировать** в студии → тестовый проект с `AdapterId` → рецепты/schema |

### 0.7. Запрещённые формулировки в UI

Избегать: «подключить адаптер», «адаптер активен», «запустить адаптер», «установить адаптер» (путается с установкой программы в ОС).

Использовать: **«зарегистрировать пакет»**, **«тип среды»**, **«описание пакета `{id}`»**.

---

## 1. Область действия

### 1.1. Что регулирует контракт

1. **Структура пакета адаптера** — каталог, который регистрируется в `%ProgramData%\ISIDA\Adapters\{id}\`.
2. **`manifest.json`** — метаданные адаптера и ссылки на части пакета.
3. **Runtime contract YAML** — формат файлов `EnvironmentRecipes.yaml` и `EnvironmentTriggers.yaml`, которые:
   - редактирует AIStudio;
   - читает runtime host;
   - копируются из пакета адаптера в проект симбионта при создании проекта.
4. **Нормализация** — канонические ключи при записи, допустимые алиасы при чтении, правила round-trip.
5. **UI schema** (JSON в `schema\`) — описание полей редакторов «Среда» (combobox типов шагов, preconditions, detect); загрузка через `AdapterSchemaLoader` в AIStudio.

### 1.2. Что контракт не регулирует

- Форматы ISIDA (`.dat`: G_AD, EA, GeneticReflexes, гомеостаз) — зона ответственности `isida.dll` и редакторов студии (если используется AIStudio).
- Реализацию API конкретной среды (CAD, офисные пакеты, IDE и т.д.) — только в runtime host.
- Загрузку DLL адаптера в процесс AIStudio — **запрещена**; runtime исполняется вне студии.
- Деплой и дистрибуцию runtime host на машине пользователя.

### 1.3. Роли

| Роль | Ответственность |
|------|-----------------|
| **AIStudio** | Редактирование данных симбионта; регистрация пакетов; валидация «Проверить»; запись YAML по runtime contract |
| **Пакет адаптера** | Для студии: manifest, BootData-образцы, runtime DLL (**вкл. `isida.dll`**), schema (рекомендуется) |
| **Runtime host** | Чтение YAML проекта; исполнение шагов рецептов; детекция триггеров |
| **Проект симбионта** | `AgentProperties.dat` с `AdapterId`; рабочие `BootData\Environment\*.yaml` и `Data\` |

---

## 2. Версионирование

### 2.1. `contractVersion` в manifest

| Значение | Поддержка AIStudio 1.x |
|----------|------------------------|
| `1.0` | Да (текущая) |

- Студия **отклоняет** пакет с неизвестной major-версией контракта.
- Minor-расширения контракта (новые опциональные ключи YAML при сохранении обратной совместимости) оформляются ревизией этого документа без смены major, если все существующие runtime v1 продолжают читать старые файлы.

### 2.2. `version` в manifest

Версия **пакета адаптера** (SemVer рекомендуется: `1.0.0`). Не путать с `contractVersion`.

### 2.3. Политика изменений

| Изменение | Действие |
|-----------|----------|
| Новый обязательный ключ в YAML | Новая major `contractVersion` + обновление runtime |
| Новый опциональный ключ | Документировать в § YAML; runtime игнорирует неизвестные ключи |
| Новый `step.type` или `detect.kind` | Поддержка в runtime адаптера + запись в schema; контракт YAML остаётся extensible (type/kind — строки) |
| Смена канона записи | Обновить § 5 + dual round-trip тесты |

---

## 3. Регистрация и пути

### 3.1. Каталог адаптеров

```
%ProgramData%\ISIDA\Adapters\{adapterId}\
```

- `adapterId` совпадает с полем `id` в `manifest.json`.
- Допустимые символы `id`: `[a-z0-9_-]+` (регистр сохраняется; рекомендуется lowercase).
- Повторная установка того же `id` — полная замена каталога (с подтверждением в UI).

### 3.2. Проект симбионта

```
{projectRoot}\
  Settings\Settings.xml       — профиль проекта (пути)
  Data\Gomeostas\AgentProperties.dat — свойства симбионта, в т.ч. AdapterId
  BootData\Environment\
    EnvironmentRecipes.yaml
    EnvironmentTriggers.yaml
  Data\                       — ISIDA (.dat)
```

При **новом проекте** студия копирует `{Adapters\{adapterId}\BootData\}` → `{projectRoot}\BootData\` (файлы не перезаписываются, если уже существуют — политика seed).

### 3.3. AdapterId в свойствах симбионта (MVP)

```
AdapterId|my-adapter
```

(строка в `Data\Gomeostas\AgentProperties.dat`, формат `Ключ|Значение`)

- Задаётся при создании проекта или в UI «Свойства симбионта»; должен совпадать с зарегистрированным пакетом в `Adapters\{id}\`.
- Пустой или отсутствующий `AdapterId` → редакторы меню «Среда» заблокированы; **гомеостаз и пульт оператора доступны**.
- `AdapterId` **не обязателен** при создании проекта симбионта (проект «только гомеостаз»).
- Legacy `<AdapterId>` в `Settings.xml` при открытии проекта переносится в `AgentProperties.dat` и удаляется из Settings.

---

## 4. Структура пакета адаптера

### 4.1. MVP (минимум для «Проверить» и нового проекта)

```
{packageRoot}\
  manifest.json                         ОБЯЗАТЕЛЬНО
  BootData\
    Environment\
      EnvironmentRecipes.yaml             ОБЯЗАТЕЛЬНО (допускается recipes: [])
      EnvironmentTriggers.yaml          ОБЯЗАТЕЛЬНО (допускается triggers: [])
  runtime\                              ОБЯЗАТЕЛЬНО для адаптеров с DLL-host
    *.dll                               (полный closure — § 8)
  schema\                               РЕКОМЕНДУЕТСЯ (редакторы «Среда»)
  adapter-settings\                     ОПЦИОНАЛЬНО
```

Пакет **для регистрации в AIStudio** должен содержать manifest, BootData, непустой `runtime\` с **`isida.dll`**. Без студии достаточно host + `isida.dll` (§ 0.5).

### 4.2. Расширенный пакет (рекомендации)

Дополнительно к § 4.1:

- `schema\` — все файлы UI schema (§ 10); для полноценных редакторов «Среда».
- `adapter-settings\Settings.xml` — шаблон настроек host на целевой машине.

### 4.3. Имена файлов BootData

| Файл | Строго |
|------|--------|
| Рецепты | `BootData\Environment\EnvironmentRecipes.yaml` |
| Триггеры | `BootData\Environment\EnvironmentTriggers.yaml` |

Другие имена (`UserTriggers.yaml`, `Recipes\*.yaml`) — **legacy** вне контракта v1; миграция на усмотрение host.

---

## 5. Нормализация YAML (round-trip)

### 5.1. Принцип

- **На диске (канон записи)** — ключи и значения из столбца «Канон».
- **При чтении** — допускаются алиасы; парсеры приводят к внутренней модели.
- **Dual round-trip** (обязательный gate): эталонный файл → Read → Write → Read; результат эквивалентен канону для обоих стеков:
  - AIStudio: `EnvironmentYamlCodec`;
  - Runtime host: `HostRecipeYamlReader` / `HostTriggersYamlReader` → `EnvironmentYamlWriter`.

### 5.2. Таблица алиасов

| Область | Канон (запись) | Алиасы (только чтение) |
|---------|----------------|------------------------|
| ID рецепта | `id` | `recipe_id` |
| ID триггера | `id` | `trigger_key` |
| Типы документа в preconditions | `document_kinds` | `document_types` |
| Фильтр документа триггера | `document_kinds` | `document_filter` |
| Тип шага set property | `set_property` | `set_custom_property` |
| Detect: команда до выполнения | `command_before` | `command_pre` |
| Detect: сохранение документа | `document_saved` | `file_save_post` |

### 5.3. Внутренняя модель после чтения

Оба парсера (AIStudio и runtime host) нормализуют тип шага свойства:

```
set_property  →  set_custom_property   (в памяти)
set_custom_property  →  set_custom_property
```

При записи на диск оба writer эмитируют **`set_property`**.

Detect `kind` в памяти может оставаться как в файле; runtime host принимает канон и алиасы (см. `HostEventDetector`).

### 5.4. Пустые коллекции

Writers студии и runtime host для пустых списков эмитируют явно:

```yaml
document_kinds: []
recommended_trigger_influence_ids: []
command_ids: []
```

Отсутствие ключа и `[]` при чтении трактуются как пустой список (для `document_kinds` — «без фильтра по типу» в runtime host).

### 5.5. Устаревший формат файла рецептов

Допускается **один рецепт в корне документа** (без ключа `recipes:`) — только для чтения. Writers всегда создают:

```yaml
recipes:
  - id: ...
```

### 5.6. Кодировка и синтаксис

- UTF-8 без BOM.
- Подмножество YAML 1.1: maps, sequences, scalars; block sequences для `recipes`, `triggers`, `steps`, `detect`.
- Комментарии `#` допускаются; writers могут добавлять заголовочный комментарий.

---

## 6. Runtime contract: `EnvironmentRecipes.yaml`

### 6.1. Корневая структура

```yaml
recipes:
  - id: <string>                    # обязательно, уникален в файле
    adaptive_action_id: <int>       # обязательно, > 0, ISIDA G_AD
    display_name: <string>          # опционально
    description: <string>           # опционально
    risk_tier: A|B|C                # опционально; default B при неизвестном
    reactive_eligible: <bool>       # опционально; default true
    recommended_trigger_influence_ids: [<int>, ...]  # опционально, справочно
    preconditions: { ... }          # опционально
    steps: [ ... ]                  # опционально
    postcondition_log: <string>     # опционально
    test_notes: <string>            # опционально
```

### 6.2. Связь с ISIDA

| Поле | Справочник ISIDA | Назначение |
|------|------------------|------------|
| `adaptive_action_id` | Adaptive Actions (G_AD) | Рецепт исполняется, когда активно это G_AD |
| `recommended_trigger_influence_ids` | Influence Actions (EA) | Рекомендация редактору; **не** исполняется runtime из YAML |

Runtime **не** проверяет существование ID в `.dat` при чтении YAML; валидация — задача студии и сценариев тестирования.

### 6.3. Блок `preconditions`

Контракт v1 — фиксированный набор ключей:

| Ключ | Тип | Default | Описание |
|------|-----|---------|----------|
| `document_kinds` | `[document \| project \| view]` | `[]` | Допустимые типы активного документа |
| `not_edit_mode` | bool | `false` | Запрет исполнения в режиме редактирования |
| `not_read_only` | bool | `false` | Документ не read-only |
| `checkout_required` | bool | `false` | Требуется извлечение из хранилища |

Другие ключи в `preconditions` runtime host v1 **игнорирует**. Адаптер другой среды может определять собственные ключи через schema (post-MVP); для contract 1.0 расширение preconditions — только через новую версию контракта или согласованный профиль адаптера в schema.

### 6.4. Блок `steps`

Каждый элемент:

```yaml
- type: <string>
  <param_key>: <scalar>
```

Все ключи кроме `type` — параметры шага (строковые scalars). Регистр ключей параметров **не** значим при чтении.

#### 6.4.1. Типы шагов contract v1 (эталонный набор)

| `type` (в файле) | Внутренний type | Параметры | Примечание |
|------------------|-----------------|-----------|------------|
| `set_property` | `set_custom_property` | см. ниже | Запись пользовательского свойства документа |
| `run_command` | `run_command` | `command_id` (int, обяз.) | Выполнение команды host по ID |
| `refresh` | `refresh` | — | Обновление активного документа |
| `log` | `log` | `message`, `level` (`info`\|`warn`\|`error`, default `info`) | Запись в лог host |

**`set_property` / `set_custom_property` — параметры:**

| Параметр | Обяз. | Описание |
|----------|-------|----------|
| `name` | да | Имя свойства |
| `template` | нет | Шаблон значения (`{PROJECT}`, `{DESCRIPTION}`, …) |
| `config` | нет | `document` / `active` — контекст документа |
| `overwrite` | нет | `if_empty`, `never_if_filled`, … |

Неизвестный `type` → runtime host: шаг с ошибкой `unknown_step_type`, рецепт прерывается (политика host).

#### 6.4.2. Расширение типов шагов

Новые `type` добавляет **автор адаптера** в schema и runtime. Контракт 1.0 не фиксирует closed enum на уровне YAML — студия в MVP может показывать фиксированный список эталонного набора; post-MVP — список из `schema/recipe-steps.json`.

### 6.5. `risk_tier`

| Значение | Смысл (политика проекта) |
|----------|--------------------------|
| `A` | Низкий риск, без подтверждения |
| `B` | Стандарт |
| `C` | Высокий риск / бэклог подтверждений |

Неизвестное значение → `Unknown` / трактовка как `B` при записи.

### 6.6. Пример (эталон)

```yaml
# Рецепты среды (исполняемая моторика; привязка к G_AD — adaptive_action_id).
recipes:
  - id: kb_name_on_save
    display_name: "Наименование по КБ после сохранения"
    description: "Заполнить Обозначение и Наименование по шаблону политики КБ"
    adaptive_action_id: 37
    risk_tier: B
    reactive_eligible: true
    recommended_trigger_influence_ids: [101]

    preconditions:
      document_kinds: [document, project]
      not_edit_mode: true
      not_read_only: true
      checkout_required: false

    steps:
      - type: set_property
        config: document
        name: "Обозначение"
        template: "{PROJECT}-{DISCIPLINE}-{SEQ:4}"
        overwrite: if_empty
      - type: set_property
        config: document
        name: "Наименование"
        template: "{DESCRIPTION}"
        overwrite: if_empty

    postcondition_log: properties_updated
    test_notes: "Открыть документ → сохранить → проверить свойства и лог"
```

---

## 7. Runtime contract: `EnvironmentTriggers.yaml`

### 7.1. Корневая структура

```yaml
triggers:
  - id: <string>                    # обязательно, уникален в файле
    display_name: <string>          # опционально
    influence_action_id: <int>      # обязательно, > 0, ISIDA EA (прокси)
    document_kinds: [...]           # опционально; см. § 6.3
    detect:
      - kind: <string>
        enabled: <bool>             # опционально, default true
        environment: <string>       # опционально, напр. my-adapter
        command_ids: [<int>, ...]   # для command_before
```

Секция `triggers:` **обязательна** (допускается пустой список). Файл без `triggers:` — ошибка валидации.

### 7.2. Связь с ISIDA

| Поле | Справочник | Назначение |
|------|------------|------------|
| `influence_action_id` | Influence Actions (EA) | При срабатывании detect → `ApplyMultipleInfluenceActions` |

### 7.3. Правила `detect` (contract v1)

| `kind` (канон) | Алиас | Параметры | Поведение runtime host |
|----------------|-------|-----------|------------------------|
| `command_before` | `command_pre` | `command_ids`, `environment` | Перед выполнением команды; match по ID |
| `document_saved` | `file_save_post` | `environment`, `enabled` | После сохранения документа |

- `environment: my-adapter` — фильтр host; опционален, но рекомендуется для мультисредовых файлов.
- Пустой `command_ids` — правило зарегистрировано, ID заполняются калибровкой в среде.

### 7.4. Пример (эталон)

```yaml
# Триггеры среды: событие → influence_action_id.
triggers:
  - id: save_active_document
    display_name: "Сохранение активного документа"
    influence_action_id: 101
    document_kinds: [document, project]
    detect:
      - kind: command_before
        environment: my-adapter
        command_ids: []
      - kind: document_saved
        environment: my-adapter
        enabled: true
```

---

## 8. Runtime closure (каталог `runtime\`)

### 8.1. Требование

В `runtime\` должны находиться **все** сборки, без которых host не стартует на чистой машине пользователя, включая:

- основную DLL host (например `my-host.dll`);
- **`isida.dll`** (обязательно) и транзитивные зависимости;
- interop/bindings целевой среды;
- зависимости UI host и прочие из `bin\Debug` / Release host-проекта.

### 8.2. Чеклист для адаптера (ориентир)

| Категория | Примеры |
|-----------|---------|
| Host | `my-host.dll` |
| ISIDA | `isida.dll` |
| Interop | `*.Interop.*.dll`, bindings целевого API |
| UI host | зависимости UI-фреймворка host |

Точный список фиксируется в README пакета адаптера. Пример среды — `docs\adapter-package-example\velum\` (конкретная реализация, не часть contract). «Проверить» в студии: `runtime\` не пуст и содержит **`isida.dll`** (Error). «Создать пакет…» дополняет `isida.dll` из каталога AIStudio, если host не положил его в `bin\Debug`.

### 8.3. Исключения

**Не** включать в пакет: `.cs`, `.csproj`, `.pdb` (опционально), исходники, `.git`, артефакты AIStudio.

---

## 9. `manifest.json`

### 9.1. Обязательные поля (contract 1.0)

```json
{
  "id": "my-adapter",
  "displayName": "Мой адаптер",
  "version": "1.0.0",
  "contractVersion": "1.0",
  "author": "Example Author",
  "bootDataRelativePath": "BootData"
}
```

| Поле | Тип | Правила |
|------|-----|---------|
| `id` | string | `[a-z0-9_-]+`, уникален среди установленных |
| `displayName` | string | Для UI студии |
| `version` | string | Версия пакета |
| `contractVersion` | string | `"1.0"` для этого документа |
| `author` | string | |
| `bootDataRelativePath` | string | Относительно корня пакета; обычно `"BootData"` |

### 9.2. Опциональные поля

| Поле | Назначение |
|------|------------|
| `schemaVersion` | Версия набора JSON в `schema\` (напр. `"1.0"`) |
| `adapterSettingsRelativePath` | `"adapter-settings"` |
| `description` | Краткое описание |
| `supportedStudioVersions` | Ограничение версий AIStudio (post-MVP) |

### 9.3. Пример (полный)

```json
{
  "id": "my-adapter",
  "displayName": "Мой адаптер — Reactive Core",
  "version": "1.0.0",
  "contractVersion": "1.0",
  "schemaVersion": "1.0",
  "author": "Example Author",
  "bootDataRelativePath": "BootData",
  "adapterSettingsRelativePath": "adapter-settings",
  "description": "Эталонный адаптер среды для симбионта ISIDA"
}
```

---

## 10. UI schema (`schema\`, post-MVP)

В MVP студия **не обязана** читать schema для редактирования. Файлы schema используются для:

- расширенной «Проверить» (валидный JSON);
- schema-driven редакторов (фаза 3 платформы);
- документирования полей адаптера для автора.

### 10.1. Имена файлов (schemaVersion 1.0)

| Файл | Содержание |
|------|------------|
| `recipe-preconditions.json` | Поля блока `preconditions` |
| `recipe-steps.json` | Допустимые `type` шагов и параметры |
| `trigger-filter.json` | Фильтр контекста триггера (`document_kinds`, …) |
| `trigger-detect.json` | Правила `detect` |
| `metric-probes.json` | Допустимые ключи **`EnvironmentMetricProbeKey`** в таблице воздействий EA (combobox в студии, post-MVP) |

### 10.2. Формат дескriptor поля (фрагмент)

```json
{
  "schemaVersion": "1.0",
  "fields": [
    {
      "key": "document_kinds",
      "label": "Типы документа",
      "type": "stringList",
      "enumValues": ["document", "project", "view"],
      "required": false,
      "default": []
    },
    {
      "key": "not_edit_mode",
      "label": "Не в режиме редактирования",
      "type": "bool",
      "required": false,
      "default": false
    }
  ]
}
```

### 10.3. Формат дескriptor шага (фрагмент)

```json
{
  "schemaVersion": "1.0",
  "stepTypes": [
    {
      "type": "set_property",
      "label": "Задать свойство документа",
      "runtimeType": "set_custom_property",
      "parameters": [
        { "key": "name", "label": "Имя", "type": "string", "required": true },
        { "key": "template", "label": "Шаблон", "type": "string" },
        {
          "key": "overwrite",
          "label": "Перезапись",
          "type": "enum",
          "values": ["if_empty", "never_if_filled"]
        }
      ]
    },
    {
      "type": "run_command",
      "label": "Выполнить команду",
      "parameters": [
        { "key": "command_id", "label": "Command ID", "type": "int", "required": true }
      ]
    }
  ]
}
```

**Правило:** каждый `key` / `type` в schema должен поддерживаться runtime адаптера. Студия не добавляет поля, отсутствующие в schema (post-MVP).

### 10.4. Формат `metric-probes.json` (фрагмент)

```json
{
  "schemaVersion": "1.0",
  "probes": [
    {
      "key": "host.active_doc_modified",
      "label": "Документ изменён",
      "description": "Сэмплер host: активный документ имеет несохранённые изменения"
    }
  ]
}
```

Студия (post-MVP) заполняет combobox колонки `EnvironmentMetricProbeKey` в `InfluenceActions.dat` из `probes[].key`; пустое значение — воздействие **оператора** (пульт), не среды.

---

## 11. Согласование парсеров (AIStudio ↔ runtime host)

Общая библиотека codec **не входит** в contract 1.0. Согласование — **тестами**:

| # | Тест |
|---|------|
| T1 | AIStudio: Read(fixture) → Write(temp) → Read(temp) ≡ канон |
| T2 | Runtime host: Read(fixture) → Write(temp) → Read(temp) ≡ канон |
| T3 | AIStudio Write после host Read — host Read без ошибок |
| T4 | Host Write после AIStudio Read — AIStudio Read без ошибок |

**Расположение фикстур:** `AIStudio\tests\EnvironmentYaml\fixtures\` (3–5 файлов, включая пустые каталоги, полный рецепт, legacy `recipe_id` + `document_types`, алиасы detect).

**CI:** job при изменении `AdapterContract.md`, `AdapterContract.html`, `EnvironmentYamlCodec*`, `HostRecipeYamlReader`, `EnvironmentYamlWriter`.

---

## 12. Валидация «Проверить» (AIStudio, MVP)

| # | Проверка | Severity |
|---|----------|----------|
| V1 | `manifest.json` существует, JSON валиден | Error |
| V2 | `contractVersion` поддерживается | Error |
| V3 | `id` валиден | Error |
| V4 | `BootData\Environment\EnvironmentRecipes.yaml` парсится codec студии | Error |
| V5 | `BootData\Environment\EnvironmentTriggers.yaml` парсится codec студии | Error |
| V6 | `runtime\` содержит ≥1 `.dll`, в т.ч. **`isida.dll`** | Error |
| V7 | Файлы `schema\*.json` парсятся (если каталог есть) | Warning |
| V8 | Ключи sample YAML ⊆ schema (если schema есть) | Warning (post-MVP → Error) |

Студия **не загружает** DLL из `runtime\`.

---

## 13. Разделение уровней данных

| Уровень | Владелец | Формат |
|---------|----------|--------|
| Гомеостаз, рефлексы, сценарии | ISIDA / AIStudio | `.dat`, сценарии |
| Каркас рецепта/триггера (`id`, `adaptive_action_id`, `steps`, `detect`) | Contract 1.0 | YAML |
| Семантика preconditions/detect/step params | Runtime адаптера | YAML + schema |
| Исполнение | Host (`my-host.dll`, …) | — |

---

## 14. `adapter-settings` (опционально)

Шаблон `adapter-settings\Settings.xml` — настройки **host на машине пользователя** после установки (не профиль проекта симбионта).

Для типичных адаптеров ориентир по ключам (не часть YAML contract):

| Ключ | Назначение |
|------|------------|
| `BootDataFolderPath` | Корень BootData на целевой машине |
| `EnvironmentRecipesFilePath` | Путь к `EnvironmentRecipes.yaml` |
| `EnvironmentTriggersFilePath` | Путь к `EnvironmentTriggers.yaml` |
| `DataActionsFolderPath` | Каталог ISIDA Actions |

Пути часто задают через `%ProgramData%\...`.

---

## 15. Чеклист соответствия contract 1.0

**Автор host (минимум, без студии):**

- [ ] Host ссылается на `isida.dll`; YAML по § 5–7
- [ ] Runtime host читает/пишет канонический YAML

**Автор пакета для AIStudio:**

- [ ] `manifest.json` с `contractVersion: "1.0"` и валидным `id`
- [ ] BootData с обоими YAML; секции `recipes:` / `triggers:` присутствуют
- [ ] Sample YAML проходит runtime host
- [ ] `runtime\` — полный closure, **`isida.dll` обязателен**
- [ ] (Рекомендуется) `schema\` согласована с runtime

**Разработчик AIStudio:**

- [ ] Dual round-trip T1–T4 зелёный на фикстурах
- [ ] «Проверить» реализует § 12
- [ ] Новый проект копирует BootData из `Adapters\{id}`
- [ ] `AdapterId` в AgentProperties.dat; блок «Среда» без адаптера

**Разработчик runtime host (эталонная реализация):**

- [ ] Reader принимает все алиасы § 5.2
- [ ] Writer эмитирует канон § 5.2
- [ ] Executor поддерживает step types § 6.4.1
- [ ] Detector поддерживает detect kinds § 7.3

---

## 16. История версий документа

| Версия | Дата | Изменения |
|--------|------|-----------|
| 1.0 | 2026-06-03 | Первый нормативный release: manifest, YAML v1, нормализация, MVP vs post-MVP schema |
| 1.0-rev.4 | 2026-06-06 | Убраны упоминания установщиков и ISS из контракта (§ 0.6, бывш. § 14) |
| 1.0-rev.3 | 2026-06-06 | Настройщик симбионта не собирает установщик; студия не создаёт ISS в проекте |
| 1.0-rev.2 | 2026-06-06 | Нейтральные обозначения вместо привязки к конкретной среде; синхронизация с `schema\` |
| 1.0-rev.1 | 2026-06-06 | § 0.5 два пути (host / студия); установщик вне студии (§ 14); `isida.dll` обязателен в `runtime\` (V6) |
| 1.0-rev.2 | 2026-06-06 | Нейтральные обозначения вместо привязки к конкретной среде; синхронизация с `schema\` |

---

*Контракт 1.0 соответствует эталонной реализации runtime host v1 и codec AIStudio `EnvironmentYamlCodec`. Изменения runtime или codec без обновления этого документа и тестов § 11 не допускаются.*
