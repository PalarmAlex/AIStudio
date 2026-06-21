Каталог BootData\Environment\ — образцы YAML каталогов среды для нового проекта симбионта (contract 3.2).

При создании проекта или «дополнить BootData из пакета» студия копирует файлы в BootData проекта
(без перезаписи уже существующих). Дальше редакторы «Среда» работают с YAML проекта, не с пакетом.

Файлы
-----

EnvironmentRecipes.yaml

  Каталог рецептов: моторика host по adaptive_action_id (G_AD из AdaptiveActions.dat).

  Корневой ключ schema: environment-recipes/3.2 и recipes: — массив рецептов.
  Dispatch — rising edge ActiveAdaptiveActions на OnPulseCompleted.

  Минимальный каркас:

    schema: environment-recipes/3.2
    recipes: []

  Пример (demo):

    schema: environment-recipes/3.2
    recipes:
      - id: doc_props_on_save
        display_name: Демо рецепт
        adaptive_action_id: 1
        reactive_eligible: true
        steps:
          - type: invoke
            handler: demo_log
            message: ok

  Запрещено (contract 3.2): expression_pattern_id, recommended_trigger_keys,
  genetic_reflex_id, influence_action_id, EnvironmentTriggers.yaml.

Mechanical path и Command-пуск — не в этом YAML:
  - EnvironmentPressureRules.dat (ProbeKey, homeostasis_deltas)
  - GeneticReflexes.dat (command_pattern_ids)

Формат
------

SymbiontEnv.Contract (EnvironmentYamlCodec), contractVersion 3.2 в manifest.json.
Норма полей — docs/AdapterContract.md, docs/AdapterAuthorGuide.md (v2.0).

Шаги invoke: handler + flat-ключи argsSchema (строковый args не поддерживается).
Маски шаблона значения — schema\recipe-template-catalog.json (справочник в UI студии).

«Проверить»: разбор YAML; отсутствие schema: environment-recipes/3.2 или legacy ключи — Error.
Наличие EnvironmentTriggers.yaml в пакете — Error.
