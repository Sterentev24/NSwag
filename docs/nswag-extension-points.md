# NSwag — точки расширения

Пять способов повлиять на генерацию.

## 1. Атрибуты в контроллерах (declarative)

Проект `NSwag.Annotations` — атрибуты, которыми декорируешь ASP.NET-контроллеры.

| Атрибут | Что делает |
|---|---|
| `[OpenApiTag("Users")]` | Явно задаёт tag операции (иначе — из имени контроллера) |
| `[OpenApiOperation("GetUser", "Get user by id")]` | Задаёт operationId и описание |
| `[OpenApiIgnore]` | Исключает контроллер/action из спеки |
| `[OpenApiResponse(200, typeof(UserDto))]` | Явно описывает response (кроме `ProducesResponseType`) |
| `[OpenApiExtensionData("x-vendor-extension", "value")]` | Добавляет `x-` расширение |
| `[OpenApiParameter]`, `[OpenApiBodyParameter]` | Управление параметрами |
| `[OpenApiFile]` | Помечает параметр как file upload |

Плюс NSwag читает стандартные ASP.NET-атрибуты: `[ProducesResponseType]`, `[HttpGet]`, `[Route]`, `[FromBody]`, `[FromQuery]`, XML doc-комменты (`<summary>`, `<param>`, `<returns>`).

## 2. Document Processors и Operation Processors (imperative)

Основная точка расширения — процессоры. Регистрируются в `AddOpenApiDocument`:

```csharp
services.AddOpenApiDocument(settings =>
{
    settings.DocumentProcessors.Add(new MyDocumentProcessor());
    settings.OperationProcessors.Add(new MyOperationProcessor());
});
```

### `IDocumentProcessor`

Вызывается один раз после того, как весь документ собран. Может модифицировать всё:

```csharp
public interface IDocumentProcessor
{
    void Process(DocumentProcessorContext context);
}
```

**Типовые применения**:
- Добавить security scheme в весь документ
- Добавить свои теги
- Модифицировать `info` (title, version, description)
- Добавить `x-` расширения к document root

### `IOperationProcessor`

Вызывается для каждой операции. Может вернуть `false` — операция будет исключена.

```csharp
public interface IOperationProcessor
{
    bool Process(OperationProcessorContext context);
}
```

**Типовые применения**:
- Добавить общие headers во все операции
- Отфильтровать операции по кастомному критерию
- Модифицировать response type в зависимости от контекста
- Извлечь метаданные из custom-атрибутов

### Встроенные процессоры

- `OperationParameterProcessor` — парсит параметры
- `OperationResponseProcessor` — обрабатывает responses
- `OperationSecurityScopeProcessor` — security
- `ApiVersionProcessor` — интеграция с ASP.NET Core API Versioning

Все встроенные лежат в `src/NSwag.Generation.AspNetCore/Processors/` и `src/NSwag.Generation/Processors/`.

## 3. Settings (declarative, но программно)

Все аспекты генерации управляются через настройки. Основные:

### Для генерации спеки

- `AspNetCoreOpenApiDocumentGeneratorSettings.SchemaSettings` (это `JsonSchemaGeneratorSettings` из NJsonSchema)
  - `SchemaType` — Swagger2 или OpenApi3
  - `DefaultReferenceTypeNullHandling` — Null / NotNull
  - `SerializerSettings` — Newtonsoft.Json/System.Text.Json options
- `ApiGroupNames` — какие группы включать
- `DocumentName` — имя документа (для `/swagger/{name}/swagger.json`)
- `PostProcess = document => { ... }` — колбэк для финальной модификации

### Для генерации клиентов

Специфические поля описаны в [nswag-code-generation.md](nswag-code-generation.md). Основные принципы:

- Общие настройки лежат в `ClientGeneratorBaseSettings`
- Language-специфика — в `CSharpGeneratorSettings` / `TypeScriptGeneratorSettings` (это уже из NJsonSchema)
- Composed через свойство: `settings.CSharpGeneratorSettings.Namespace = ...`

## 4. Type Mappers (через NJsonSchema)

При генерации спеки из .NET-типа NSwag делегирует NJsonSchema, который поддерживает custom type mappers:

```csharp
services.AddOpenApiDocument(settings =>
{
    settings.SchemaSettings.TypeMappers.Add(new PrimitiveTypeMapper(
        typeof(MyMoney),
        schema => { schema.Type = JsonObjectType.String; schema.Format = "money"; }));
});
```

Позволяет:
- Отобразить кастомный value type в стандартный JSON тип
- Использовать одну и ту же схему для нескольких .NET-типов
- Обойти стандартную рефлексию для специфических типов (например `Newtonsoft.Json.Linq.JObject`)

## 5. Liquid-шаблоны (наиболее радикальное)

Все `.liquid`-шаблоны в `Templates/` можно **полностью заменить**, передав кастомную `ITemplateFactory`:

```csharp
public class MyTemplateFactory : DefaultTemplateFactory
{
    protected override string GetEmbeddedLiquidTemplate(string language, string template)
    {
        // Загрузить из своего source: файл, embedded resource, database
        return File.ReadAllText($"MyTemplates/{template}.liquid");
    }
}

var settings = new CSharpClientGeneratorSettings
{
    TemplateFactory = new MyTemplateFactory()
};
```

**Когда нужно**:
- Кастомный стиль генерируемого кода (например, для внутренней библиотеки-обёртки)
- Дополнительные using'и/imports во все файлы
- Свой формат exception-класса или base class

**Когда НЕ нужно**:
- Мелкие правки — часто решаются через `Settings.AdditionalContractNamespaceUsages`, `Settings.AdditionalNamespaceUsages`, partial classes.

## 6. Partial classes (C#) / declaration merging (TS)

Все генерируемые классы — `partial`. Значит, можно дописать свою логику рядом без правки автогенерированного файла:

```csharp
// Автогенерировано NSwag'ом:
public partial class MyApiClient { /* ... */ }

// Твой код (в отдельном файле):
public partial class MyApiClient
{
    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        request.Headers.Add("X-Custom-Header", "value");
    }
}
```

NSwag генерирует `partial` методы (`PrepareRequest`, `ProcessResponse`) специально для расширения без модификации.

## Выбор точки расширения (правило)

```
Задача                                    → Что использовать
────────────────────────────────────────────────────────────
Добавить headers во все запросы           → partial method PrepareRequest
Изменить формат operation ID              → OperationNameGenerator (в settings)
Скрыть некоторые endpoints                → [OpenApiIgnore] или IOperationProcessor
Кастомный тип в схеме                     → TypeMapper
Кастомный namespace / usings              → CSharpGeneratorSettings.AdditionalNamespaceUsages
Полная замена шаблона клиента             → ITemplateFactory
Модифицировать response-класс             → Property Settings.ResponseClass или partial class
Добавить security scheme                  → IDocumentProcessor
Извлечь метаданные из custom-атрибутов    → IOperationProcessor
```

Правило пирамиды: сначала атрибуты, потом settings, потом процессоры, потом partial-classes, потом шаблоны. Каждый следующий уровень — сложнее и требует больше поддержки.
