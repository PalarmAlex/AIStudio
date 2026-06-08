Каталог schema\ — машиночитаемое описание возможностей адаптера для AIStudio.

Студия читает эти JSON при редактировании проекта симбионта (редакторы «Среда», combobox ProbeKey
в справочнике «Давление среды на виталы» — EnvironmentPressureRules.dat). Runtime host DLL для этого не нужен.

Версия формата: schemaVersion 2.0 (совпадает с contractVersion 2.0 в manifest.json).
-------------------------------------------------------------------------------------
Обязательные файлы (все шесть должны быть в пакете для «Проверить» без Error):
recipe-preconditions.json
  Допустимые поля блока preconditions в рецептах (чекбоксы/списки в редакторе рецептов).
  Массив fields[]: key, label, type, enumValues (для stringList), required, default.
  Пример элемента:
    { "key": "not_read_only", "label": "Не read-only", "type": "bool", "required": false, "default": false }
recipe-steps.json
  Допустимые type шагов рецепта и параметры для редактора вкладки «Шаги».
  Массив stepTypes[]: type, label, runtimeType (опционально), parameters[].
  Для enum-полей укажите values: ["active", "document", "default"].
recipe-catalog.json
  Каталог допустимых ID рецептов для редактора рецептов среды.
  Массив recipes[]: id (обязателен), label, description.
trigger-filter.json
  Поля фильтра контекста триггера (типы документа и т.п.).
  Формат как у recipe-preconditions.json — массив fields[].
trigger-detect.json
  Допустимые kind правил detect в триггерах.
  Массив detectKinds[]: kind, label.
  Пример:
    { "kind": "document_saved", "label": "Документ сохранён" }
metric-probes.json
  Ключи ProbeKey для EnvironmentPressureRules.dat (давление метрик на виталы на пульсе).
  Массив probes[]: key (обязателен), label, description.
  Пример:
    {
      "key": "host.active_doc_modified",
      "label": "Документ изменён",
      "description": "Активный документ имеет несохранённые изменения"
    }

Правило
-------
Каждый key / type / kind в schema должен поддерживаться вашим runtime host.
Если файл отсутствует или пуст, студия для соответствующего редактора использует пустой список.
«Проверить»: наличие schema\, валидный JSON, обязательные массивы (fields, stepTypes, detectKinds, probes, recipes).
