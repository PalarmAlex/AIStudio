Каталог schema\ — машиночитаемое описание возможностей адаптера для AIStudio (contract 3.0).

Студия читает JSON при редактировании проекта (редакторы «Среда», ProbeKey в EnvironmentPressureRules.dat).
Runtime host DLL для этого не нужен.

Версия: schemaVersion 3.0 (совпадает с contractVersion 3.0 в manifest.json).

-------------------------------------------------------------------------------------

Обязательные файлы (шесть — для «Проверить» без Error):

handlers-catalog.json
  Handler'ы для шагов type: invoke. Массив handlers[].
  argsSchema[]: опционально editorHint = template_placeholder | property_name.

trigger-detect.json
  Допустимые event в триггерах. Массив detectKinds[].

trigger-catalog.json
  ID триггеров для редактора. Массив triggers[].

recipe-catalog.json
  ID рецептов для редактора. Массив recipes[].

expression-pattern-catalog.json   [обязателен в 3.0]
  Паттерны Expression channel: expr:demo.env.*, expr:demo.recipe.* и т.д.
  Массив patterns[]: id, token, label, description, kind.
  Используется для picker expression_pattern_id и reflex_trigger_expression_pattern_id.

metric-probes.json
  Ключи ProbeKey для EnvironmentPressureRules.dat. Массив probes[].

Опциональный файл:

recipe-template-catalog.json
  Справочник масок {PLACEHOLDER} (placeholders[]) и имён свойств (propertyNames[])
  для кнопок «Вставить…» / «Справочник…» в редакторе шагов рецепта.
  У каждой записи: token/name, label (кратко), description (пояснение в диалоге студии).
  Связан с editorHint в handlers-catalog.json. Runtime host может не читать этот файл.

Шаги рецепта в YAML:
  - type: invoke — handler + flat-ключи из argsSchema
  - type: comment — text (пропускается runtime)

Удалено (contract 3.0):
  adaptive_action_id, influence_action_id в YAML.
  Operator path — HomeostasisSignificance + каналы (не InfluenceActions.dat).

Правило
-------

Каждый handler id / event kind / pattern id в schema должен согласовываться с boot проекта
(DefaultExpressionPrimaries.tmp) и runtime host (IHostMotorDispatcher).

«Проверить»: schema\, валидный JSON, обязательные массивы
(handlers, detectKinds, triggers, recipes, patterns, probes).
