# Руководство автора адаптера среды (AIStudio)

Практическая инструкция для **разработчика host-среды** (Velum, Excel, CAD…). AIStudio **не обязательна**: ядро платформы — **`isida.dll`**, контракт YAML ([`AdapterContract.md`](AdapterContract.md) **v3.0**), **`IHostMotorDispatcher`**. Студия нужна, если вы (или коллега) редактируете **данные симбионта** и YAML среды в общем UI.

Технические форматы — в [`AdapterContract.md`](AdapterContract.md). Архитектура operator/environment — [`SymbiontArchitecture_OperatorEnvironment_Spec.md`](../../../../VELUM/Velum/docs/SymbiontArchitecture_OperatorEnvironment_Spec.md) v2.2. План функций студии — [`AdapterPlatform_ImplementationPlan.md`](AdapterPlatform_ImplementationPlan.md).

**Версия гайда:** 1.0 (2026-06-13), `contractVersion` платформы: **3.0**.

---

## 1. Два пути разработчика адаптера

### 1.1. Только host + `isida.dll` (студия не нужна)

| Нужно | Не нужно |
|-------|----------|
| Ссылка на `isida.dll` в проекте host | `manifest.json`, каталог `Adapters\` |
| Чтение/запись YAML contract **3.0** (`SymbiontEnv.Contract.dll`) | Регистрация пакета в AIStudio |
| Реализация **`IHostMotorDispatcher`** | `schema\`, BootData в формате пакета |

### 1.2. Интеграция с AIStudio (пакет в `Adapters\{id}\`)

| Зачем пакет | Что даёт студии |
|-------------|-----------------|
| Регистрация в **Зарегистрированные пакеты…** | Список типов среды при создании проекта |
| `BootData\Environment\` | Seed YAML v3 в новый проект |
| `schema\` | Поля в редакторах «Рецепты/Триггеры среды» |
| `runtime\` | DLL для дистрибуции (**`isida.dll`** обязателен) |
| `manifest.json` | `id`, версия, `contractVersion: "3.0"` |

### 1.3. Без адаптера (исследовательские / виртуальные проекты)

| Аспект | Поведение |
|--------|-----------|
| Движок | Студия через `isida.dll` |
| «Среда» | Меню рецептов/триггеров **недоступно** |
| Оператор | Пульт: каналы Verbal/Expression + **`HomeostasisSignificance`** (не `InfluenceActions.dat`) |
| Типичное применение | Исследования, сценарии без CAD |

### 1.4. Operator path vs mechanical path (кратко)

| Путь | YAML / механизм |
|------|-----------------|
| **Оператор** | PerceptionImage + significance (Spec v2.2) |
| **Среда (триггеры)** | `homeostasis_deltas` + `reflex_trigger_expression_pattern_id` |
| **Рецепты** | `expression_pattern_id` → `IHostMotorDispatcher` |

**Не использовать** в новых пакетах: `adaptive_action_id`, `influence_action_id`, EA-прокси.

### 1.5. Кто что делает в студии

| Роль | Типичные задачи |
|------|-----------------|
| **Разработчик адаптера** | Host DLL, «Создать пакет…», «Проверить», ZIP |
| **Настройщик симбионта** | Регистрация пакета, проект, рецепты/триггеры v3 |
| **Разработчик симбионта** | Гомеостаз, Expression primaries, genetic reflexes |

Меню: **Проект → Зарегистрированные пакеты…**

---

## 2. Каркас пакета (`demo`)

| Файл в `demo\runtime\` | Назначение |
|------------------------|------------|
| `isida.dll` | Движок симбионта |
| `SymbiontEnv.Contract.dll` | Кодек YAML v3, валидация пакета |

| Где | Путь |
|-----|------|
| Исходник | `docs/AdapterPackageTemplates/demo/` |
| На машине | `%ProgramData%\ISIDA\AdapterPackageTemplates\demo\` |

**Не регистрируйте** demo напрямую. Используйте **Создать пакет…** или ручную сборку (§ 7).

---

## 3. Два сценария

### Сценарий A — всё у себя

1. Собрать host.
2. **Создать пакет…** (§ 5.1) или ручная сборка (§ 7).
3. **Проверить** — `contractVersion` должен быть **3.0**.
4. **Создать проект симбионта** с типом среды.
5. Рецепты/триггеры v3, виртуальные тесты.

### Сценарий B — пакет передали коллеге

ZIP → **Зарегистрировать** → **Проверить** → новый проект.

---

## 4. Раздел «Зарегистрированные пакеты среды»

| Действие | Назначение |
|----------|------------|
| **Создать пакет…** | Manifest 3.0 → SDK + host → Adapters |
| **Зарегистрировать из папки/ZIP…** | Готовый пакет |
| **Проверить** | Manifest, schema; legacy ключи в YAML → Error |
| **Руководство автора** | Этот документ |

---

## 5. Сборка пакета в студии

### 5.1. «Создать пакет…»

1. Форма manifest: `contractVersion` = **`3.0`** (не `2.0`).
2. Опционально — каталог `bin\Debug` host.
3. SDK из demo + host DLL → **Проверить** → `Adapters\{id}\`.

---

## 6. Структура каталога пакета

```
MyAdapter\
  manifest.json                     ← contractVersion: "3.0"
  runtime\                          ← host + isida.dll + IHostMotorDispatcher
  BootData\Environment\
    EnvironmentRecipes.yaml         ← expression_pattern_id
    EnvironmentTriggers.yaml        ← homeostasis_deltas, expr:env trigger
  schema\                           ← шесть JSON (§ 7.5)
  adapter-settings\                 ← опционально
