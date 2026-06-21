Каталог schema\ — машиночитаемое описание возможностей адаптера для AIStudio (contract 3.2).

Студия читает JSON при редактировании проекта (редакторы «Среда», ProbeKey в EnvironmentPressureRules.dat).
Runtime host DLL для этого не нужен.

Версия: schemaVersion 3.2 (совпадает с contractVersion 3.2 в manifest.json).

-------------------------------------------------------------------------------------

Обязательные файлы (четыре — для «Проверить» без Error):

handlers-catalog.json
  Handler'ы для шагов type: invoke. Массив handlers[].
  argsSchema[]: опционально editorHint = template_placeholder | property_name.

recipe-catalog.json
  ID рецептов для редактора. Массив recipes[].

metric-probes.json
  Ключи ProbeKey для EnvironmentPressureRules.dat. Массив probes[].

command-buffer-policy.json
  Defaults Command idle-flush для host (Velum: seed в Velum.Settings.xml).
  Поля: idle_flush_ms, max_tokens, max_age_ms.

Опциональный файл:

recipe-template-catalog.json
  Справочник масок {PLACEHOLDER} (placeholders[]) и имён свойств (propertyNames[])
  для кнопок «Вставить…» / «Справочник…» в редакторе шагов рецепта.

Шаги рецепта в YAML:
  - type: invoke — handler + flat-ключи из argsSchema
  - type: comment — text (пропускается runtime)

Удалено (contract 3.2):
  trigger-detect.json, trigger-catalog.json, expression-pattern-catalog.json.
  EnvironmentTriggers.yaml; expression_pattern_id; recommended_trigger_keys.

Правило
-------

Каждый handler id / recipe id / ProbeKey в schema должен согласовываться с boot проекта
(AdaptiveActions.dat, EnvironmentPressureRules.dat) и runtime host (G_AD dispatch).

«Проверить»: schema\, валидный JSON, обязательные массивы
(handlers, recipes, probes) + command-buffer-policy.json.
Наличие trigger-detect.json или trigger-catalog.json — Error.
