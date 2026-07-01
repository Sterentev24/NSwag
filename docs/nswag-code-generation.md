# NSwag — генерация клиентов и контроллеров

## Общая схема

```
[OpenAPI document]           [Settings]
       │                        │
       └────────┬───────────────┘
                ▼
    ┌──────────────────────┐
    │  ClientGeneratorBase │  (общий пайплайн)
    └──────────┬───────────┘
               │
      ┌────────┴────────┐
      ▼                 ▼
  CSharpClient      TypeScriptClient
  Generator         Generator
      │                 │
      ▼                 ▼
[TypeResolver]     [TypeResolver]
      │                 │
      ▼                 ▼
[Liquid template]  [Liquid template]
      │                 │
      ▼                 ▼
   [C# code]        [TS code]
```

## Три вида генераторов

NSwag генерирует три разных артефакта:

| Что генерируется | Класс | Проект |
|---|---|---|
| **C# HTTP-клиент** (потребление API) | `CSharpClientGenerator` | `NSwag.CodeGeneration.CSharp` |
| **C# ASP.NET контроллеры** (contract-first) | `CSharpControllerGenerator` | `NSwag.CodeGeneration.CSharp` |
| **TypeScript HTTP-клиент** | `TypeScriptClientGenerator` | `NSwag.CodeGeneration.TypeScript` |

## Пайплайн генерации (C# client, пример)

```
1. OpenApiDocument загружен (из файла/URL/строки)
2. new CSharpClientGenerator(document, settings)
3. generator.GenerateFile() вызывается
4. Внутри:
   a. Обход document.Paths → для каждой операции создаётся модель метода
   b. Через CSharpTypeResolver разрешаются типы параметров и возвращаемых значений
      (тут используется NJsonSchema.CodeGeneration.CSharp для DTO-классов)
   c. Собираются модели: ClientTemplateModel, MethodTemplateModel, ParameterTemplateModel
   d. Liquid-шаблоны из Templates/ рендерят модели в строки кода
   e. Собирается итоговый .cs-файл (клиент + DTO-классы)
```

## Настройки (Settings)

### `CSharpClientGeneratorSettings`

Основные поля:

