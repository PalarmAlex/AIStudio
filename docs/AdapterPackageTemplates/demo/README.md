# Каркас пакета адаптера (`demo`)

Шаблон для разработки нового адаптера среды. **Не регистрируйте этот каталог как есть** — скопируйте в рабочую папку и измените `id` в `manifest.json`.

**Версии:** `contractVersion` и `schemaVersion` в `manifest.json` — **`3.0`** (`architectureSpecVersion`: `2.2`).

## Где лежит копия на машине

После установки AIStudio:

```
%ProgramData%\ISIDA\AdapterPackageTemplates\demo\
```

## Структура

| Путь | Назначение |
|------|------------|
| `manifest.json` | `contractVersion` / `schemaVersion`: `3.0` |
| `BootData/Environment/*.yaml` | `expression_pattern_id`, `homeostasis_deltas`, `reflex_trigger_expression_pattern_id` |
| `schema/*.json` | **Шесть** файлов (incl. `expression-pattern-catalog.json`) |
| `runtime/` | SDK + host DLL; host реализует `IHostMotorDispatcher` |

## Demo ID паттернов (согласовать с boot проекта)

| ID | Token | Назначение |
|----|-------|------------|
| 101 | `expr:demo.env.document_saved` | Триггер Save → Genetic L3 |
| 201 | `expr:demo.recipe.doc_props_on_save` | Рецепт `doc_props_on_save` |

## Быстрый старт

1. Скопируйте каталог, например `D:\Work\my-env-adapter\`.
2. В `manifest.json` — свой `id`, `displayName`, `author`; `contractVersion` = `3.0`.
3. Заполните `schema\` (все шесть JSON) и `expression-pattern-catalog.json`.
4. Дополните `runtime\` DLL host с `IHostMotorDispatcher`.
5. AIStudio → **Зарегистрировать из папки…** → **Проверить**.

Подробнее — `docs/AdapterAuthorGuide.md` (v1.0), норматив — `docs/AdapterContract.md` (v3.0).
