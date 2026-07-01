# NJsonSchema — генерация C# / TypeScript кода из схемы

## Что делает

Обратная операция к schema generation. Берёт JSON Schema и генерирует эквивалентные классы/интерфейсы в целевом языке.

Используется:
- **Внутри NSwag** — для генерации DTO-классов, включаемых в HTTP-клиент
- **Отдельно** — если нужно сгенерировать модель из внешней JSON Schema (например, schema из отдельного файла — contract-first подход)

## Простейший пример (C#)

```csharp
var schema = await JsonSchema.FromJsonAsync(jsonSchemaText);

var settings = new CSharpGeneratorSettings
{
    Namespace = "MyApp.Models",
    ClassStyle = CSharpClassStyle.Poco
};

var generator = new CSharpGenerator(schema, settings);
string csharpCode = generator.GenerateFile();
```

Из схемы:

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "integer" },
    "name": { "type": "string" }
  }
}
```

Получим:

```csharp
namespace MyApp.Models
{
    public partial class Anonymous
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
```

## Ключевые классы

| Функция | Класс | Проект |
|---|---|---|
| C# генератор | `CSharpGenerator` | `NJsonSchema.CodeGeneration.CSharp` |
| C# разрешение имён типов | `CSharpTypeResolver` | там же |
| C# настройки | `CSharpGeneratorSettings` | там же |
| TS генератор | `TypeScriptGenerator` | `NJsonSchema.CodeGeneration.TypeScript` |
| TS разрешение имён типов | `TypeScriptTypeResolver` | там же |
| TS настройки | `TypeScriptGeneratorSettings` | там же |
| Общая база | `GeneratorBase`, `TypeResolverBase` | `NJsonSchema.CodeGeneration` |

## Настройки C#: `CSharpGeneratorSettings`

| Поле | Что делает |
|---|---|
| `Namespace` | Namespace для генерируемых классов |
| `ClassStyle` | `Poco`, `Inpc` (INotifyPropertyChanged), `Prism`, `Record` |
| `JsonLibrary` | `NewtonsoftJson` или `SystemTextJson` — какие атрибуты генерировать (`[JsonProperty]` vs `[JsonPropertyName]`) |
| `DateType` | Тип для JSON date: `System.DateTime`, `System.DateOnly`, кастомный |
| `DateTimeType` | Аналогично для date-time: `System.DateTime`, `System.DateTimeOffset` |
| `TimeType` | Для JSON time |
| `TimeSpanType` | Как маппить duration |
| `ArrayType` | `System.Collections.Generic.List`, `ICollection`, `IEnumerable`, `Array` |
| `DictionaryType` | `Dictionary`, `IDictionary`, `IReadOnlyDictionary` |
| `GenerateDataAnnotations` | Добавлять ли `[Required]`, `[MaxLength]` |
| `GenerateJsonMethods` | Добавлять `ToJson()`/`FromJson()` статические методы |
| `HandleReferences` | Использовать ли `$ref` — для циклических графов |
| `GenerateOptionalPropertiesAsNullable` | Optional свойства делать nullable |
| `GenerateNullableReferenceTypes` | Использовать nullable reference types (`string?`) |
| `EnumNameGenerator` | Как называть enum-члены |
| `PropertyNameGenerator` | Как называть свойства (PascalCase из camelCase) |
| `TemplateFactory` | Замена шаблонов |

## Настройки TypeScript: `TypeScriptGeneratorSettings`

| Поле | Что делает |
|---|---|
| `TypeStyle` | `Interface`, `Class`, `KnockoutClass` |
| `ExtensionCode` | Дополнительный код, включаемый в файл |
| `EnumStyle` | `Enum` (TS enum) или `StringLiteral` |
| `MarkOptionalProperties` | `?` для optional свойств |
| `ExportTypes` | Использовать `export` перед классами |
| `ModuleName` | Помещать в `module { ... }` |
| `NamespaceName` | Помещать в `namespace { ... }` |
| `DateTimeType` | `Date`, `string`, `moment`, `luxon`, `offsetMomentJS` |
| `NullValue` | `null` или `undefined` для null values |
| `HandleReferences` | Циклические ссылки |
| `TemplateFactory` | Замена шаблонов |

## TypeResolver

TypeResolver — центральный сервис в code generation. Отвечает за:

1. **Разрешение JSON Schema → имя целевого языка**
   - `{ "type": "string" }` → `string` (C#) / `string` (TS)
   - `{ "type": "integer", "format": "int64" }` → `long` / `number`
   - `{ "type": "array", "items": {...} }` → `List<T>` / `T[]`
   - `{ "$ref": "#/definitions/User" }` → `User` (регистрирует UserDto для генерации, если ещё не было)

2. **Уникальность имён**
   - Если несколько inline-схем анонимные → создаёт `Anonymous`, `Anonymous2`, `Anonymous3`
   - Если явное имя конфликтует — суффиксы

3. **Регистрация типов для генерации**
   - Все встреченные reference-типы регистрируются, потом генерируются в один файл (или разные — зависит от settings)

## Liquid-шаблоны

**C# (`NJsonSchema.CodeGeneration.CSharp/Templates/`)**:

- `Class.liquid` — класс DTO
- `Class.Inpc.liquid` — INPC-вариант
- `Class.Record.liquid` — record-вариант (C# 9+)
- `Enum.liquid` — enum
- `File.liquid` — обёртка файла (namespace, using'и)

**TypeScript (`NJsonSchema.CodeGeneration.TypeScript/Templates/`)**:

- `Class.liquid`, `Interface.liquid`
- `Enum.liquid`
- `File.liquid`

Все шаблоны embedded в бинарь.

Кастомизация через `ITemplateFactory` (см. [nswag-extension-points.md](nswag-extension-points.md) — механизм одинаковый).

## Class Style (C#)

### `CSharpClassStyle.Poco` (default)

```csharp
public partial class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

### `CSharpClassStyle.Inpc`

Реализует `INotifyPropertyChanged`, генерирует backing fields + `OnPropertyChanged` вызовы. Для WPF/Xamarin.

### `CSharpClassStyle.Prism`

Использует `BindableBase` из Prism (Prism.Mvvm) — для WPF/Xamarin с Prism.

### `CSharpClassStyle.Record` (C# 9+)

```csharp
public partial record User
{
    public int Id { get; init; }
    public string Name { get; init; }
}
```

## JsonLibrary (C#)

Управляет тем, какие атрибуты навешивать на свойства:

- **`NewtonsoftJson`** (default):
  ```csharp
  [JsonProperty("id", Required = Required.Always)]
  public int Id { get; set; }
  ```
- **`SystemTextJson`**:
  ```csharp
  [JsonPropertyName("id")]
  public int Id { get; set; }
  ```

Также влияет на `Required` handling и converter'ы (StringEnumConverter vs JsonStringEnumConverter).

## Пример TypeScript (Interface)

Из схемы:
```json
{
  "type": "object",
  "properties": {
    "id": { "type": "integer" },
    "roles": { "type": "array", "items": { "type": "string" } },
    "createdAt": { "type": "string", "format": "date-time" }
  },
  "required": ["id"]
}
```

Получим (при `TypeStyle: Interface`):

```typescript
export interface User {
    id: number;
    roles?: string[];
    createdAt?: Date;
}
```

## Approval-тесты

Тесты в `NJsonSchema.CodeGeneration.CSharp.Tests` и `NJsonSchema.CodeGeneration.TypeScript.Tests` работают через сравнение с сохранёнными файлами:

- Меняешь генератор → snapshot ломается
- Смотришь diff → обновляешь `.approved.cs` (если изменение ожидаемое)

Это защищает от случайных regression'ов и делает изменения в генерации явными в PR.

## Связь с NSwag

NSwag не переопределяет DTO-генерацию — он использует `CSharpGenerator` / `TypeScriptGenerator` из NJsonSchema. NSwag только:

1. Загружает `OpenApiDocument`
2. Обходит `paths.*.operations.*.parameters` и `responses`
3. Для каждой встреченной schema — регистрирует её в `CSharpTypeResolver`
4. В конце — TypeResolver отдаёт список типов, NSwag'овский `CSharpClientGenerator` рендерит клиента, а DTO-классы генерирует NJsonSchema

Итого: если тебе нужно поменять как выглядит DTO — это NJsonSchema settings, а не NSwag.
