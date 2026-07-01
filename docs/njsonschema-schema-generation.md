# NJsonSchema — генерация схемы из .NET-типов

## Что делает

Берёт .NET-тип (класс/enum/interface) и через рефлексию создаёт эквивалентный `JsonSchema`. Используется:

- Внутри NSwag — для описания request/response DTO в OpenAPI-документе
- Отдельно — если нужно опубликовать JSON Schema для своего API (например, для валидации на клиенте)

## Простейший пример

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

var schema = JsonSchema.FromType<User>();
Console.WriteLine(schema.ToJson());
```

Результат:

```json
{
  "type": "object",
  "properties": {
    "Id": { "type": "integer", "format": "int32" },
    "Name": { "type": "string" },
    "CreatedAt": { "type": "string", "format": "date-time" }
  }
}
```

## Ключевой класс

**`JsonSchemaGenerator`** (`src/NJsonSchema/Generation/JsonSchemaGenerator.cs`)

```csharp
var settings = new JsonSchemaGeneratorSettings
{
    SerializerSettings = new JsonSerializerSettings { /* ... */ }
};

var generator = new JsonSchemaGenerator(settings);
var schema = generator.Generate(typeof(User));
```

## Настройки: `JsonSchemaGeneratorSettings`

| Поле | Что делает |
|---|---|
| `SchemaType` | `Swagger2`, `OpenApi3`, `JsonSchema` — влияет на нюансы формата |
| `SerializerSettings` | Настройки JSON-сериалайзера — определяют имена полей (camelCase / PascalCase) |
| `DefaultReferenceTypeNullHandling` | Null / NotNull — как трактовать reference-типы (по умолчанию Null для C#) |
| `DefaultDictionaryValueReferenceTypeNullHandling` | То же для значений в Dictionary |
| `GenerateAbstractProperties` | Включать ли абстрактные свойства |
| `FlattenInheritanceHierarchy` | Разворачивать наследование в один тип vs использовать `allOf` |
| `GenerateKnownTypes` | Обрабатывать ли `[KnownType]` |
| `GenerateEnumMappingDescription` | Добавлять описания к enum-значениям |
| `IgnoreObsoleteProperties` | Пропускать ли `[Obsolete]` свойства |
| `TypeMappers` | Кастомные mapper'ы (см. ниже) |
| `SchemaProcessors` | Пост-обработка сгенерированной схемы |
| `ReflectionService` | `NewtonsoftJsonReflectionService` или `SystemTextJsonReflectionService` |

## Reflection Service

Ключевая абстракция — как читать метаданные типа. Два готовых сервиса:

### `NewtonsoftJsonReflectionService`

- Пакет: `NJsonSchema.NewtonsoftJson`
- Читает атрибуты **Newtonsoft.Json**: `[JsonProperty("name")]`, `[JsonIgnore]`, `[JsonConverter]`, `[JsonRequired]`
- Использует `JsonContract` для определения имён свойств (учитывая `NamingStrategy`)

### `SystemTextJsonReflectionService`

- Встроен в `NJsonSchema` (для net6.0+)
- Читает атрибуты **System.Text.Json**: `[JsonPropertyName("name")]`, `[JsonIgnore]`, `[JsonConverter]`
- Использует `JsonSerializerOptions.PropertyNamingPolicy`

Выбирается через `settings.ReflectionService = ...`. Если сериализация в проекте — Newtonsoft, тебе нужен Newtonsoft-service, иначе имена свойств в схеме и в реальном JSON разойдутся.

## Type Mappers

Позволяют переопределить, как конкретный .NET-тип отображается в JSON Schema:

```csharp
settings.TypeMappers.Add(new PrimitiveTypeMapper(
    typeof(Money),
    schema =>
    {
        schema.Type = JsonObjectType.String;
        schema.Format = "money";
        schema.Pattern = @"^\d+\.\d{2}$";
    }));
```

**Виды mapper'ов**:
- `PrimitiveTypeMapper` — маппит на примитивный тип (string, number, boolean, integer)
- `ObjectTypeMapper` — маппит на object-схему (с properties)
- Кастомный `ITypeMapper` — своя логика

**Когда полезно**:
- Value objects (`Money`, `EmailAddress`, `Guid` c кастомным форматом)
- Типы из внешних библиотек, которые NJsonSchema не должен пытаться разбирать
- `NodaTime`, `NetTopologySuite` типы

## Schema Processors

Пост-обработка. Вызывается после того, как схема сгенерирована из типа:

```csharp
public class MyProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        // context.Type — исходный .NET-тип
        // context.Schema — сгенерированная схема
        context.Schema.Description = "Modified by MyProcessor";
    }
}

settings.SchemaProcessors.Add(new MyProcessor());
```

## Атрибуты (аннотации)

Проект `NJsonSchema.Annotations`:

| Атрибут | Что делает |
|---|---|
| `[JsonSchemaIgnore]` | Не включать свойство/класс в схему |
| `[JsonSchemaType(typeof(string))]` | Явно указать тип, отличный от declared |
| `[CanBeNull]` / `[NotNull]` | Задать nullability явно |
| `[JsonSchemaExtensionData]` | Добавить `x-` расширения |

Также NJsonSchema читает стандартные .NET-атрибуты:

- `[Required]`, `[MinLength]`, `[MaxLength]`, `[Range]` — из `System.ComponentModel.DataAnnotations`
- `[Description]` — description в схеме
- XML-документация (`<summary>`) — если включена

## Наследование и композиция

### Наследование → `allOf`

```csharp
public class Animal { public string Name { get; set; } }
public class Dog : Animal { public string Breed { get; set; } }

var schema = JsonSchema.FromType<Dog>();
// Dog в схеме будет "allOf": [ {"$ref": "Animal"}, { "properties": { "Breed": ... } } ]
```

Управляется через `FlattenInheritanceHierarchy` — если `true`, схема Dog просто включает все свойства Animal напрямую.

### Интерфейсы / абстрактные классы → discriminator

Если пометить базовый класс атрибутом `[JsonInheritanceAttribute]`, NJsonSchema сгенерирует OpenAPI `discriminator`:

```csharp
[JsonInheritance("dog", typeof(Dog))]
[JsonInheritance("cat", typeof(Cat))]
public abstract class Animal { public string Name { get; set; } }
```

## Enum'ы

Enum'ы генерируются как JSON Schema с `enum: [...]`.

```csharp
public enum Status { Active, Inactive, Pending }
```

По умолчанию — как integer. Через `[JsonConverter(typeof(StringEnumConverter))]` (Newtonsoft) или `JsonStringEnumConverter` (System.Text.Json) — как string enum.

Атрибут `[EnumMember]` позволяет задать конкретные строковые значения:

```csharp
public enum Status
{
    [EnumMember(Value = "active")] Active,
    [EnumMember(Value = "inactive")] Inactive
}
```

## Известные проблемы / ограничения

- **Циклические ссылки** — обрабатываются через `$ref` (тип регистрируется однажды).
- **Открытые generic'и** — не поддерживаются (`List<>` без параметра).
- **Union types** (C# 12) — пока нет встроенной поддержки.

## Использование в NSwag

NSwag'овские `AspNetCoreOpenApiDocumentGeneratorSettings.SchemaSettings` — это ровно `JsonSchemaGeneratorSettings`. Все настройки, описанные выше, применяются и в NSwag:

```csharp
services.AddOpenApiDocument(settings =>
{
    settings.SchemaSettings.SchemaType = SchemaType.OpenApi3;
    settings.SchemaSettings.TypeMappers.Add(new PrimitiveTypeMapper(...));
    settings.SchemaSettings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
});
```
