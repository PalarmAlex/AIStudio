Каталог BootData\Environment\ — образцы YAML каталогов среды для нового проекта симбионта.
При создании проекта или «дополнить BootData из пакета» студия копирует файлы в BootData проекта
(без перезаписи уже существующих). Дальше редакторы «Среда» работают с YAML проекта, не с пакетом.
Файлы
-----
EnvironmentRecipes.yaml
  Каталог рецептов среды: исполняемая моторика, привязка к адаптивным действиям (adaptive_action_id).
  Корневой ключ recipes: — массив рецептов. Условия запуска задаются рефлексами ISIDA и триггерами среды.
  Минимальный каркас:
    recipes: []
  Пример одного рецепта (сокращённо):
    recipes:
      - id: doc_props_on_save
        display_name: Демо рецепт
        adaptive_action_id: 1
        reactive_eligible: true
        steps:
          - type: invoke
            handler: demo_log
            message: ok
          - type: comment
            text: "Демо-комментарий в рецепте"
EnvironmentTriggers.yaml
  Каталог триггеров: одно событие среды → influence_action_id (воздействие на гомеостаз).
  Корневой ключ triggers: — массив триггеров. На каждый триггер — одно поле event и опциональные параметры события.
  Минимальный каркас:
    triggers: []
  Пример одного триггера (сокращённо):
    triggers:
      - id: demo.on_save
        display_name: После сохранения
        influence_action_id: 2
        event: document_saved
Формат
------
Совместим с SymbiontEnv.Contract (EnvironmentYamlCodec), contractVersion 2.0 в manifest.json.
Подробная норма полей — docs/AdapterContract.md / docs/AdapterAuthorGuide.md.
Шаги invoke: handler + flat-ключи argsSchema (строковый args не поддерживается).
«Проверить»: при наличии файлов проверяется разбор YAML (предупреждения, не блокирует регистрацию).
