# Руководство автора адаптера среды (AIStudio)

Практическая инструкция для **разработчика host-среды** (Velum, Excel, CAD…). AIStudio **не обязательна**: ядро платформы — **`isida.dll`**, runtime contract YAML ([`AdapterContract.md`](AdapterContract.md) **v3.2**), dispatch рецептов по **G_AD** на `OnPulseCompleted`. Студия нужна, если вы (или коллега) редактируете **данные симбионта** и YAML среды в общем UI.

Технические форматы — в [`AdapterContract.md`](AdapterContract.md). Архитектура operator/environment — [`SymbiontArchitecture_IsidaAdapter.md`](../../../../VELUM/Velum/docs/SymbiontArchitecture_IsidaAdapter.md). План функций студии — [`AdapterPlatform_ImplementationPlan.md`](AdapterPlatform_ImplementationPlan.md).

**Версия гайда:** 2.0 (2026-06-21), `contractVersion` платформы: **3.2**.

---

## 1. Два пути разработчика адаптера

### 1.1. Только host + `isida.dll` (студия не нужна)

| Нужно | Не нужно |
|-------|----------|
| Ссылка на `isida.dll` в проекте host | `manifest.json`, каталог `Adapters\` |
| Чтение/запись YAML contract **3.2** (`SymbiontEnv.Contract.dll`) | Регистрация пакета в AIStudio |
| Dispatch рецептов по `adaptive_action_id` (G_AD) | `schema\`, BootData в формате пакета |

### 1.2. Интеграция с AIStudio (пакет в `Adapters\{id}\`)

| Зачем пакет | Что даёт студии |
|-------------|-----------------|
| Регистрация в **Зарегистрированные пакеты…** | Список типов среды при создании проекта |
| `BootData\Environment\` | Seed `EnvironmentRecipes.yaml` в новый проект |
| `schema\` | Поля в редакторах «Рецепты среды» и «Метрики среды (EA)» |
| `runtime\` | DLL для дистрибуции (**`isida.dll`** обязателен) |
| `manifest.json` | `id`, версия, `contractVersion: "3.2"` |

### 1.3. Без адаптера (исследовательские / виртуальные проекты)

| Аспект | Поведение |
|--------|-----------|
| Движок | Студия через `isida.dll` |
| «Среда» | Меню «Среда» **недоступно** (нет `AdapterId`) |
| Оператор | Пульт: каналы Verbal/Command + **`HomeostasisSignificance`** |
| Типичное применение | Исследования, сценарии без CAD |

### 1.4. Три канала среды (contract v3.2)

| Путь | Источник | Где настраивается |
|------|----------|-------------------|
| **Operator** | Пульт: Verbal, Command, Visual, EA | каналы + EA в ISIDA |
| **Mechanical** | Метрики host, SessionHealth | `InfluenceActions.dat` (ProbeKey, ID ≥ 50) |
| **Command SW** | Command buffer host | `GeneticReflexes.dat` → `command_pattern_ids`; idle-flush |

**Удалено в v3.2:** `EnvironmentTriggers.yaml`, `recommended_trigger_keys`, `expression_pattern_id`, `IHostMotorDispatcher`.

**Не использовать** в новых пакетах: `influence_action_id`, EA-прокси для mechanical path.

### 1.5. Кто что делает в студии

| Роль | Типичные задачи |
|------|-----------------|
| **Разработчик адаптера** | Host DLL, «Создать пакет…», «Проверить», ZIP |
| **Настройщик симбионта** | Регистрация пакета, проект, рецепты, EA с ProbeKey |
| **Разработчик симбионта** | Гомеостаз, genetic reflexes (Command pattern), G_AD |

Меню: **Проект → Зарегистрированные пакеты…**; **Среда → Рецепты / Метрики среды (EA)**.

---

## 2. Каркас пакета (`demo`)

| Файл в `demo\runtime\` | Назначение |
|------------------------|------------|
| `isida.dll` | Движок симбионта |
| `SymbiontEnv.Contract.dll` | Кодек YAML v3.2, валидация пакета |

| Где | Путь |
|-----|------|
| Исходник | `docs/AdapterPackageTemplates/demo/` |
| На машине | `%ProgramData%\ISIDA\AdapterPackageTemplates\demo\` |

**Не регистрируйте** demo напрямую. Используйте **Создать пакет…** или ручную сборку (§ 7).

---

## 3. Два сценария

### Сценарий A — всё у себя

1. Собрать host с G_AD dispatch.
2. **Создать пакет…** (§ 5.1) или ручная сборка (§ 7).
3. **Проверить** — `contractVersion` должен быть **3.2**.
4. **Создать проект симбионта** с типом среды.
5. Рецепты, EA с ProbeKey, genetic Level3 (Command).

### Сценарий B — пакет передали коллеге

ZIP → **Зарегистрировать** → **Проверить** → новый проект.

---

## 4. Раздел «Зарегистрированные пакеты среды»

| Действие | Назначение |
|----------|------------|
| **Создать пакет…** | Manifest 3.2 → SDK + host → Adapters |
| **Зарегистрировать из папки/ZIP…** | Готовый пакет |
| **Проверить** | Manifest, schema; triggers YAML / legacy schema → Error |
| **Руководство автора** | Этот документ |

---

## 5. Сборка пакета в студии

### 5.1. «Создать пакет…»

1. Форма manifest: `contractVersion` = **`3.2`**.
2. Опционально — каталог `bin\Debug` host.
3. SDK из demo + host DLL → **Проверить** → `Adapters\{id}\`.

---

## 6. Структура каталога пакета

```
MyAdapter\
  manifest.json                     ← contractVersion: "3.2"
  runtime\                          ← host + isida.dll + SymbiontEnv.Contract.dll
  BootData\Environment\
    EnvironmentRecipes.yaml         ← adaptive_action_id + steps
  schema\                           ← четыре обязательных JSON (§ 7.5)
  adapter-settings\                 ← опционально
