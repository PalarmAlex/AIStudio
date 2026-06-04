# Эталонный пакет адаптера `velum` (MVP)

Структура каталога для ручной сборки и регистрации в AIStudio (фаза 1.6).

## Содержимое в репозитории

| Путь | Назначение |
|------|------------|
| `manifest.json` | Метаданные, `contractVersion` 1.0 |
| `BootData/Environment/*.yaml` | Минимальные образцы для «Проверить» и seed проекта |
| `runtime/` | **Не в git** — копируется при сборке |

## Runtime closure

После сборки Velum (`d:\VELUM\Velum\bin\Debug\`) скопируйте **все** DLL из этого каталога в `runtime\`, без которых host не стартует:

| Категория | Примеры |
|-----------|---------|
| Host | `velum.dll` |
| Контракт YAML | `SymbiontEnv.Contract.dll`, `Newtonsoft.Json.dll` |
| ISIDA | `isida.dll` и транзитивные зависимости из `isida\bin\Debug\` |
| SolidWorks | `SolidWorks.Interop.*.dll` |
| XCad / UI | зависимости из выхода сборки Velum |

Проверка: каталог `runtime\` не пуст и содержит `velum.dll` + `isida.dll`.

## Сборка пакета вручную

1. Скопировать этот каталог, например в `D:\Out\velum-pkg\`.
2. Заполнить `runtime\` из `Velum\bin\Debug\` (см. выше).
3. В AIStudio: **Установить адаптер…** → папка пакета → **Проверить**.

Подробнее — `docs/AdapterAuthorGuide.md` § 6.
