# План рефакторинга: разделение стимулов и давления среды

**Версия:** 1.1  
**Дата:** 2026-06-07  
**Статус:** завершено  
**Область:** Velum (`D:\VELUM\Velum`), AIStudio (`D:\ISIDA\Programms\app\AIStudio`), ISIDA (`D:\ISIDA\Programms\isida`), данные проектов (`%ProgramData%\VELUM`, `%ProgramData%\ISIDA\Adapters\`), пакеты адаптеров.

**Принцип внедрения:** без обратной совместимости со старым 6-столбцовым `InfluenceActions.dat` и без fallback на `EnvironmentMetricProbeKey`. Проект в стадии развития — новый формат данных и прямое обновление файлов.

---

## 1. Проблема

Сейчас один справочник `InfluenceActions.dat` совмещает **три разные сущности**:

| Сущность | Пример ID | Как применяется | Роль |
|----------|-----------|-----------------|------|
| Операторский стимул | 1–100 | `ApplyMultipleInfluenceActions` | рефлексы, пульт |
| Давление метрики SW | 13–21 | пульсовой composer → запись виталов | только гомеостаз |
| Прокси-триггер SW | 101+ | `ApplyMultipleInfluenceActions` (из YAML) | рефлексы, события SW |

Разделение опиралось только на поле `EnvironmentMetricProbeKey`, которое смешивало смыслы и давало ложное впечатление, что «среда» на пульте = метрики.

**Цель рефакторинга:** две независимые модели данных и два runtime-контура без смешения смыслов.

---

## 2. Целевая архитектура

### 2.1. Два справочника

```
┌─────────────────────────────────────┐     ┌──────────────────────────────────┐
│  InfluenceActions.dat               │     │  EnvironmentPressureRules.dat     │
│  (стимулы / дискретные воздействия) │     │  (давление метрик на виталы)      │
├─────────────────────────────────────┤     ├──────────────────────────────────┤
│  ApplyMultipleInfluenceActions      │     │  VelumSolidEnvironmentInfluence   │
│  → PerceptionImage                  │     │  Composer → HostBatchUpdate       │
│  → TriggerStimulusActivated         │     │  (только на пульсе)               │
│  → GeneticReflex Level3             │     │  без рефлексов                    │
└─────────────────────────────────────┘     └──────────────────────────────────┘
         ▲                                              ▲
         │                                              │
   Оператор / YAML-триггеры                    Опрос probe_key в SW
   (EnvironmentTriggers.yaml)                  (metric-probes.json)
```

**InfluenceActions.dat** (имя файла сохраняем для ISIDA) содержит **только стимулы**.  
**EnvironmentPressureRules.dat** — новый файл; строки 13–21 из текущего VELUM переезжают сюда.

### 2.2. Третий слой — привязка событий (без изменения сути)

`EnvironmentTriggers.yaml` по-прежнему связывает событие SW → `influence_action_id` (стимул).  
Это **не** справочник стимулов, а каталог детекторов.

### 2.3. Пульт: две колонки стимулов (не метрик)

На пульте AIStudio и Velum Task Pane — **две колонки только из `InfluenceActions.dat`**, разбивка по наличию ссылки в `EnvironmentTriggers.yaml`:

| Колонка | Содержимое | Смысл для оператора |
|---------|------------|---------------------|
| **Прямое воздействие** | стимулы, **не** указанные ни в одном триггере YAML | только оператор шлёт вручную |
| **Через среду** | стимулы, на которые есть запись в `EnvironmentTriggers.yaml` | оператор может послать вручную **и** среда может имитировать то же (Save, команда SW и т.д.) |

Обе колонки при отправке вызывают один API: `ApplyMultipleInfluenceActions`.  
**EnvironmentPressureRules** на пульте **не показываются** — это фоновое давление, не кнопки.

Классификация колонок — **вычисляемая** при загрузке:

```csharp
var yamlBoundStimulusIds = SwTriggerCatalog.GetAllReferencedInfluenceActionIds();
directOnly   = stimuli.Where(s => !yamlBoundStimulusIds.Contains(s.Id));
viaEnvironment = stimuli.Where(s =>  yamlBoundStimulusIds.Contains(s.Id));
```

### 2.4. Правила использования

| Вопрос | Ответ |
|--------|--------|
| Можно ли указать pressure rule в Level3 рефлекса? | **Нет** (валидация в Studio) |
| Сработает ли рефлекс при изменении метрики? | **Нет** — только через явный стимул |
| Может ли оператор вручную послать стимул из колонки «Через среду»? | **Да** |
| Может ли Velum послать стимул без YAML? | **Нет** — только ID из триггеров |
| Антагонисты между pressure rules и стимулами? | **Нет** — только внутри одного справочника |

---

## 3. Форматы данных

### 3.1. `InfluenceActions.dat` (после рефакторинга)

```
# Формат: ID|Имя|Описание|Воздействие|Антагонисты
# Воздействие: paramId1:effect1;paramId2:effect2  (пусто — чистый пусковой стимул)
# Антагонисты: id1,id2,id3
# Только дискретные стимулы (оператор и/или YAML-триггеры). Метрики среды — в EnvironmentPressureRules.dat
```

**Удалён столбец:** `EnvironmentMetricProbeKey` (свойство удалено из `InfluenceActionSystem`, не `[Obsolete]`).

**Рекомендуемые диапазоны ID** (валидация в Studio, не в ISIDA):

| Диапазон | Назначение |
|----------|------------|
| 1–100 | операторские стимулы (обычно не в YAML) |
| 101–1000 | прокси для YAML-триггеров |

### 3.2. `EnvironmentPressureRules.dat` (новый)

Путь по умолчанию: `{DataActionsFolderPath}\EnvironmentPressureRules.dat`

```
# Формат: RuleId|ProbeKey|Имя|Описание|Influences|Antagonists
# ProbeKey: ключ из schema/metric-probes.json пакета адаптера
# Influences: paramId1:effect1;paramId2:effect2  (шаблон при ухудшении метрики)
# Antagonists: id1,id2  (только между правилами давления)
13|Velum.Solid.Material|Материал в модели|...|1:-1;2:1;...|2,10
```

### 3.3. `EnvironmentTriggers.yaml` (уточнение контракта)

Поле `influence_action_id` остаётся; в документации явно: **ссылка на строку `InfluenceActions.dat`, не на pressure rule**.

### 3.4. Настройки проекта

Добавить в `Settings.xml` / `VelumAppConfig` / `SettingsValidator`:

```xml
<EnvironmentPressureRulesFilePath>{ProjectRoot}\Data\Actions\EnvironmentPressureRules.dat</EnvironmentPressureRulesFilePath>
```

---

## 4. Фазы внедрения

| Фаза | Содержание | Критерий готовности |
|------|------------|---------------------|
| **0** | Документация, ADR, обновление данных в новом формате | этот документ + миграция `.dat` |
| **1** | `EnvironmentPressureRules.dat`; Velum composer читает его | VELUM: метрики работают из нового файла |
| **2** | AIStudio: два редактора; пульт — две колонки по YAML | оператор видит разделение стимулов |
| **3** | ISIDA: 5-столбцовый InfluenceActions; удалён probe key | один столбец меньше в `.dat` |
| **4** | Документация AdapterContract, demo, sldworks_19 | чистая схема для авторов |

**Не делаем:** fallback на старый `.dat`, миграционный wizard, `[Obsolete]` для `EnvironmentMetricProbeKey`.

---

## 5. Velum — затрагиваемые файлы

| Файл | Изменение |
|------|-----------|
| `SolidHomeostasis\EnvironmentPressureRuleSystem.cs` | загрузка `EnvironmentPressureRules.dat` |
| `SolidHomeostasis\EnvironmentPressureRule.cs` | модель строки |
| `SolidHomeostasis\VelumSolidEnvironmentInfluenceComposer.cs` | источник — pressure rules |
| `SolidHomeostasis\VelumSolidEnvironmentBridge.cs` | probe keys из pressure rules |
| `Common\VelumAgentTaskPane.cs` | две колонки по YAML; tooltip из pressure rules |
| `ReactiveCore\SwTriggerCatalog.cs` | `GetAllReferencedInfluenceActionIds()` |
| `Configuration\VelumAppConfig.cs` | `EnvironmentPressureRulesFilePath` |

---

## 6. AIStudio — затрагиваемые файлы

| Файл | Изменение |
|------|-----------|
| `ViewModels\AgentPultViewModel.cs` | split по YAML, не по probe key |
| `Pages\AgentPultView.xaml` | «Прямое воздействие» / «Через среду» |
| `ViewModels\ExterInalInfluencesViewModel.cs` | без probe key; валидация ID 1–100 / 101–1000 |
| `Pages\ExterInalInfluencesView.xaml` | убрать колонку «Ключ метрики среды» |
| `ViewModels\Environment\EnvironmentPressureRulesViewModel.cs` | **новый** CRUD pressure rules |
| `Pages\Environment\EnvironmentPressureRulesView.xaml` | **новый** |
| `Common\ProjectBootstrap.cs` | seed pressure rules; 5-столбцовый InfluenceActions |
| `AppConfig.cs` | `EnvironmentPressureRulesFilePath` |

---

## 7. ISIDA

| Файл | Изменение |
|------|-----------|
| `Actions\InfluenceActionSystem.cs` | 5 столбцов; **удалить** `EnvironmentMetricProbeKey` |
| `Common\FileValidator.cs` | новый заголовок InfluenceActions; валидация pressure rules |
| `Common\SettingsValidator.cs` | `ValidateEnvironmentMetricProbeKey` → для ProbeKey в pressure rules (переименовать сообщения) |

---

## 8. Миграция данных (ручное обновление файлов)

```
1. Строки 13–21 с probe key → EnvironmentPressureRules.dat
2. Остальные → InfluenceActions.dat (5 столбцов)
3. Убрать cross-antagonists между стимулами и бывшими pressure ID
4. Проверить GeneticReflexes Level3 и EnvironmentTriggers.yaml — только stimulus IDs
```

---

## 9. Тест-план

| # | Сценарий | Ожидание |
|---|----------|----------|
| 1 | Save в SW, пульсация вкл | стимул 101, рефлекс Level3=101 |
| 2 | Оператор: «Наказать» (не в YAML) | колонка «Прямое» |
| 3 | Оператор: вручную «SW: Save» (101) | колонка «Через среду», тот же рефлекс что п.1 |
| 4 | Изменение метрики материала | сдвиг виталов, **без** TriggerStimulusActivated |
| 5 | Level3=13 (pressure rule ID) | Studio: ошибка валидации |
| 6 | Пустой EnvironmentPressureRules.dat | предупреждение; стимулы не затронуты |

---

## 10. Чеклист

### Velum
- [x] `EnvironmentPressureRuleSystem` + путь в config
- [x] Composer → pressure rules
- [x] Task Pane: две колонки по YAML
- [x] `SwTriggerCatalog.GetAllReferencedInfluenceActionIds()`
- [x] Миграция ProgramData VELUM

### AIStudio
- [x] Пульт: две колонки стимулов
- [x] Редактор pressure rules
- [x] ExterInalInfluencesView без probe key
- [x] ProjectBootstrap seed
- [x] Валидация Level3 ≠ RuleId pressure rules
- [x] Эталонные данные: стимулы 101+, триггер Save

### ISIDA
- [x] 5-столбцовый InfluenceActions
- [x] Удалён EnvironmentMetricProbeKey
- [x] ValidateEnvironmentProbeKey; отклонение 6-колоночного `.dat`
- [x] EnvironmentPressureRulesFilePath в SettingsValidator

### Документация и пакеты (фаза 4)
- [x] AdapterContract.md / .html
- [x] demo + sldworks_19 schema/README, EnvironmentTriggers.yaml
- [x] ADR_StimulusAndEnvironmentPressure.md

---

## 11. Глоссарий

| Термин | Определение |
|--------|-------------|
| **Стимул** | запись в `InfluenceActions.dat`; `ApplyMultipleInfluenceActions` |
| **Правило давления** | запись в `EnvironmentPressureRules.dat`; probe → виталы на пульсе |
| **YAML-привязка** | `influence_action_id` в `EnvironmentTriggers.yaml` |
| **Прямое воздействие** | стимул без YAML-привязки |
| **Через среду** | стимул с YAML-привязкой |

---

*Конец документа.*
