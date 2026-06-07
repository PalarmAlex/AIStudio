Каталог BootData\Environment\ — образцы YAML каталогов среды для нового проекта симбионта.
При создании проекта или «дополнить BootData из пакета» студия копирует файлы в BootData проекта
(без перезаписи уже существующих). Дальше редакторы «Среда» работают с YAML проекта, не с пакетом.

Файлы
-----

EnvironmentRecipes.yaml

  Каталог рецептов среды: исполняемая моторика, привязка к адаптивным действиям (adaptive_action_id).
  Корневой ключ recipes: — массив рецептов.

  Минимальный каркас:

    recipes: []

  Пример одного рецепта (сокращённо):

    recipes:
      - id: doc_props_on_save
        display_name: Свойства при сохранении
        adaptive_action_id: 1
        risk_tier: B
        reactive_eligible: true
        preconditions:
          document_kinds: [document, project]
          not_read_only: true
        steps:
          - type: set_property
            name: "ISIDA_Demo"
            template: "ok"

EnvironmentTriggers.yaml

  Каталог триггеров: событие среды → influence_action_id (воздействие на гомеостаз).
  Корневой ключ triggers: — массив триггеров.
  document_kinds — на уровне триггера (не вложенный filter).

  Минимальный каркас:

    triggers: []

  Пример одного триггера (сокращённо):

    triggers:
      - id: on_document_saved
        display_name: После сохранения
        influence_action_id: 2
        document_kinds: [document, project]
        detect:
          - kind: document_saved
            environment: my-adapter
            enabled: true

Формат
------
Совместим с SymbiontEnv.Contract (EnvironmentYamlCodec), contractVersion 2.0 в manifest.json.
Подробная норма полей — docs/AdapterContract.md / docs/AdapterAuthorGuide.md.
«Проверить»: при наличии файлов проверяется разбор YAML (предупреждения, не блокирует регистрацию).