```

---

## 7. Сборка пакета вручную

### 7.1–7.3. Host и runtime

- Все DLL в `runtime\`, включая **`isida.dll`**.
- Host реализует **`IHostMotorDispatcher`** (Spec v2.2 §3.7).
- .NET Framework 4.8.

### 7.4. `BootData\Environment\`

**`EnvironmentRecipes.yaml`**

```yaml
# Рецепты: expression_pattern_id → IHostMotorDispatcher (contract 3.0).
recipes:
  - id: kb_name_on_save
    display_name: "Наименование по КБ"
    expression_pattern_id: 42001
    reactive_eligible: true
    recommended_trigger_keys: [save_active_document]
    steps:
      - type: invoke
        handler: set_custom_property
        name: Обозначение
        template: "{PROJECT}-{SEQ:4}"
        overwrite: if_empty
```

**`EnvironmentTriggers.yaml`**

```yaml
# Триггеры: mechanical path (contract 3.0). Не influence_action_id.
triggers:
  - id: save_active_document
    display_name: "Save активного документа"
    event: document_saved
    homeostasis_deltas:
      - parameter_id: 3
        delta: 1.0
    reflex_trigger_expression_pattern_id: 41001
  - id: before_rebuild
    display_name: "Перед Rebuild (только Command buffer)"
    event: command_before
    command_ids: [57603]
