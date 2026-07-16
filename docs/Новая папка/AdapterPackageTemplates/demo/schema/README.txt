Каталог schema\ — машиночитаемое описание возможностей адаптера для AIStudio (contract 3.2).

Студия читает эти JSON при редактировании проекта симбионта (редакторы «Среда», combobox ProbeKey
в справочнике «Давление среды на виталы» — EnvironmentPressureRules.dat). Runtime host DLL для этого не нужен.

Версия формата: schemaVersion 3.2 (совпадает с contractVersion 3.2 в manifest.json).

-------------------------------------------------------------------------------------

Обязательные файлы (для «Проверить» без Error):

handlers-catalog.json
  Handler'ы для шагов type: invoke в рецептах.
  Массив handlers[]: id, label, description, argsSchema[] (key, label, type, required, values, editorHint).
  editorHint: template_placeholder | property_name — кнопки справочника в редакторе шагов.

recipe-catalog.json
  Каталог допустимых ID рецептов для редактора рецептов среды.
  Массив recipes[]: id (обязателен), label, description.

command-buffer-policy.json
  Defaults Command idle-flush для host (Velum: seed в Velum.Settings.xml).
  Поля: idle_flush_ms, max_tokens, max_age_ms.

metric-probes.json
  Ключи ProbeKey для EnvironmentPressureRules.dat.
  Массив probes[]: key (обязателен), label, description.

Опциональный файл:

recipe-template-catalog.json
  Справочник масок {PLACEHOLDER} (placeholders[]) и имён свойств документа (propertyNames[]).
  Используется кнопками «Вставить…» / «Справочник…» в редакторе шагов рецепта.
  Runtime Velum разрешает шаблоны в коде (VelumRecipeTemplateResolver); этот JSON — только для UI студии.

Шаги рецепта в YAML:
  - type: invoke — handler + flat-ключи из argsSchema
  - type: comment — text (пропускается runtime)

Удалено (contract 3.2):
  trigger-detect.json, trigger-catalog.json, expression-pattern-catalog.json.
  EnvironmentTriggers.yaml; expression_pattern_id; recommended_trigger_keys.

Правило
-------

Каждый handler id / recipe id / ProbeKey в schema должен согласовываться с boot проекта и runtime host Velum.
Dispatch рецепта — по adaptive_action_id (G_AD) на OnPulseCompleted, не по expression pattern.

«Проверить»: schema\, валидный JSON, обязательные массивы
(handlers, recipes, probes) + command-buffer-policy.json.
Наличие trigger-detect.json или trigger-catalog.json — Error.
