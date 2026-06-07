# Контракт платформы адаптеров среды (AIStudio)

**Версия контракта:** `2.0`  
**Дата:** 2026-06-07  
**Статус:** нормативный документ для AIStudio, авторов адаптеров и runtime host. AIStudio **не обязательна** для разработки host; пакет с `manifest.json` нужен только при интеграции со студией (§ 0.5). Кодек YAML и проверка пакета — **`SymbiontEnv.Contract.dll`** (общая библиотека студии и host).

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

Студия **не копирует** каркас при работе: каталог появляется **при установке AIStudio**. Исходник каркаса — `docs/AdapterPackageTemplates/demo/`. Автор копирует каталог, меняет `id`, заполняет schema и runtime.

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
- combobox и подсказки из `schema\` — в т.ч. **ProbeKey** в редакторе «Давление среды на виталы» (`EnvironmentPressureRules.dat`) и каталог ID рецептов из `recipe-catalog.json`.

### 0.5. Когда нужен пакет для студии

Ядро платформы для host — **`isida.dll`** и runtime contract YAML (§ 5–7). Формат **пакета** (`manifest.json`, `Adapters\{id}\`, BootData, schema) обязателен **только** если используется AIStudio.

| Компонент | Только host (без студии) | С AIStudio |
|-----------|--------------------------|------------|
| `isida.dll` | **Обязательно** (ссылка в host) | **Обязательно** в `runtime\` |
| `manifest.json`, `Adapters\{id}\` | Не нужны | Нужны для регистрации |
| `BootData\Environment\*.yaml` в пакете | Не нужны | Нужны (seed проекта) |
| `schema\` | Не нужна | **Обязательна** для регистрации в студии (редакторы «Среда») |

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

| Значение | Поддержка AIStudio (текущая) |
|----------|------------------------------|
| `2.0` | Да (текущая) |
| `1.0` | Нет (устарела) |

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
  schema\                               ОБЯЗАТЕЛЬНО для регистрации в студии
  adapter-settings\                     ОПЦИОНАЛЬНО
```

Пакет **для регистрации в AIStudio** должен содержать `manifest.json` с `contractVersion: "2.0"` и каталог `schema\` с валидными JSON (§ 12). `BootData\` и `runtime\` рекомендуются для seed проекта и дистрибуции host, но **не блокируют** «Проверить». Без студии достаточно host + `isida.dll` (§ 0.5).

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
- **Dual round-trip** (рекомендуемый gate): эталонный файл → Read → Write → Read; результат эквивалентен канону для **AIStudio** и **runtime host**, если оба используют `EnvironmentYamlCodec` из `SymbiontEnv.Contract.dll`.

### 5.2. Таблица алиасов (поддерживает `EnvironmentYamlCodec`)

| Область | Канон (запись) | Алиасы (только чтение) |
|---------|----------------|------------------------|
| ID рецепта | `id` | `recipe_id` |
| ID триггера | `id` | `trigger_key` |
| Типы документа в preconditions | `document_kinds` | `document_types` |
| Фильтр документа триггера | `document_kinds` | `document_filter` |

Дополнительные алиасы (`set_custom_property`, `command_pre`, `file_save_post` и т.п.) — **на усмотрение конкретного host**; кодек их не нормализует. Host может принимать legacy-имена при исполнении, но при записи из студии используются канонические ключи § 5.2.

### 5.3. Внутренняя модель после чтения

- `document_kinds` в preconditions и триггерах выделяется в отдельное поле модели; остальные ключи `preconditions` с булевыми значениями попадают в словарь `Preconditions` (имена **opaque**, семантика — в `schema\` адаптера).
- `type` шага и `kind` detect **сохраняются как в файле** (без переименования кодеком).
- Параметры шага и detect — словари строк; `command_ids` в detect сериализуется списком целых.

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

Структура YAML фиксирована; **набор ключей** задаётся адаптером в `schema/recipe-preconditions.json`:

| Ключ в YAML | Тип в schema | Описание |
|-------------|--------------|----------|
| `document_kinds` | `stringList` | Допустимые типы активного документа (значения `enumValues` — opaque-строки среды) |
| `<bool_key>` | `bool` | Логические предусловия; имя и подпись — из `fields[].key` / `label` |

Студия показывает только поля из schema. Runtime host интерпретирует ключи, описанные в schema своего пакета; неизвестные ключи при чтении YAML сохраняются, но host может игнорировать их при исполнении.

Типовой пример bool-ключей (не обязателен для всех адаптеров): `not_edit_mode`, `not_read_only`, `checkout_required`.

### 6.4. Блок `steps`

Каждый элемент:

```yaml
- type: <string>
  <param_key>: <scalar>