| Поле | Что делает |
|---|---|
| `ClassName` | Имя генерируемого класса клиента (обычно с `{controller}` placeholder'ом) |
| `CSharpGeneratorSettings.Namespace` | Namespace для клиента |
| `ExceptionClass` | Имя класса исключения (по умолчанию `ApiException`) |
| `UseBaseUrl` | Использовать ли поле `BaseUrl` в клиенте |
| `GenerateSyncMethods` | Генерировать ли синхронные обёртки над async |
| `GenerateClientClasses` | Генерировать класс или только интерфейс |
| `GenerateClientInterfaces` | Дополнительно генерировать интерфейс |
| `GenerateDtoTypes` | Включать ли DTO в тот же файл |
| `InjectHttpClient` | Инжектить `HttpClient` через конструктор (для DI) |
| `UseHttpClientCreationMethod` | Использовать виртуальный метод для создания `HttpClient` |
| `OperationGenerationMode` | Как группировать методы (`SingleClientFromOperationId`, `MultipleClientsFromOperationId`, и т.д.) |
| `ResponseClass` | Имя класса response wrapper'а |
| `WrapResponses` | Оборачивать ли responses в wrapper |

Наследуется от `CSharpGeneratorBaseSettings`, которая наследуется от общей `ClientGeneratorBaseSettings` (в `NSwag.CodeGeneration`).

### `TypeScriptClientGeneratorSettings`

Похожая структура, но с TS-специфичным:

- `Template` — какая либа: `Angular`, `Fetch`, `Axios`, `JQueryPromises`, и т.д.
- `PromiseType` — `Promise` или `Q`
- `HttpClass` — `HttpClient` (Angular) или другой
- `InjectionTokenType` — `InjectionToken` (Angular) или `OpaqueToken` (старый)
- `TypeScriptGeneratorSettings.ImportRequiredTypes`, `TypeStyle` (Class / Interface / KnockoutClass)

## TypeResolver

**Проблема**: OpenAPI `schema` может быть либо inline, либо `$ref` на другой schema, либо композицией (`allOf`, `oneOf`, `anyOf`). Задача TypeResolver'а — превратить это в валидный тип целевого языка.

**Классы**:

- `CSharpTypeResolver` (из NJsonSchema.CodeGeneration.CSharp) — маппит JSON Schema типы в C# (`string` → `string`, `array` → `List<T>`, `object` с $ref → generated class, enum → generated enum)
- `TypeScriptTypeResolver` (из NJsonSchema.CodeGeneration.TypeScript) — маппит в TS

TypeResolver также следит за уникальностью имён (`resolve name conflicts`), хранит зарегистрированные типы и делегирует их генерацию базовому NJsonSchema-генератору.

## Liquid-шаблоны

Все шаблоны лежат в `Templates/` в соответствующих проектах:

**`NSwag.CodeGeneration.CSharp/Templates/`**:
- `Client.Class.liquid` — оболочка класса клиента
- `Client.Method.liquid` — один метод (async, конструирует HttpRequest, обрабатывает Response)
- `Client.Exception.liquid` — класс исключения
- `Client.Constructor.liquid`, `Client.PartialMethods.liquid`, и т.д.

**`NSwag.CodeGeneration.TypeScript/Templates/`**:
- `Client.Class.liquid`
- `Client.Angular.liquid`, `Client.Fetch.liquid`, `Client.Axios.liquid` — специфика per-template
- `Client.RxJs.Observable.liquid` — обёртка Observable для Angular

Шаблоны компилируются в бинарь как embedded resources (см. `.csproj` — `<EmbeddedResource Include="Templates/**/*.liquid" />`).

## Программный пример

```csharp
// Читаем спеку
var document = await OpenApiDocument.FromFileAsync("api.json");

// Настройки
var settings = new CSharpClientGeneratorSettings
{
    ClassName = "MyApiClient",
    CSharpGeneratorSettings =
    {
        Namespace = "MyCompany.Api.Client",
        DateTimeType = "System.DateTimeOffset",
        ArrayType = "System.Collections.Generic.List"
    },
    GenerateClientInterfaces = true,
    InjectHttpClient = true
};

// Генерация
var generator = new CSharpClientGenerator(document, settings);
var code = generator.GenerateFile();

// code — готовый .cs-файл (строка), пишем в файловую систему
File.WriteAllText("MyApiClient.cs", code);
```

## Что генерируется на выходе (C# client, упрощённо)

```csharp
namespace MyCompany.Api.Client
{
    // Интерфейс (если GenerateClientInterfaces = true)
    public interface IMyApiClient
    {
        Task<UserDto> GetUserAsync(int id, CancellationToken ct = default);
        // ... остальные методы
    }

    // Реализация
    public partial class MyApiClient : IMyApiClient
    {
        private HttpClient _httpClient;
        private string _baseUrl = "";

        public MyApiClient(HttpClient httpClient) { _httpClient = httpClient; }

        public async Task<UserDto> GetUserAsync(int id, CancellationToken ct = default)
        {
            var url = _baseUrl + "/users/" + id;
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, ct);
            // ... десериализация, обработка status codes, throw ApiException
        }
    }

    // DTO
    public partial class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // Exception
    public partial class ApiException : Exception { /* ... */ }
}
```

## Что генерируется на выходе (TypeScript, Angular)

```typescript
@Injectable()
export class MyApiClient {
    private http: HttpClient;
    private baseUrl: string;

    constructor(@Inject(HttpClient) http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.http = http;
        this.baseUrl = baseUrl ?? "";
    }

    getUser(id: number): Observable<UserDto> {
        let url = this.baseUrl + "/users/" + id;
        return this.http.get<UserDto>(url).pipe(/* ... */);
    }
}

export interface UserDto {
    id: number;
    name: string;
}
```

## Ключевые файлы

| Файл | Что там |
|---|---|
| `src/NSwag.CodeGeneration/ClientGeneratorBase.cs` | Общий пайплайн, обход document.Paths |
| `src/NSwag.CodeGeneration.CSharp/CSharpClientGenerator.cs` | C# специфика |
| `src/NSwag.CodeGeneration.CSharp/Templates/Client.Class.liquid` | Шаблон C#-клиента |
| `src/NSwag.CodeGeneration.TypeScript/TypeScriptClientGenerator.cs` | TS специфика |
| `src/NSwag.CodeGeneration.TypeScript/Templates/Client.Class.liquid` | Шаблон TS-клиента |

## Тесты

Тесты сравнивают ожидаемый вывод с фактическим:

- `NSwag.CodeGeneration.CSharp.Tests` — набор `*.approved.cs` snapshot'ов с ожидаемым C#-кодом
- `NSwag.CodeGeneration.TypeScript.Tests` — аналогично для TS

Меняешь генерацию → snapshot ломается → сравниваешь → обновляешь approved-файл.
