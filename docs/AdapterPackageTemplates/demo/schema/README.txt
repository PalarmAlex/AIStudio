Каталог schema\ — машиночитаемое описание возможностей адаптера для AIStudio.
Студия читает эти JSON при редактировании проекта симбионта (редакторы «Среда», combobox ключей
метрик в справочнике воздействий). Runtime host DLL для этого не нужен.
Файлы (schemaVersion 1.0)
-------------------------
recipe-preconditions.json
  Допустимые поля блока preconditions в рецептах (чекбоксы/списки в редакторе рецептов).
  Массив fields[]: key, label, type, enumValues (для stringList), required, default.
  Пример элемента:
    { "key": "not_read_only", "label": "Не read-only", "type": "bool", "required": false, "default": false }
recipe-steps.json
  Допустимые type шагов рецепта (combobox типа шага).
  Массив stepTypes[]: type, label.
  Пример:
    { "type": "set_property", "label": "Установить свойство" }
trigger-filter.json
  Поля фильтра контекста триггера (какие типы документа и т.п.).
  Формат как у recipe-preconditions.json — массив fields[].
trigger-detect.json
  Допустимые kind правил detect в триггерах.
  Массив detectKinds[]: kind, label.
  Пример:
    { "kind": "document_saved", "label": "Документ сохранён" }
metric-probes.json
  Ключи EnvironmentMetricProbeKey для таблицы воздействий на гомеостаз (связь метрик среды со справочником).
  Массив probes[]: key (обязателен), label, description.
  Пустой key в таблице воздействий — воздействие оператора (пульт), не среды.
  Пример:
    {
      "key": "host.active_doc_modified",
      "label": "Документ изменён",
      "description": "Активный документ имеет несохранённые изменения"
    }
Правило
-------
Каждый key / type / kind в schema должен поддерживаться вашим runtime host.
Если файл отсутствует или пуст, студия для соответствующего редактора использует fallback или пустой список.
Проверка «Проверить» в студии: наличие schema\, валидный JSON и обязательные поля в массивах.