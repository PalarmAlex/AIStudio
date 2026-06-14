Каталог BootData\Environment\ — образцы YAML каталогов среды для нового проекта симбионта (contract 3.0).

При создании проекта или «дополнить BootData из пакета» студия копирует файлы в BootData проекта
(без перезаписи уже существующих). Дальше редакторы «Среда» работают с YAML проекта, не с пакетом.

Файлы
-----

EnvironmentRecipes.yaml

  Каталог рецептов: исполняемая моторика host, привязка expression_pattern_id (Expression channel).

  Корневой ключ recipes: — массив рецептов. Запуск — Genetic L3 + IHostMotorDispatcher.

  Минимальный каркас:

    recipes: []

  Пример (demo):

    recipes:
      - id: doc_props_on_save
        display_name: Демо рецепт
        expression_pattern_id: 201
        reactive_eligible: true
        recommended_trigger_keys: [demo.on_save]
        steps:
          - type: invoke
            handler: demo_log
            message: ok

  Удалено (contract 3.0): adaptive_action_id, recommended_trigger_influence_ids.

EnvironmentTriggers.yaml

  Каталог триггеров: mechanical path (homeostasis_deltas) + expr:env.* (reflex_trigger_expression_pattern_id).

  Корневой ключ triggers: — массив. На триггер — одно поле event и опциональные параметры.

  Минимальный каркас:

    triggers: []

  Пример (demo):

    triggers:
      - id: demo.on_save
        display_name: После сохранения
        event: document_saved
        homeostasis_deltas:
          - parameter_id: 3
            delta: 1.0
        reflex_trigger_expression_pattern_id: 101

  Удалено (contract 3.0): influence_action_id.

  command_before — только Command buffer host; mechanical deltas — на отдельных event (например document_saved).

Формат
------

SymbiontEnv.Contract (EnvironmentYamlCodec), contractVersion 3.0 в manifest.json.
Норма полей — docs/AdapterContract.md, docs/AdapterAuthorGuide.md (v1.0).
Архитектура — SymbiontArchitecture_OperatorEnvironment_Spec.md v2.2 (Velum docs).

Шаги invoke: handler + flat-ключи argsSchema (строковый args не поддерживается).

«Проверить»: разбор YAML; legacy ключи adaptive_action_id / influence_action_id — Error.
