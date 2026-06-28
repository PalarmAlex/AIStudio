Каталог schema\ — машиночитаемое описание возможностей адаптера для AIStudio.

Студия читает эти JSON при редактировании проекта симбионта (редакторы «Среда», combobox ProbeKey

в справочнике «Давление среды на виталы» — EnvironmentPressureRules.dat). Runtime host DLL для этого не нужен.

Версия формата: schemaVersion 2.0 (совпадает с contractVersion 2.0 в manifest.json).

-------------------------------------------------------------------------------------

Обязательные файлы (все пять должны быть в пакете для «Проверить» без Error):

handlers-catalog.json

  Допустимые handler'ы для шагов type: invoke в рецептах.

  Массив handlers[]: id, label, description, argsSchema[] (key, label, type, required, values).

  Аргументы в YAML — flat-ключи на уровне шага (не строковый args).

trigger-detect.json

  Допустимые типы события event в триггерах.

  Массив detectKinds[]: kind, label, parameters[] (опционально, для параметров события).

trigger-catalog.json

  Каталог допустимых ID триггеров для редактора триггеров среды.

  Массив triggers[]: id (обязателен), label, description.

recipe-catalog.json

  Каталог допустимых ID рецептов для редактора рецептов среды.

  Массив recipes[]: id (обязателен), label, description.

metric-probes.json

  Ключи ProbeKey для EnvironmentPressureRules.dat (давление метрик на виталы на пульсе).

  Массив probes[]: key (обязателен), label, description.

Шаги рецепта в YAML:

  - type: invoke — handler + flat-ключи из argsSchema

  - type: comment — text (пропускается runtime)

  Legacy: строковый args и single-recipe в корне YAML не поддерживаются.

Правило

-------

Каждый handler id / kind в schema должен поддерживаться вашим runtime host.

Если файл отсутствует или пуст, студия для соответствующего редактора использует пустой список.

«Проверить»: наличие schema\, валидный JSON, обязательные массивы (handlers, detectKinds, triggers, recipes, probes).

