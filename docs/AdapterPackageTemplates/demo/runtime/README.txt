Стартовый SDK (isida.dll, SymbiontEnv.Contract.dll, Newtonsoft.Json.dll) кладёт установщик AIStudio.
При «Создать пакет…» студия копирует SDK из этого каталога; при отмене выбора host — регистрируется пакет только с SDK.

После сборки host добавьте DLL в Adapters\{id}\runtime\ или укажите bin\Debug при повторном создании пакета.