```

**Запрещено в v3.2:** `BootData\Environment\EnvironmentTriggers.yaml`, `schema\trigger-detect.json`, `schema\trigger-catalog.json`, `schema\expression-pattern-catalog.json`.

---

## 7. Сборка пакета вручную

### 7.1–7.3. Host и runtime

- Все DLL в `runtime\`, включая **`isida.dll`** и **`SymbiontEnv.Contract.dll`**.
- Host читает `AppGlobalState.ActiveAdaptiveActions` на **`GlobalTimer.OnPulseCompleted`**, ищет рецепт по `adaptive_action_id` и исполняет шаги.
- .NET Framework 4.8.

### 7.4. `BootData\Environment\`

**Только `EnvironmentRecipes.yaml`:**

```yaml
# Рецепты среды (contract 3.2: adaptive_action_id + steps).
schema: environment-recipes/3.2
recipes:
  - id: kb_name_on_save
    display_name: "Наименование по КБ"
    adaptive_action_id: 37
    reactive_eligible: true
    steps:
      - type: invoke
        handler: save_file_name
        template: '$PRP:"SW-Folder Name"-{DISCIPLINE}-{SEQ:4}'
      - type: comment
        text: "G_AD dispatch на OnPulseCompleted"
```

**Запрещено:** `expression_pattern_id`, `recommended_trigger_keys`, `genetic_reflex_id`, `influence_action_id`.

Mechanical path и Command-пуск настраиваются **не** в YAML triggers, а в `InfluenceActions.dat` (ProbeKey) и `GeneticReflexes.dat`.

Формат — [`AdapterContract.md`](AdapterContract.md) § 6–7.

### 7.5. `schema\` (schemaVersion 3.2)

| Файл | Содержание |
|------|------------|
| `handlers-catalog.json` | Handler'ы `invoke` |
| `recipe-catalog.json` | ID рецептов для редактора |
| `metric-probes.json` | ProbeKey для `InfluenceActions.dat` |
| `command-buffer-policy.json` | Defaults idle-flush: `idle_flush_ms`, `max_tokens`, `max_age_ms` |
| `recipe-template-catalog.json` | **Опционально:** маски `{PLACEHOLDER}` и имена свойств |

Пример `command-buffer-policy.json`:

```json
{
  "idle_flush_ms": 3000,
  "max_tokens": 64,
  "max_age_ms": 120000
}
```

Пример `handlers-catalog.json`:

```json
{
  "schemaVersion": "3.2",
  "handlers": [
    {
      "id": "save_file_name",
      "label": "Имя файла по шаблону",
      "argsSchema": [
        { "key": "template", "label": "Шаблон", "type": "string", "editorHint": "template_placeholder" }
      ]
    }
  ]
}
```

### 7.6. `manifest.json`

```json
{
  "id": "my-cad-host",
  "displayName": "Адаптер CAD-среды (пример)",
  "version": "1.0.0",
  "contractVersion": "3.2",
  "schemaVersion": "3.2",
  "author": "Your Company",
  "bootDataRelativePath": "BootData",
  "description": "Рецепты + pressure rules; без EnvironmentTriggers.yaml."
}
```

| Поле | Описание |
|------|----------|
| `contractVersion` | **3.2** (текущая) |
| `schemaVersion` | Рекомендуется `"3.2"` |

### 7.7–7.8. adapter-settings и потоки

На целевой машине с CAD нужен runtime из `Adapters\{id}\runtime\`. Defaults Command idle-flush seed — из `command-buffer-policy.json` (Velum: `Velum.Settings.xml`).

---

## 8. Регистрация и проект симбионта

1. **Зарегистрировать пакет…** → `Adapters\{id}\`.
2. **Проверить** — без Error; triggers YAML / legacy schema → Error.
3. **Создать проект** — опционально тип среды → BootData seed.
4. **`AdapterId`** в `AgentProperties.dat`.
5. Меню **«Среда»** — только **Рецепты** и **Давление**; стадия 0, пульсация выключена.

---

## 9. Три уровня настроек

| Файл | Где | Зачем |
|------|-----|-------|
| `adapter-settings\Settings.xml` | Пакет | Дефолты host |
| `Settings\Settings.xml` | Проект | Пути Data/BootData |
| `AgentProperties.dat` | Проект | **AdapterId**, стадия |

---

## 10. Редакторы «Среда»

| Вкладка | Поля v3.2 |
|---------|-----------|
| Рецепты | `adaptive_action_id`, `steps` |
| Метрики среды | `InfluenceActions.dat` (ProbeKey из `metric-probes.json`, ID ≥ 50) |

Цепочка runtime: **SW Command → idle-flush → genetic Level3 → G_AD → рецепт**; **метрика → pressure rule → P_i** (mechanical path).

Picker `adaptive_action_id` — из `AdaptiveActions.dat` проекта; id рецепта — из `recipe-catalog.json`.

---

## 11. Что делает «Проверить»

| Проверка | Severity |
|----------|----------|
| `contractVersion` = **`3.2`** | Error |
| `schema\` с обязательными JSON (§ 7.5) | Error |
| `EnvironmentTriggers.yaml` в BootData | **Error** |
| `trigger-detect.json` / `trigger-catalog.json` в schema | **Error** |
| YAML парсится codec v3.2 | Warning |
| **`expression_pattern_id` / `recommended_trigger_keys` в YAML** | **Error** |

`runtime\` не проверяется при регистрации.

---

## 12. Частые ошибки

| Симптом | Причина | Решение |
|---------|---------|---------|
| «Неподдерживаемый contractVersion» | manifest `3.0` / `3.1` | Поставить **`3.2`**, миграция § 13 |
| `EnvironmentTriggers.yaml` в пакете | v3.1 boot | Удалить файл; mechanical → pressure rules |
| `recommended_trigger_keys` в recipe | v3.1 | Удалить ключ |
| Рецепт не исполняется | Нет genetic L3 / G_AD | Проверить `AdaptiveActions.dat` и rising edge G_AD |
| Double dispatch (Velum) | Command + EA на одном событии | Разделить mechanical и Command (Spec §4) |
| Шаги не исполняются | Строковый `args` | Flat-ключи по `argsSchema` |

---

## 13. Миграция 3.1 → 3.2

| v3.1 | v3.2 |
|------|------|
| `EnvironmentTriggers.yaml` | **удалить файл** |
| `recommended_trigger_keys` | **удалить ключ** |
| `schema/trigger-detect.json` | удалить из пакета |
| `schema/trigger-catalog.json` | удалить из пакета |
| `homeostasis_deltas` в trigger | перенести в `InfluenceActions.dat` (ProbeKey, influences) |
| `reflex_trigger_command_pattern_id` | `GeneticReflexes.dat` → `command_pattern_ids` |

Подробнее — [`AdapterContract.md`](AdapterContract.md) § 16.

---

## 14. Чеклист перед регистрацией

- [ ] `contractVersion`: **`3.2`**
- [ ] `schema\` — **четыре** обязательных JSON (§ 7.5)
- [ ] BootData: **только** `EnvironmentRecipes.yaml`
- [ ] Нет `EnvironmentTriggers.yaml`, trigger schema, `expression-pattern-catalog.json`
- [ ] «Проверить» — без Error
- [ ] Host: G_AD dispatch + codec v3.2
- [ ] `runtime\` — полный closure

---

## 15. Связанные документы

| Документ | Назначение |
|----------|------------|
| [`AdapterContract.md`](AdapterContract.md) | Норматив YAML v3.2 |
| [`SymbiontArchitecture_IsidaAdapter.md`](../../../../VELUM/Velum/docs/SymbiontArchitecture_IsidaAdapter.md) | Архитектура G_AD, Command, pressure |
| [`RefactoringPlan_SimplifyEnvironmentLayer.md`](../../../../VELUM/Velum/docs/RefactoringPlan_SimplifyEnvironmentLayer.md) | Cut v3.2 |
| `docs/AdapterPackageTemplates/demo/` | Каркас пакета |

---

## Приложение A. Пример: Velum / SolidWorks host

1. Host: dispatch на `OnPulseCompleted` по `adaptive_action_id`.
2. **Создать пакет…** → `contractVersion: "3.2"`.
3. Boot: рецепты с G_AD из `AdaptiveActions.dat`.
4. Mechanical: `InfluenceActions.dat` (ProbeKey из `metric-probes.json`).
5. Command Save: `GeneticReflexes.dat` → `command_pattern_ids` (`sw:*`); idle-flush из `command-buffer-policy.json`.

---

*Версия 2.0 (2026-06-21): contract 3.2, G_AD dispatch, pressure rules, без triggers YAML.*
