# Каркас пакета адаптера (`demo`)

Шаблон для разработки нового адаптера среды. **Не регистрируйте этот каталог как есть** — скопируйте в рабочую папку и измените `id` в `manifest.json`.

**Версии:** `contractVersion` и `schemaVersion` в `manifest.json` — **`2.0`**.

## Где лежит копия на машине

После установки AIStudio:

```
%ProgramData%\ISIDA\AdapterPackageTemplates\demo\
```

(обычно `C:\ProgramData\ISIDA\AdapterPackageTemplates\demo\`)

## Структура

| Путь | Назначение |
|------|------------|
| `manifest.json` | Метаданные; `contractVersion` / `schemaVersion`: `2.0`; уникальный `id` (`[a-z0-9_-]+`) |
| `BootData/Environment/*.yaml` | Образцы рецептов и триггеров (seed проекта симбионта) |
| `schema/*.json` | **Обязательно** для регистрации и редакторов «Среда» (шесть файлов, см. `schema/README.txt`) |
| `runtime/` | Стартовый SDK для разработчика host (студия DLL не загружает) |

## Быстрый старт

1. Скопируйте этот каталог, например `D:\Work\my-env-adapter\`.
2. В `manifest.json` — свой `id`, `displayName`, `author`; оставьте `contractVersion` и `schemaVersion` = `2.0`.
3. Заполните все файлы в `schema\` под вашу среду.
4. При необходимости дополните `runtime\` DLL host.
5. AIStudio → **Зарегистрировать из папки…** → **Проверить** (manifest + schema).

Подробнее — `docs/AdapterAuthorGuide.md`, норматив — `docs/AdapterContract.md`.
