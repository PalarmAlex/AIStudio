# Контракт платформы адаптеров среды (AIStudio)

**Версия контракта:** `3.2`  
**Дата:** 2026-06-21  
**Статус:** нормативный документ для AIStudio, авторов адаптеров и runtime host. AIStudio **не обязательна** для разработки host; пакет с `manifest.json` нужен только при интеграции со студией (§ 0.5). Кодек YAML и проверка пакета — **`SymbiontEnv.Contract.dll`** (общая библиотека студии и host).

Связанные документы:

| Документ | Назначение |
|----------|------------|
| [`SymbiontArchitecture_IsidaAdapter.md`](file:///D:/VELUM/Velum/docs/SymbiontArchitecture_IsidaAdapter.md) (v1.4+) | **Архитектура:** operator vs environment, G_AD dispatch, Command idle-flush, Level3 |
| [`RefactoringPlan_SimplifyEnvironmentLayer.md`](../../../../VELUM/Velum/docs/RefactoringPlan_SimplifyEnvironmentLayer.md) | Cut v3.2: удаление `EnvironmentTriggers.yaml` |
| [`AdapterAuthorGuide.md`](file:///D:/ISIDA/Programms/app/AIStudio/docs/AdapterAuthorGuide.md) | Практика сборки пакета |
| [`AdapterPlatform_ImplementationPlan.md`](file:///D:/ISIDA/Programms/app/AIStudio/docs/AdapterPlatform_ImplementationPlan.md) | План реализации в студии |

**Нормативная модель v3.2:** G_AD + Command idle-flush + pressure rules. Рецепты — только `adaptive_action_id` + `steps`. Contract v3.1 (`EnvironmentTriggers.yaml`, `recommended_trigger_keys`) и v3.0 (Expression) **отклонены**. Dual-read **не поддерживается**.

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
  schema\                   — описание capability (редакторы «Среда», ProbeKey, command-buffer-policy)
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

**`runtime\*.dll`** — программа, которая **вне студии** читает YAML проекта, применяет pressure rules, агрегирует Command-буфер и исполняет рецепты по **G_AD** от движка.

| Где нужен runtime | Когда |
|-------------------|--------|
| В пакете адаптера | Дистрибуция; проверка «Проверить» (наличие файлов, в т.ч. `isida.dll`) |
| На машине пользователя | Работа в реальной среде (целевое приложение и т.д.) |
| В студии | **Не загружается** |

**Моторный dispatch (v3.2):** host читает `AppGlobalState.ActiveAdaptiveActions` на **`GlobalTimer.OnPulseCompleted`** (после `ProcessReflexPulse`), ищет рецепт по `adaptive_action_id` и исполняет шаги. **`IHostMotorDispatcher` не используется.**

### 0.4. Связь проекта симбионта и пакета

Поле **`AdapterId`** в `Data\Gomeostas\AgentProperties.dat` — метка:

> «Для этого симбионта используем описание среды из зарегистрированного пакета `{id}`».

От `AdapterId` зависят:

- доступность редакторов меню «Среда»;
- seed BootData при **создании** проекта (если тип среды выбран);
- combobox и подсказки из `schema\` — ProbeKey в `EnvironmentPressureRules.dat`, каталог рецептов из `recipe-catalog.json`.

### 0.5. Когда нужен пакет для студии

Ядро платформы для host — **`isida.dll`**, runtime contract YAML (§ 5–7), Command idle-flush, pressure rules. Формат **пакета** обязателен **только** при использовании AIStudio.

| Компонент | Только host (без студии) | С AIStudio |
|-----------|--------------------------|------------|
| `isida.dll` | **Обязательно** | **Обязательно** в `runtime\` |
| `manifest.json`, `Adapters\{id}\` | Не нужны | Нужны для регистрации |
| `BootData\Environment\*.yaml` в пакете | Не нужны | Нужны (seed проекта) |
| `schema\` | Не нужна | **Обязательна** для регистрации |

### 0.6. Operator path vs mechanical path (норматив v3.1)

**Не смешивать** в YAML и runtime:

| Путь | Источник | YAML / ISIDA | Попадает в operator `PerceptionImage`? |
|------|----------|--------------|----------------------------------------|
| **Operator** | Пульт: Verbal, Command, Visual, EA | каналы + EA | **Да** |
| **Mechanical (среда)** | Pressure rules, SessionHealth | `homeostasis_deltas` в triggers; **не** EA-прокси | **Нет** (прямая запись P_i) |
| **Наблюдаемая команда SW** | Command buffer host | `CommandPatternIdList` | **Да** (отдельно от mechanical delta) |

- Событие SW → гомеostasis — **только** `homeostasis_deltas` через pressure rules / mechanical path; **не** `InfluenceActions.dat` и **не** auto EA.
- Рецепт исполняется по **`adaptive_action_id`** (G_AD из `AdaptiveActions.dat`) на `OnPulseCompleted`, **не** по `expression_pattern_id`.
- **`reflex_trigger_command_pattern_id`** в триггерах — **только** редактор / обучение / связи genetic Level3; **не** runtime-flush и **не** отдельный канал «Save → стимул». Save попадает в образ **только** через Command-буфер (`sw:*`) + idle / max / «Отправить».
- **Double dispatch запрещён:** одно SW-событие — Command buffer **или** mechanical delta, не оба с одной семантикой через EA auto.

#### Command idle-flush (runtime host)

SW-команды **не отправляются по одной** и **не привязываются к пульсу**. Границы Command-образа задаёт **пауза оператора** (idle), как на пульте.

| Триггер flush | Поведение |
|---------------|-----------|
| **Истечение паузы** (`CommandBufferIdleFlushMs`, default 3000) | Стимул **только** с Command из буфера (EA=[]) |
| **Переполнение / max age** | Принудительный flush (`CommandBufferMaxTokens`, `CommandBufferMaxAgeMs`; max age — от **первого** токена) |
| **Кнопка «Отправить»** | Буфер включается в Command **того же** операторского стимула; idle-таймер отменяется |

Defaults seed — `schema/command-buffer-policy.json`; редактирование — вкладка **«Адаптер»** в настройках проекта host (`Velum.Settings.xml` для Velum).

**При остановленной пульсации:** append, flush, pressure rules, motor dispatch и стимулы **не выполняются**; буфер и таймер сбрасываются.

**Не делать:** flush на каждый токен; flush на каждый пульс; flush по событиям SW (`document_saved`, Save и т. д.).

### 0.7. Роли пользователей

| Роль | Типичный поток |
|------|----------------|
| **Настройщик симбионта** | Зарегистрировать пакет → создать/открыть проект → настроить рецепты/триггеры v3.1 |
| **Автор адаптера (только host)** | Host + `isida.dll` + YAML contract 3.1 + G_AD dispatch |
| **Автор адаптера + студия** | Пакет → регистрация → тестовый проект с `AdapterId` |

### 0.8. Запрещённые формулировки в UI

Избегать: «подключить адаптер», «адаптер активен», «запустить адаптер».

Использовать: **«зарегистрировать пакет»**, **«тип среды»**, **«описание пакета `{id}`»**.

---

## 1. Область действия

### 1.1. Что регулирует контракт

1. **Структура пакета адаптера** — каталог в `%ProgramData%\ISIDA\Adapters\{id}\`.
2. **`manifest.json`** — метаданные и `contractVersion`.
3. **Runtime contract YAML** — только `EnvironmentRecipes.yaml` (contract **3.2**).
4. **Нормализация** — канонические ключи, round-trip через `SymbiontEnv.Contract.dll`.
5. **UI schema** (`schema\`) — handler'ы, события, каталоги для редакторов «Среда», политика Command-буфера.

### 1.2. Что контракт не регулирует

- Форматы ISIDA (`.dat`: genetic reflexes, `AdaptiveActions.dat`, `CommandPhrases.dat`, гомеостаз) — `isida.dll` и редакторы студии.
- Реализацию API целевой среды — только в runtime host.
- Загрузку DLL адаптера в AIStudio — **запрещена**.
- Алгоритм genetic match по Command Level3 — ISIDA (фаза 5 плана Velum); до неё Command-only flush может не активировать б/у рефлексы.

### 1.3. Роли

| Роль | Ответственность |
|------|-----------------|
| **AIStudio** | Редактирование данных; регистрация пакетов; запись YAML v3.2; валидация binding G_AD |
| **Пакет адаптера** | manifest, BootData-образцы, runtime (`isida.dll` + host), schema |
| **Runtime host** | Command idle-flush; pressure rules; G_AD → рецепт на `OnPulseCompleted` |
| **Проект симбионта** | `AgentProperties.dat`; `BootData\Environment\*.yaml`; `Data\` |

---

## 2. Версионирование

### 2.1. `contractVersion` в manifest

| Значение | Поддержка |
|----------|-----------|
| `3.2` | **Да (текущая)** — G_AD, Command idle-flush, pressure rules; только рецепты YAML |
| `3.1` | **Нет** — `EnvironmentTriggers.yaml`, `recommended_trigger_keys` удалены |
| `3.0` | **Нет** — Expression / `expression_pattern_id` устарели |
| `2.0`, `1.0` | **Нет** |

- Студия и codec **отклоняют** пакет с неизвестной или устаревшей major-версией.
- Миграция boot — **offline** (ручная перепись или утилита студии); dual-read в production codec **не делается**.

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
  Data\                       — ISIDA (.dat), incl. AdaptiveActions.dat, CommandPhrases.dat, EnvironmentPressureRules.dat
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
  manifest.json                         ОБЯЗАТЕЛЬНО (contractVersion 3.2)
  BootData\Environment\EnvironmentRecipes.yaml   ОБЯЗАТЕЛЬНО (допускается recipes: [])
  runtime\                              ОБЯЗАТЕЛЬНО для DLL-host
  schema\                               ОБЯЗАТЕЛЬНО для регистрации в студии
```

### 4.2. Имена файлов BootData

| Файл | Строго |
|------|--------|
| Рецепты | `BootData\Environment\EnvironmentRecipes.yaml` |

**Запрещено в пакете v3.2:** `EnvironmentTriggers.yaml`, `schema/trigger-detect.json`, `schema/trigger-catalog.json`.

---

## 5. Нормализация YAML (round-trip)

### 5.1. Принцип

- **Канон записи** — ключи из § 6–7; корневое поле `schema` обязательно.
- **Round-trip** через `EnvironmentYamlCodec` (`SymbiontEnv.Contract.dll`, contract 3.1 only).

### 5.2. Таблица алиасов

| Область | Канон (запись) | Алиасы (только чтение) |
|---------|----------------|------------------------|
| ID рецепта | `id` | `recipe_id` |
| ID триггера | `id` | `trigger_key` |
| Тип события | `event_kind` | — (`event` **отклоняется** codec) |

**Запрещено (contract 3.1):** `expression_pattern_id`, `reflex_trigger_expression_pattern_id`, `influence_action_id`, `genetic_reflex_id`, `conditioned_reflex_id`, `automatizm_id`, `recommended_trigger_influence_ids` — **ошибка** при чтении.

### 5.3. Внутренняя модель после чтения

- Шаг рецепта: `type: invoke` + `handler` + flat args; `type: comment` + `text`.
- Триггер: `event_kind` + `homeostasis_deltas` (`param_id`, `delta`) + опционально `reflex_trigger_command_pattern_id`.

### 5.4. Пустые коллекции

```yaml
recommended_trigger_keys: []   # ЗАПРЕЩЕНО в v3.2 — error при чтении
```

### 5.5. Кодировка

UTF-8 без BOM; подмножество YAML 1.1.

---

## 6. Runtime contract: `EnvironmentRecipes.yaml`

### 6.1. Корневая структура (contract 3.2)

```yaml
schema: environment-recipes/3.2
recipes:
  - id: <string>                         # обязательно, уникален в файле
    adaptive_action_id: <int>            # обязательно, > 0; ID в AdaptiveActions.dat (G_AD)
    display_name: <string>               # опционально
    description: <string>                # опционально
    reactive_eligible: <bool>            # опционально; default true
    steps: [ ... ]                       # опционально
```

**Запрещено:** `genetic_reflex_id`, `conditioned_reflex_id`, `automatizm_id`, `expression_pattern_id`, `recommended_trigger_influence_ids`, `recommended_trigger_keys`, `EnvironmentTriggers.yaml`.

Условия **запуска** рецепта задаются движком (рефлекс/автоматизм → G_AD); host dispatch — **только** по rising edge `ActiveAdaptiveActions` на `OnPulseCompleted`.

### 6.2. Связь с ISIDA

| Поле | Справочник ISIDA | Назначение |
|------|------------------|------------|
| `adaptive_action_id` | `AdaptiveActions.dat` (G_AD) | Host ищет рецепт при dispatch на `OnPulseCompleted` |

Пуск по Command SW — `GeneticReflexes.dat` → `command_pattern_ids`; mechanical path — `EnvironmentPressureRules.dat`.

Runtime **не** проверяет существование G_AD в boot при чтении YAML; валидация — студия (§ 12) и smoke-тесты host.

### 6.3. Блок `steps`

Без изменений принципа: `invoke` (host) и `comment` (пропуск).

```yaml
- type: invoke
  handler: <string>
  <arg_key>: <scalar>
```

Handler'ы — в `schema/handlers-catalog.json`.

### 6.4. Пример (эталон contract 3.2)

```yaml
# Рецепты среды (contract 3.2: adaptive_action_id + steps).
schema: environment-recipes/3.2
recipes:
  - id: kb_name_on_save
    display_name: "Наименование по КБ после Save"
    adaptive_action_id: 37
    reactive_eligible: true
    steps:
      - type: invoke
        handler: save_file_name
        template: '$PRP:"SW-Folder Name"-{DISCIPLINE}-{SEQ:4}'
```

---

## 7. Удалено в contract 3.2

Слой **`EnvironmentTriggers.yaml`** удалён. Mechanical path — только **`EnvironmentPressureRules.dat`**. Пуск Command — **`GeneticReflexes.dat`** → `command_pattern_ids`. Файлы `schema/trigger-detect.json` и `schema/trigger-catalog.json` **запрещены** в пакете.

---

## 8. Schema JSON (`schema\`)

Каталог `schema\` обязателен для регистрации в AIStudio. **Запрещены** в v3.2: `trigger-detect.json`, `trigger-catalog.json`.

Обязательные файлы: `handlers-catalog.json`, `recipe-catalog.json`, `metric-probes.json`, `command-buffer-policy.json`, `recipe-template-catalog.json` (по необходимости).

---

## 9. Runtime closure (каталог `runtime\`)

### 8.1. Требование

В `runtime\` — полный closure host, включая:

- основную DLL host;
- **`isida.dll`** (обязательно);
- **`SymbiontEnv.Contract.dll`**;
- interop целевой среды.

### 8.2. Чеклист

| Категория | Примеры |
|-----------|---------|
| Host | `velum.dll`, `my-host.dll` |
| ISIDA | `isida.dll` |
| Contract | `SymbiontEnv.Contract.dll` |

---

## 9. `manifest.json`

### 9.1. Обязательные поля (contract 3.2)

```json
{
  "id": "my-adapter",
  "displayName": "Мой адаптер",
  "version": "1.0.0",
  "contractVersion": "3.2",
  "author": "Example Author",
  "bootDataRelativePath": "BootData"
}
```

| Поле | Правила |
|------|---------|
| `contractVersion` | **`"3.2"`** для этого документа |

### 9.2. Опциональные поля

| Поле | Назначение |
|------|------------|
| `schemaVersion` | Версия JSON в `schema\` (для contract 3.2: `"3.2"`) |
| `description` | Краткое описание пакета |

### 9.3. Пример

```json
{
  "id": "sldworks_19",
  "displayName": "Адаптер SolidWorks 2019 (Velum)",
  "version": "1.0.0",
  "contractVersion": "3.2",
  "schemaVersion": "3.2",
  "author": "VELUM",
  "bootDataRelativePath": "BootData",
  "description": "G_AD dispatch, Command idle-flush, pressure rules (contract 3.2)."
}
```

---

## 10. UI schema (`schema\`)

### 10.1. Имена файлов (schemaVersion 3.2)

| Файл | Содержание |
|------|------------|
| `handlers-catalog.json` | Handler'ы шагов `invoke` |
| `recipe-catalog.json` | Каталог `id` рецептов |
| `metric-probes.json` | ProbeKey в `EnvironmentPressureRules.dat` |
| `command-buffer-policy.json` | Defaults idle-flush: `idle_flush_ms`, `max_tokens`, `max_age_ms` |
| `recipe-template-catalog.json` | **Опционально:** маски `{PLACEHOLDER}` и имена свойств для редактора шагов рецепта |

**Запрещено в v3.2:** `trigger-detect.json`, `trigger-catalog.json`, `expression-pattern-catalog.json`.

### 10.2. `command-buffer-policy.json`

Seed defaults для host settings (Velum: `%ProgramData%\VELUM\Velum.Settings.xml` при первом запуске):

```json
{
  "idle_flush_ms": 3000,
  "max_tokens": 64,
  "max_age_ms": 120000
}
```

Runtime host может переопределять значения в настройках проекта; пакет задаёт **начальные** defaults.

### 10.3. `handlers-catalog.json`

Формат без изменений принципа; `schemaVersion`: `"3.2"`.

В `argsSchema[]` опционально поле **`editorHint`** — подсказка UI студии (не влияет на runtime):

| `editorHint` | Редактор в `SchemaActionPanel` |
|--------------|--------------------------------|
| `template_placeholder` | Кнопка «Вставить…» — справочник из `recipe-template-catalog.json` → `placeholders[]` |
| `property_name` | Кнопка «Справочник…» — `recipe-template-catalog.json` → `propertyNames[]` |

### 10.4. `recipe-template-catalog.json`

**Назначение:** машиночитаемый справочник для редактора шагов рецепта. Студия читает файл из `schema\`; **runtime host не обязан** загружать его.

Файл **опционален** для «Проверить»; без него кнопки справочника скрыты или неактивны.

### 10.5. ProbeKey и оператор

Студия заполняет combobox **ProbeKey** в `EnvironmentPressureRules.dat` из `probes[].key` (плюс «оператор, не среда»).

**Operator path:** дискретные стимулы оператора — каналы Verbal/Command/Visual + EA без ProbeKey. **Mechanical path:** строки EA с ProbeKey — шаблон давления на виталы (фаза A) и триггер рефлекса (фаза B); runtime Velum orchestrator.

### 10.6. Фаза A: независимое давление probe (без gating)

**Норма:** каждая плохая метрика с ProbeKey **независимо** давит на свои параметры виталов по шаблону EA (`influences` в `InfluenceActions.dat`). Runtime **не** подавляет давление одной probe, пока другая связанная probe плохая.

**Обоснование:** ранее в Velum использовалось скрытое gating (например, Material не давил, пока UnsavedNew плохой) — для уменьшения комбинаций стилей Level2. Это **снято**: конфликтующие реакции разводятся через **антагонисты** в `InfluenceActions.dat` (`AntagonistInfluences`), а не через подавление в orchestrator. Одновременное давление нескольких метрик — штатная ситуация и может требовать **специфического** genetic/conditioned рефлекса на комбинацию параметров.

**Запрещено в runtime:** правила вида «probe X не давит, пока probe Y плохая», если это не зафиксировано явно в данных проекта (антагонисты, отдельные рефлексы на комбинации).

### 10.7. Cooldown давления фазы A (на одну probe)

Оркестратор Velum не применяет давление по **той же** ProbeKey чаще одного раза за `ReflexActionDisplayDuration` тактов пульса (вкладка «Адаптер» / `Velum.Settings.xml`, то же значение передаётся в ISIDA). **Разные** плохие probe на одном такте могут давить **одновременно** — cooldown не разводит метрики между собой.

**Зачем:** не долбить один витал по одной метрике на каждом пульсе, пока probe остаётся плохой; ритм согласован с минимальным интервалом рефлексов в движке (тот же параметр). Разведение контекстов Level2 при нескольких метриках — через **доминантный параметр** ISIDA (§10.6), не через адаптерный throttle между probe.

**Не путать с:** `RecipeDispatchCooldownPulses` (моторика рецептов), снятым gating связанных probe (§10.6).

---

## 11. Согласование парсеров (AIStudio ↔ runtime host)

AIStudio и host **должны** использовать одну `SymbiontEnv.Contract.dll` (codec contract **3.2**).

| # | Тест |
|---|------|
| T1 | Read(fixture v3.2) → Write → Read — эквивалент канону |
| T2 | Host Read после Write студии |
| T3 | Студия Read после Write host |
| T4 | Recipe `adaptive_action_id` ↔ `AdaptiveActions.dat` boot |
| T5 | Нет `EnvironmentTriggers.yaml` и legacy ключей в round-trip |

---

## 12. Валидация «Проверить» (AIStudio)

| # | Проверка | Severity |
|---|----------|----------|
| V1 | `manifest.json` валиден | Error |
| V2 | `contractVersion` = `"3.2"` | Error |
| V3 | `id` валиден | Error |
| V4 | `schema\` с JSON (incl. `command-buffer-policy.json`) | Error |
| V5 | Структура schema | Error |
| V6 | YAML парсится codec v3.2 | Warning |
| V7 | `adaptive_action_id` ∈ boot `AdaptiveActions.dat` | Error |
| V8 | Нет `EnvironmentTriggers.yaml` в BootData | Error |
| V9 | Нет `trigger-detect.json` / `trigger-catalog.json` в schema | Error |
| V10 | Legacy ключи (`recommended_trigger_keys`, `expression_pattern_id`, …) | **Error** |

---

## 13. Разделение уровней данных

| Уровень | Владелец | Формат |
|---------|----------|--------|
| Гомеостаз, рефлексы, каналы, G_AD | ISIDA / AIStudio | `.dat` |
| Каркас рецепта (`id`, `adaptive_action_id`, `steps`) | Contract 3.2 | YAML |
| Mechanical path (метрики → P_i) | ISIDA / AIStudio | `EnvironmentPressureRules.dat` |
| Operator stimulus | ISIDA | PerceptionImage (EA, Verbal, Command, Visual) |
| Исполнение рецепта | Host | G_AD rising edge → `RecipeExecutor` → API среды |
| Command агрегация SW | Host | `VelumSolidCommandBuffer` + idle-flush (Velum) |

---

## 14. `adapter-settings` (опционально)

Без изменений принципа (пути к YAML на машине host). Ключ `EnvironmentRecipesFilePath`.

---

## 15. Чеклист соответствия contract 3.2

**Автор host:**

- [ ] `isida.dll`, `SymbiontEnv.Contract.dll` (v3.2)
- [ ] YAML v3.2 через `EnvironmentYamlCodec` (`schema: environment-recipes/3.2`)
- [ ] G_AD dispatch на `OnPulseCompleted`; `RecipeCatalog.FindByAdaptiveActionId`
- [ ] Command idle-flush; **без** auto EA для SW-событий
- [ ] Mechanical path → `EnvironmentPressureRules.dat` → P_i

**Автор пакета для AIStudio:**

- [ ] `contractVersion: "3.2"`; без `EnvironmentTriggers.yaml` и trigger schema JSON
- [ ] `schema\` incl. `command-buffer-policy.json`
- [ ] BootData recipes **без** `recommended_trigger_keys`
- [ ] «Проверить» без Error

**Разработчик AIStudio:**

- [ ] Меню «Среда»: давление + рецепты (без триггеров / обзора поведения)
- [ ] Отклонение YAML v3.1 triggers / legacy ключей
- [ ] `AdapterId` в `AgentProperties.dat`

---

## 16. Миграция contract 3.1 → 3.2

Offline, вручную (dual-read **не делается**):

| v3.1 | v3.2 |
|------|------|
| `EnvironmentTriggers.yaml` | **удалить файл** |
| `recommended_trigger_keys` в recipe | **удалить ключ** |
| `schema/trigger-detect.json`, `trigger-catalog.json` | удалить из пакета |
| `homeostasis_deltas` в trigger | перенести нужные эффекты в `EnvironmentPressureRules.dat` |
| `reflex_trigger_command_pattern_id` | перенести в `GeneticReflexes.dat` → `command_pattern_ids` |
| `schema: environment-recipes/3.1` | `environment-recipes/3.2` |
| `contractVersion: "3.1"` | `"3.2"` |

---

## 17. Миграция contract 3.0 → 3.1 (историческая)

Одноразовая offline-перепись (dual-read в production codec **не делается**):

| Было (3.0 Expression) | Стало (3.1) |
|-----------------------|-------------|
| `expression_pattern_id` | `adaptive_action_id` (маппинг по motor-столбцу `GeneticReflexes.dat`) |
| `reflex_trigger_expression_pattern_id` | `reflex_trigger_command_pattern_id` |
| `event` | `event_kind` |
| `parameter_id` в deltas | `param_id` |
| `expression-pattern-catalog.json` | удалить; G_AD из `AdaptiveActions.dat` |

| Было (2.0) | Стало (3.1) |
|------------|-------------|
| `influence_action_id` | `homeostasis_deltas` + опционально `reflex_trigger_command_pattern_id` |
| `genetic_reflex_id` в recipe | `adaptive_action_id` |
| `recommended_trigger_influence_ids` | `recommended_trigger_keys` |

---

## 18. История версий документа

| Версия | Дата | Изменения |
|--------|------|-----------|
| 3.2 | 2026-06-21 | Удалены `EnvironmentTriggers.yaml`, `recommended_trigger_keys`, trigger schema; mechanical path только через pressure rules |
| 3.1 | 2026-06-20 | G_AD + Command idle-flush (**промежуточный**, заменён v3.2) |
| 3.0 | 2026-06-13 | Expression channel (**устарел**) |
| 2.0 | 2026-06-09 | `invoke`/`comment`; `event` (**устарел**) |
| 1.0 | 2026-06-03 | Первый release (**устарел**) |

---

*Контракт 3.2 согласован с [`SymbiontArchitecture_IsidaAdapter.md`](file:///D:/VELUM/Velum/docs/SymbiontArchitecture_IsidaAdapter.md) и [`RefactoringPlan_SimplifyEnvironmentLayer.md`](../../../../VELUM/Velum/docs/RefactoringPlan_SimplifyEnvironmentLayer.md).*