```

Для примеров используйте **expression pattern ID** из boot проекта (`DefaultExpressionPrimaries.tmp`, `expr:velum.*`, `expr:env.*`). **Не** `adaptive_action_id` / `influence_action_id`.

Формат — [`AdapterContract.md`](AdapterContract.md) § 6–7.

### 7.5. `schema\` (schemaVersion 3.0)

| Файл | Содержание |
|------|------------|
| `handlers-catalog.json` | Handler'ы `invoke` |
| `trigger-detect.json` | `event` kinds |
| `trigger-catalog.json` | ID триггеров |
| `recipe-catalog.json` | ID рецептов |
| `expression-pattern-catalog.json` | **Новый:** id, token (`expr:velum.*`, `expr:env.*`), label |
| `metric-probes.json` | ProbeKey |

Пример `expression-pattern-catalog.json`:

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

Пример `handlers-catalog.json`:

```json
{
  "schemaVersion": "3.0",
  "handlers": [
    {
      "id": "set_custom_property",
      "label": "Задать свойство документа",
      "argsSchema": [
        { "key": "name", "label": "Имя", "type": "string", "required": true },
        { "key": "template", "label": "Шаблон", "type": "string" }
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
  "contractVersion": "3.0",
  "schemaVersion": "3.0",
  "architectureSpecVersion": "2.2",
  "author": "Your Company",
  "bootDataRelativePath": "BootData"
}
```

| Поле | Описание |
|------|----------|
| `contractVersion` | **3.0** (текущая) |
| `architectureSpecVersion` | Рекомендуется `"2.2"` |

### 7.7–7.8. adapter-settings и потоки

Без изменений принципа (§ 8–9 старого гайда). На целевой машине с CAD нужен runtime из `Adapters\{id}\runtime\`.

---

## 8. Регистрация и проект симбионта

1. **Зарегистрировать пакет…** → `Adapters\{id}\`.
2. **Проверить** — без Error; legacy YAML → Error.
3. **Создать проект** — опционально тип среды → BootData seed.
4. **`AdapterId`** в `AgentProperties.dat`.
5. Меню **«Среда»** — стадия 0, пульсация выключена.

---

## 9. Три уровня настроек

| Файл | Где | Зачем |
|------|-----|-------|
| `adapter-settings\Settings.xml` | Пакет | Дефолты host |
| `Settings\Settings.xml` | Проект | Пути Data/BootData |
| `AgentProperties.dat` | Проект | **AdapterId**, стадия |

---

## 10. Редакторы «Среда»

| Вкладка | Поля v3 |
|---------|---------|
| Рецепты | `expression_pattern_id`, steps |
| Триггеры | `event`, `homeostasis_deltas`, `reflex_trigger_expression_pattern_id` |
| Давление среды | `EnvironmentPressureRules.dat` (ProbeKey из schema) |

Цепочка в обзоре: **триггер → mechanical delta / expr:env → genetic reflex → expr:velum.recipe → host dispatch**.

Picker `expression_pattern_id` — из `expression-pattern-catalog.json` и boot проекта.

---

## 11. Что делает «Проверить»

| Проверка | Severity |
|----------|----------|
| `contractVersion` = **`3.0`** | Error |
| `schema\` с JSON (incl. `expression-pattern-catalog.json`) | Error |
| YAML парсится codec v3 | Warning |
| **`adaptive_action_id` / `influence_action_id` в YAML** | **Error** |

`runtime\` не проверяется при регистрации.

---

## 12. Частые ошибки

| Симптом | Причина | Решение |
|---------|---------|---------|
| «Неподдерживаемый contractVersion» | manifest `2.0` | Поставить **`3.0`**, обновить YAML |
| Legacy ключи в YAML | Старый boot | Миграция §16 Contract |
| Рецепт не исполняется | Нет genetic L3 / dispatcher | `expression_pattern_id` + `IHostMotorDispatcher` |
| Double dispatch (Velum) | Command + EA на одном событии | Разделить `event_kind` (Spec §4.2) |
| Триггер двигает гомеостаз через EA | `influence_action_id` | Заменить на `homeostasis_deltas` |
| Шаги не исполняются | Строковый `args` | Flat-ключи по `argsSchema` |

---

## 13. Версионирование

- **`contractVersion`:** **3.0** (текущая). **2.0 устарел.**
- **`version`:** версия вашего пакета.
- Миграция 2.0→3.0 — [`AdapterContract.md`](AdapterContract.md) § 16.

---

## 14. Чеклист перед регистрации

- [ ] `contractVersion`: **`3.0`**
- [ ] `schema\` — **шесть** JSON (§ 7.5)
- [ ] BootData YAML **без** `adaptive_action_id` / `influence_action_id`
- [ ] «Проверить» — без Error
- [ ] Host: `IHostMotorDispatcher` + codec v3
- [ ] `runtime\` — полный closure

---

## 15. Связанные документы

| Документ | Назначение |
|----------|------------|
| [`AdapterContract.md`](AdapterContract.md) | Норматив YAML v3 |
| [`SymbiontArchitecture_OperatorEnvironment_Spec.md`](../../../../VELUM/Velum/docs/SymbiontArchitecture_OperatorEnvironment_Spec.md) | Архитектура v2.2 |
| [`Velum_RecipeReflexEditor_ImplementationPlan.md`](../../../../VELUM/Velum/docs/Velum_RecipeReflexEditor_ImplementationPlan.md) | Эталон Velum |
| `docs/AdapterPackageTemplates/demo/` | Каркас пакета |

---

## Приложение A. Пример: Velum / CAD host

1. Host с `VelumHostMotorDispatcher`.
2. **Создать пакет…** → `contractVersion: "3.0"`.
3. Boot: `expr:velum.recipe.*`, `expr:env.document_saved`.
4. Trigger Save: `homeostasis_deltas` + `reflex_trigger_expression_pattern_id`.
5. Recipe: `expression_pattern_id` + steps COM.

---

*Версия 1.0 (2026-06-13): contract 3.0, expression_pattern_id, mechanical triggers, IHostMotorDispatcher, синхронизация со Spec v2.2.*
