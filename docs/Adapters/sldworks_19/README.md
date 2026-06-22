# Пакет адаптера SolidWorks 19 (`sldworks_19`)

Зарегистрированный пакет среды **Velum** для SolidWorks 2019. AIStudio использует его при создании проекта симбионта с типом среды `sldworks_19`.

**Версии:** `contractVersion` и `schemaVersion` в `manifest.json` — **`3.1`**.

**Нормативная модель:** G_AD + Command idle-flush + `homeostasis_deltas`. Contract v3.0 (Expression, `expression_pattern_id`, `IHostMotorDispatcher`) **не поддерживается**.

## Архитектура

| Документ | Путь |
|----------|------|
| Архитектура ISIDA ↔ адаптер | `D:\VELUM\Velum\docs\SymbiontArchitecture_IsidaAdapter.md` |
| План рефакторинга каналов | `D:\VELUM\Velum\docs\RefactoringPlan_OperatorEnvironmentChannels.md` |
| Контракт YAML/пакета (этот каталог) | `AdapterContract.md` / `AdapterContract.html` |

Runtime host (Velum): dispatch рецептов по **`adaptive_action_id`** (G_AD) на `OnPulseCompleted`; SW-команды — **Command idle-flush**; механика — **pressure rules** → P_i. Auto-path через EA для событий SW **запрещён**.

## Структура пакета

| Путь | Назначение |
|------|------------|
| `manifest.json` | Метаданные; `id`: `sldworks_19`; `contractVersion` / `schemaVersion`: `3.2` |
| `BootData/Environment/EnvironmentRecipes.yaml` | Образец рецептов (seed проекта; **обязателен** `schema: environment-recipes/3.2`) |
| `schema/*.json` | Capability для редакторов «Среда» (см. `schema/README.txt`) |
| `runtime/` | `velum.dll`, `isida.dll`, `SymbiontEnv.Contract.dll` |
| `AdapterContract.md` / `.html` | Норматив contract 3.2 (синхронизирован с AIStudio) |

## Schema (contract 3.2)

| Файл | Назначение |
|------|------------|
| `handlers-catalog.json` | Handler'ы для шагов `type: invoke` |
| `recipe-catalog.json` | Каталог ID рецептов |
| `command-buffer-policy.json` | Defaults idle-flush (`idle_flush_ms`, `max_tokens`, `max_age_ms`) |
| `metric-probes.json` | ProbeKey для «Давление среды на виталы» |
| `recipe-template-catalog.json` | **Опционально:** маски `{PLACEHOLDER}` и имена свойств для редактора шагов |

**Удалено в v3.2:** `trigger-detect.json`, `trigger-catalog.json`, `EnvironmentTriggers.yaml`, `expression-pattern-catalog.json`.

## YAML (runtime contract)

- **Рецепты:** `schema: environment-recipes/3.2`, `adaptive_action_id`, шаги `invoke` / `comment`.
- Mechanical path — `EnvironmentPressureRules.dat`; пуск Command — `GeneticReflexes.dat` → `command_pattern_ids`.

Подробнее — `AdapterContract.md`. Практика сборки пакета — `D:\ISIDA\Programms\app\AIStudio\docs\AdapterAuthorGuide.md`.

## Runtime vs schema

`VelumRecipeTemplateResolver` разрешает шаблоны при исполнении рецепта (контекст SW-сессии).  
`recipe-template-catalog.json` нужен **только AIStudio** — справочник масок в UI; host может не читать этот файл.

## Миграция с contract 3.0

1. Обновить `manifest.json` и все `schema/*.json` до `schemaVersion: "3.1"`.
2. Добавить `command-buffer-policy.json`; удалить `expression-pattern-catalog.json`.
3. Переписать BootData YAML: добавить `schema:`, заменить `expression_pattern_id` → `adaptive_action_id`, `event` → `event_kind`.
4. Переписать BootData **открытых проектов** (seed не перезаписывает существующие файлы).

См. § 16 в `AdapterContract.md`.