```

Все ключи кроме `type` — параметры шага (строковые scalars). Регистр ключей параметров **не** значим при чтении.

#### 6.4.1. Типы шагов

Допустимые `type` и параметры описываются в `schema/recipe-steps.json` (`stepTypes[]`). Студия строит combobox и поля шага **только** из этого файла.

Типовой набор для CAD/документных сред (пример в каркасе demo):

| `type` | Назначение |
|--------|------------|
| `set_property` | Запись свойства документа (`name`, `template`, `config`, `overwrite`, …) |
| `run_command` | Выполнение команды host по `command_id` |
| `refresh` / `rebuild` | Обновление представления или перестроение модели (семантика host) |
| `log` | Запись в лог host (`message`, `level`) |

Поле `runtimeType` в schema (опционально) подсказывает host внутреннее имя обработчика; в YAML на диске пишется `type` из schema.

Неизвестный `type` при исполнении — ошибка host (политика на стороне runtime).

#### 6.4.2. Расширение типов шагов

Новые `type` добавляет автор адаптера в `recipe-steps.json` и реализует в runtime host. Контракт YAML **не** фиксирует closed enum — только строковый `type` и произвольные scalar-параметры.

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
| `influence_action_id` | Influence Actions (EA) | При срабатывании detect → `ApplyMultipleInfluenceActions`. **Только ID из `InfluenceActions.dat`**, не RuleId из `EnvironmentPressureRules.dat`. |

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

Точный список фиксируется в README пакета адаптера. «Создать пакет…» копирует стартовый SDK (`isida.dll`, `SymbiontEnv.Contract.dll`, …) из `%ProgramData%\ISIDA\AdapterPackageTemplates\demo\runtime\` и дополняет DLL host из `bin\Debug`. «Проверить» **не** валидирует содержимое `runtime\` (§ 12) — ответственность автора при дистрибуции.

### 8.3. Исключения

**Не** включать в пакет: `.cs`, `.csproj`, `.pdb` (опционально), исходники, `.git`, артефакты AIStudio.

---

## 9. `manifest.json`

### 9.1. Обязательные поля (contract 2.0)

```json
{
  "id": "my-adapter",
  "displayName": "Мой адаптер",
  "version": "1.0.0",
  "contractVersion": "2.0",
  "author": "Example Author",
  "bootDataRelativePath": "BootData"
}
```

| Поле | Тип | Правила |
|------|-----|---------|
| `id` | string | `[a-z0-9_-]+`, уникален среди установленных |
| `displayName` | string | Для UI студии |
| `version` | string | Версия пакета |
| `contractVersion` | string | `"2.0"` для этого документа |
| `author` | string | |
| `bootDataRelativePath` | string | Относительно корня пакета; обычно `"BootData"` |

### 9.2. Опциональные поля

| Поле | Назначение |
|------|------------|
| `schemaVersion` | Версия набора JSON в `schema\` (для contract 2.0: `"2.0"`) |
| `adapterSettingsRelativePath` | `"adapter-settings"` |
| `description` | Краткое описание |
| `supportedStudioVersions` | Ограничение версий AIStudio (post-MVP) |

### 9.3. Пример (полный)

```json
{
  "id": "my-adapter",
  "displayName": "Мой адаптер — Reactive Core",
  "version": "1.0.0",
  "contractVersion": "2.0",
  "schemaVersion": "2.0",
  "author": "Example Author",
  "bootDataRelativePath": "BootData",
  "adapterSettingsRelativePath": "adapter-settings",
  "description": "Эталонный адаптер среды для симбионта ISIDA"
}
```

---

## 10. UI schema (`schema\`)

Студия загружает schema через `AdapterSchemaLoader` при редактировании проекта с выбранным `AdapterId`. Каталог `schema\` **обязателен** для регистрации пакета (§ 12). Без schema редакторы «Среда» показывают пустые списки полей и типов шагов.

### 10.1. Имена файлов (schemaVersion 2.0)

| Файл | Содержание |
|------|------------|
| `recipe-preconditions.json` | Поля блока `preconditions` |
| `recipe-steps.json` | Допустимые `type` шагов и параметры |
| `recipe-catalog.json` | Каталог допустимых `id` рецептов (combobox в редакторе рецепта) |
| `trigger-filter.json` | Фильтр контекста триггера (`document_kinds`, …) |
| `trigger-detect.json` | Правила `detect` (`detectKinds[]`) |
| `metric-probes.json` | Ключи **ProbeKey** в `EnvironmentPressureRules.dat` (редактор «Давление среды на виталы») |

### 10.2. Формат дескriptor поля (фрагмент)

```json
{
  "schemaVersion": "2.0",
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
  "schemaVersion": "2.0",
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

**Правило:** каждый `key` / `type` / `kind` в schema должен поддерживаться runtime адаптера. Студия не добавляет поля и типы шагов, отсутствующие в schema.

### 10.5. Формат `recipe-catalog.json` (фрагмент)

```json
{
  "schemaVersion": "2.0",
  "recipes": [
    {
      "id": "doc_props_on_save",
      "label": "Свойства документа при сохранении",
      "description": "Заполнить свойства по шаблону политики"
    }
  ]
}
```

### 10.4. Формат `metric-probes.json` (фрагмент)

```json
{
  "schemaVersion": "2.0",
  "probes": [
    {
      "key": "host.active_doc_modified",
      "label": "Документ изменён",
      "description": "Сэмплер host: активный документ имеет несохранённые изменения"
    }
  ]
}
```

Студия заполняет combobox **ProbeKey** в `EnvironmentPressureRules.dat` из `probes[].key` (плюс пункт «оператор, не среда»). Дискретные стимулы оператора и YAML-триггеры — в `InfluenceActions.dat` (без ProbeKey).

---

## 11. Согласование парсеров (AIStudio ↔ runtime host)

С contract **2.0** AIStudio и host **должны** ссылаться на одну сборку `SymbiontEnv.Contract.dll` (`EnvironmentYamlCodec`). Согласование — round-trip тестами:

| # | Тест |
|---|------|
| T1 | `EnvironmentYamlCodec`: Read(fixture) → Write(temp) → Read(temp) — эквивалент канону |
| T2 | Host после Write студии — Read без ошибок |
| T3 | Студия после Write host — Read без ошибок |

Рекомендуемые фикстуры: пустые каталоги, полный рецепт, legacy `recipe_id` + `document_types`, триггер с `document_filter`.

**CI:** пересборка при изменении `AdapterContract.md`, `SymbiontEnv.Contract`, редакторов среды в AIStudio.

---

## 12. Валидация «Проверить» (AIStudio)

Реализация: `AdapterPackageValidator` в `SymbiontEnv.Contract.dll` (обёртка в студии — `AdapterValidator`).

| # | Проверка | Severity |
|---|----------|----------|
| V1 | `manifest.json` существует, JSON валиден | Error |
| V2 | `contractVersion` = `"2.0"` | Error |
| V3 | `id` валиден (`[a-z0-9_-]+`) | Error |
| V4 | Каталог `schema\` существует и содержит `*.json` | Error |
| V5 | Структура JSON в schema (массивы `fields`, `stepTypes`, `detectKinds`, `probes`, …) | Error |
| V6 | `displayName`, `version`, `author` заполнены | Warning |
| V7 | `BootData\Environment\*.yaml` парсятся codec (если есть) | Warning |
| V8 | Неизвестные имена файлов в `schema\` | Info |

`runtime\`, BootData и `adapter-settings\` **не блокируют** регистрацию. Студия **не загружает** DLL из `runtime\`.

---

## 13. Разделение уровней данных

| Уровень | Владелец | Формат |
|---------|----------|--------|
| Гомеостаз, рефлексы, сценарии | ISIDA / AIStudio | `.dat`, сценарии |
| Каркас рецепта/триггера (`id`, `adaptive_action_id`, `steps`, `detect`) | Contract 2.0 | YAML |
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

## 15. Чеклист соответствия contract 2.0

**Автор host (минимум, без студии):**

- [ ] Host ссылается на `isida.dll` и `SymbiontEnv.Contract.dll`
- [ ] YAML читается/пишется через `EnvironmentYamlCodec` (§ 5–7)

**Автор пакета для AIStudio:**

- [ ] `manifest.json` с `contractVersion: "2.0"` и валидным `id`
- [ ] `schema\` — шесть JSON-файлов (§ 10.1) согласованы с runtime
- [ ] «Проверить» проходит без Error
- [ ] (Рекомендуется) BootData с `recipes:` / `triggers:` для seed проекта
- [ ] (Рекомендуется) `runtime\` — полный closure с `isida.dll` для дистрибуции

**Разработчик AIStudio:**

- [ ] Round-trip T1–T3 на фикстурах YAML
- [ ] «Проверить» реализует § 12
- [ ] Редакторы «Среда» читают schema через `AdapterSchemaLoader`
- [ ] `AdapterId` в `AgentProperties.dat`; блок «Среда» без адаптера

**Разработчик runtime host:**

- [ ] Использует `EnvironmentYamlCodec` из `SymbiontEnv.Contract.dll`
- [ ] Поддерживает `type` / `kind` / bool-ключи из schema пакета
- [ ] Исполняет шаги и detect по семантике своей среды

---

## 16. История версий документа

| Версия | Дата | Изменения |
|--------|------|-----------|
| 2.0 | 2026-06-07 | `contractVersion` 2.0; schema-driven редакторы; `recipe-catalog.json`; `SymbiontEnv.Contract.dll`; валидация § 12 без проверки `runtime\`; preconditions/steps — по schema |
| 1.0 | 2026-06-03 | Первый нормативный release (устарел) |

---

*Контракт 2.0 соответствует `SymbiontEnv.Contract.dll` (`EnvironmentYamlCodec`, `AdapterPackageValidator`) и редакторам «Среда» в AIStudio. Изменения codec или schema без обновления этого документа не допускаются.*
