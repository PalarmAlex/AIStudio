Каталог BootData\Environment\ — образцы YAML каталогов среды для нового проекта симбионта.При создании проекта или «дополнить BootData из пакета» студия копирует файлы в BootData проекта(без перезаписи уже существующих). Дальше редакторы «Среда» работают с YAML проекта, не с пакетом.
Файлы
-----
EnvironmentRecipes.yaml
  Каталог рецептов среды: исполняемая моторика, привязка к адаптивным действиям (adaptive_action_id).
  Корневой ключ recipes: — массив рецептов.
  Минимальный каркас:
    recipes: []
  Пример одного рецепта (сокращённо):
    recipes:
      - id: demo.set_property
        display_name: Установить свойство
        adaptive_action_id: 1
        risk_tier: A
        reactive_eligible: true
        preconditions:
          document_kinds: [part]
          not_read_only: true
        steps:
          - type: set_property
            name: "ISIDA_Demo"
            template: "ok"
EnvironmentTriggers.yaml
  Каталог триггеров: событие среды → influence_action_id (воздействие на гомеостаз).
  Корневой ключ triggers: — массив триггеров.
  Минимальный каркас:
    triggers: []
  Пример одного триггера (сокращённо):
    triggers:
      - id: demo.on_save
        display_name: После сохранения
        influence_action_id: 2
        document_kinds: [part, assembly]
        detect:
          - kind: document_saved
            enabled: true
Формат
------
Совместим с SymbiontEnv.Contract (EnvironmentYamlCodec) и contractVersion в manifest.json.
Подробная норма полей — AdapterContract.md / AdapterAuthorGuide.md.
Проверка «Проверить» в студии: при наличии файлов проверяется разбор YAML (предупреждения, не блокирует регистрацию).