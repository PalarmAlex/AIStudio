# Каркас пакета адаптера (`demo`)

Шаблон для разработки нового адаптера среды. **Не регистрируйте этот каталог как есть** — скопируйте в рабочую папку и измените `id` в `manifest.json`.

## Где лежит на машине пользователя

```
%ProgramData%\ISIDA\AdapterPackageTemplates\demo\
```

(обычно `C:\ProgramData\ISIDA\AdapterPackageTemplates\demo\`)

Каталог создаёт **установщик AIStudio**, не runtime студии.

## Исходник для сборки установщика

В репозитории AIStudio: `docs/adapter-package-example/demo/` — перед сборкой ISS переносится в `bin\Debug` по правилам вашего скрипта установки.

## Структура

| Путь | Назначение |
|------|------------|
| `manifest.json` | Метаданные; задайте уникальный `id` (`[a-z0-9_-]+`) |
| `BootData/Environment/*.yaml` | Образцы рецептов и триггеров (seed проекта симбионта) |
| `schema/*.json` | Описание capability для студии (combobox, post-MVP) |
| `runtime/` | Сюда DLL после сборки host (для «Проверить» и инсталлятора) |

## Быстрый старт

1. Откройте каркас: AIStudio → **Зарегистрированные пакеты…** → **Каркас demo…**
2. Скопируйте каталог, например `D:\Work\my-env-adapter\`.
3. В `manifest.json` — свой `id`, `displayName`, `author`.
4. Заполните `schema\`, соберите host → DLL в `runtime\`.
5. **Зарегистрировать из папки…** → **Проверить**.

Подробнее — `docs/AdapterAuthorGuide.md`. Эталон Velum — `docs/adapter-package-example/velum/`.
