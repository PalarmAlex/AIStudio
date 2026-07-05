# Давление метрик SolidWorks на параметры гомеостаза

Документ описывает, как Velum переносит «плохие» метрики среды (probe) в значения параметров P_i движка ISIDA на такте `OnPulseBeforeGomeostasis`.

Связанные типы: `VelumSolidMetricPressureOrchestrator`, `VelumSolidEnvironmentInfluenceComposer`, `EnvironmentMetricPressureComposer` / `EnvironmentMetricParameterRelease` (SymbiontEnv.Contract).

## Источники данных

| Слой | Что читает |
|------|------------|
| SolidWorks | Снимок проб по `ProbeKey` из `InfluenceActions.dat` |
| EA среды | Шаблон влияний: `paramId → величина` (−10…+10) |
| Движок | Текущие `Value`, `Speed`, `LastState`, `DynamicTime`, `DifSensorPar` |

Порог «плохая / хорошая» метрика: `MetricProbeThresholds` + `SolidEnvironmentMetricDeltaEpsilon` (Velum.Settings.xml).

## Давление (фаза A)

Пока метрика **плохая**:

1. На такте собираются probe keys, прошедшие cooldown `ReflexActionDisplayDuration`.
2. `EnvironmentMetricPressureComposer` для каждого ключа добавляет к **текущему** значению в движке сумму дельт по параметрам:
   - несколько метрик на **одном пульсе** — дельты **складываются**;
   - composite-метрики — масштаб `YesSlots / TotalSlots`;
   - суммарный сдвиг одного P_i за такт ограничен ±10.
3. Запись в host — через `VelumSolidEnvironmentBridge`; для давления действует фильтр `SolidHostImpulseMinParameterDelta`.

Повторное давление — **инкрементальное** (`текущее + Δ`), не фиксированный «целевой уровень».

### Подпитка состояния «Плохо»

Если P_i ещё в **хорошей зоне** по числу, но на него давят плохие метрики (и нет release-паузы), раз в **`DynamicTime − 1`** пульсов (из настроек гомеостаза) добавляется импульс давления — чтобы в движке удерживалось временное **Bad** без мерцания в Normal.

Cooldown `ReflexActionDisplayDuration` и ритм state-feed работают **независимо** (state-feed может сработать раньше cooldown).

## Release (метрика стала хорошей)

Переход **плохая → хорошая** (`WasBad` и `IsProbeGood`).

### Одна метрика на параметр

Полный возврат P_i в норму по `Speed` (100 / 0 / `NormaWell`) — как раньше.

### Несколько метрик на один параметр

1. **Частичное улучшение**: `Value − Σ templateEffects` от **всех** отпустивших на этом пульсе метрик. Используется **шаблон EA** (без composite scale).
2. **Пауза импульсов**: остальные ещё плохие метрики на этом P_i не дают давления, пока у параметра `LastState == Well` (реестр `VelumSolidMetricPressurePauseRegistry`, per `(paramId, probeKey)`).
3. После истечения удержания Well пауза снимается; давление возобновляется по обычным правилам.

Release-записи **не** проходят фильтр `SolidHostImpulseMinParameterDelta`.

## Валидация EA при сохранении

Для воздействий с `ProbeKey` при `SaveInfluenceActions` каждое ненулевое влияние должно удовлетворять:

`|effect| > GomeostasSystem.DifSensorPar`

Иначе «удар» не достигнет порога значимого изменения в `HomeostasisCalculator`, и временное Well/Bad не возникнет.

## Сброс состояния

Оркестратор, реестр пауз и таймеры state-feed очищаются при:

- остановке пульсации (`VelumSolidMetricPressureOrchestrator.Clear`);
- смене активного документа SW (`VelumSolidDocumentEpisodeReset`);
- ручном отключении метрики в чек-листе (`VelumEnvironmentMetricsPickerForm` → `ClearPausesForProbeKey`).

## Поток на одном пульсе

```
OnPulseBeforeGomeostasis
  → опрос SW (если включён)
  → SyncWithGomeostasis (снять паузы, если Well истёкло)
  → release по paramId (полный / частичный + пауза)
  → давление + state-feed (с учётом пауз per param)
  → ApplyHost: release без minDelta; pressure с minDelta
  → UpdateStateOnly (ISIDA)
```

## Режим наблюдения

При `ObservationMode` записи release и давления в движок не выполняются; логика оркестратора для диагностики может выполняться частично без `HostBatchUpdate`.
