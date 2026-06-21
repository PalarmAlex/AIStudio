# Каркас пакета адаптера (`demo`)

Шаблон для разработки нового адаптера среды. **Не регистрируйте этот каталог как есть** — скопируйте в рабочую папку и измените `id` в `manifest.json`.

**Версии:** `contractVersion` и `schemaVersion` в `manifest.json` — **`3.2`**.

## Где лежит копия на машине

После установки AIStudio:

```
%ProgramData%\ISIDA\AdapterPackageTemplates\demo\
```

## Структура

| Путь | Назначение |
|------|------------|
| `manifest.json` | `contractVersion` / `schemaVersion`: `3.2` |
| `BootData/Environment/EnvironmentRecipes.yaml` | `adaptive_action_id`, `steps` |
| `schema/*.json` | **Четыре** обязательных JSON + опционально `recipe-template-catalog.json` |
| `runtime/` | SDK + host DLL; host dispatch по G_AD на `OnPulseCompleted` |

## Demo ID

| Поле | Пример | Назначение |
|------|--------|------------|
| `adaptive_action_id` | `1` | G_AD в `AdaptiveActions.dat` проекта |
| recipe `id` | `doc_props_on_save` | Каталог рецептов |

Mechanical path и Command-пуск — в `EnvironmentPressureRules.dat` и `GeneticReflexes.dat`, не в YAML triggers.

## Быстрый старт

1. Скопируйте каталог, например `D:\Work\my-env-adapter\`.
2. В `manifest.json` — свой `id`, `displayName`, `author`; `contractVersion` = `3.2`.
3. Заполните `schema\` (четыре обязательных JSON).
4. Дополните `runtime\` DLL host с G_AD dispatch.
5. AIStudio → **Зарегистрировать из папки…** → **Проверить**.

Подробнее — `docs/AdapterAuthorGuide.md` (v2.0), норматив — `docs/AdapterContract.md` (v3.2).
