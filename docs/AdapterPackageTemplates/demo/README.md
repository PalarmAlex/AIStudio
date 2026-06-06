# Каркас пакета адаптера (`demo`)

Шаблон для разработки нового адаптера среды. **Не регистрируйте этот каталог как есть** — скопируйте в рабочую папку и измените `id` в `manifest.json`.

## Где лежит копия на машине

После первого запуска AIStudio (или открытия «Зарегистрированные пакеты»):

```
%ProgramData%\ISIDA\AdapterPackageTemplates\demo\
```

(обычно `C:\ProgramData\ISIDA\AdapterPackageTemplates\demo\`)

## Структура

| Путь | Назначение |
|------|------------|
| `manifest.json` | Метаданные; задайте уникальный `id` (`[a-z0-9_-]+`) |
| `BootData/Environment/*.yaml` | Образцы рецептов и триггеров (seed проекта симбионта) |
| `schema/*.json` | Описание capability для студии (combobox, post-MVP) |
| `runtime/` | Сюда DLL после сборки host (для «Проверить» и инсталлятора) |

## Быстрый старт

1. Скопируйте этот каталог, например `D:\Work\my-env-adapter\`.
2. В `manifest.json` — свой `id`, `displayName`, `author`.
3. Заполните `schema\` (типы шагов, probe keys для таблицы воздействий).
4. Соберите host → скопируйте все нужные DLL в `runtime\`.
5. AIStudio → **Зарегистрировать из папки…** → **Проверить**.

Подробнее — `docs/AdapterAuthorGuide.md`. Эталон с данными Velum — `docs/adapter-package-example/velum/`.
