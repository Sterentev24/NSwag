# NSwag — детальная разбивка проектов

Все проекты лежат в `src/`. Ниже — по логическим слоям, снизу вверх.

## 1. Ядро спецификаций (Core)

### `NSwag.Core`

- **Роль**: модель OpenAPI 2.0/3.0. Классы `OpenApiDocument`, `OpenApiPathItem`, `OpenApiOperation`, `OpenApiParameter`, `OpenApiResponse`, `OpenApiSecurityScheme`, и т.д.
- **Ключевой класс**: `OpenApiDocument` — корень модели. Поддерживает чтение из строки/файла/URL, запись в JSON.
- **Основная сериализация**: `Newtonsoft.Json`
- **TFM**: `netstandard2.0`, `net462`

### `NSwag.Core.Yaml`

- **Роль**: YAML-адаптер над `NSwag.Core`. Расширяет `OpenApiDocument` методами `FromYamlAsync` / `ToYaml`.
- **Зависимость**: `YamlDotNet`

### `NSwag.Annotations`

- **Роль**: атрибуты, которыми разработчик декорирует ASP.NET-контроллеры для управления генерацией.
- **Ключевые атрибуты**: `OpenApiTagAttribute`, `OpenApiOperationAttribute`, `OpenApiIgnoreAttribute`, `OpenApiResponseAttribute`.
- Референсится в проекте контроллеров, не тянет тяжёлых зависимостей.

## 2. Генерация OpenAPI из контроллеров (Generation)

### `NSwag.Generation`

- **Роль**: базовый пайплайн генерации. Абстрактный `OpenApiDocumentGenerator`, `OpenApiDocumentGeneratorSettings`.
- **Ключевые интерфейсы для расширения**:
  - `IDocumentProcessor` — модифицирует документ как целое
  - `IOperationProcessor` — модифицирует конкретную операцию
- **Не зависит от ASP.NET** — только модель + процессоры.

### `NSwag.Generation.AspNetCore`

- **Роль**: генерация OpenAPI из ASP.NET Core через `ApiExplorer`.
- **Ключевой класс**: `AspNetCoreOpenApiDocumentGenerator` — использует `IApiDescriptionGroupCollectionProvider` для получения списка endpoint'ов.
- **Файл**: `src/NSwag.Generation.AspNetCore/AspNetCoreOpenApiDocumentGenerator.cs`
- **Настройки**: `AspNetCoreOpenApiDocumentGeneratorSettings` — SchemaSettings, DefaultResponseReferenceTypeNullHandling, ApiGroupNames и т.д.
- Собственные процессоры лежат в `Processors/`.

### `NSwag.Generation.WebApi`

- **Роль**: то же самое, но для классического ASP.NET Web API 2.x (не Core).
- **Ключевой класс**: `WebApiOpenApiDocumentGenerator` — анализирует контроллеры через собственную рефлексию (без ApiExplorer).

## 3. Генерация кода (CodeGeneration)

### `NSwag.CodeGeneration`

- **Роль**: базовые классы для генерации клиентов на любом языке. `ClientGeneratorBase`, `ClientGeneratorBaseSettings`.
- **Универсальные подсистемы**:
  - `OperationNameGenerator` — как называть методы клиента из operationId
  - `ParameterNameGenerator` — как называть параметры
  - `ExceptionType`, `ResponseType` — обобщённые модели

### `NSwag.CodeGeneration.CSharp`

- **Роль**: генерация C# клиентов и контроллеров.
- **Классы**:
  - `CSharpClientGenerator` — HTTP-клиент (использует `HttpClient`)
  - `CSharpControllerGenerator` — ASP.NET Web API/Core контроллеры (contract-first)
  - `CSharpGeneratorBase` — общая база
- **Настройки**: `CSharpClientGeneratorSettings` (класс, namespace, exception class, base URL policy)
- **Шаблоны**: `Templates/*.liquid` — Client.Class.liquid, Client.Method.liquid, Response.liquid, и т.д.

### `NSwag.CodeGeneration.TypeScript`

- **Роль**: генерация TypeScript клиентов.
- **Класс**: `TypeScriptClientGenerator`
- **Поддерживаемые шаблоны/либы**:
  - `JQueryCallbacks`, `JQueryPromises`
  - `AngularJS`
  - `Angular` (v2+, использует `HttpClient` из `@angular/common/http`)
  - `Fetch` (window.fetch + ES6 Promises)
  - `Aurelia` (на базе Fetch)
  - `Axios` (preview)
- **Шаблоны**: аналогичный Liquid-набор для TS.

## 4. ASP.NET интеграция

### `NSwag.AspNetCore`

- **Роль**: middleware для ASP.NET Core, раздача OpenAPI JSON + Swagger UI + ReDoc.
- **Основные extension'ы**:
  - `services.AddOpenApiDocument()` / `AddSwaggerDocument()` — регистрирует генератор
  - `app.UseOpenApi()` — раздаёт `/swagger/v1/swagger.json`
  - `app.UseSwaggerUi()` — раздаёт Swagger UI по `/swagger`
  - `app.UseReDoc()` — раздаёт ReDoc
- **Файлы**:
  - `src/NSwag.AspNetCore/Extensions/NSwagServiceCollectionExtensions.cs`
  - `src/NSwag.AspNetCore/Extensions/NSwagApplicationBuilderExtensions.cs`
- **TFM**: `net6.0`, `net8.0`

### `NSwag.AspNet.Owin`

- **Роль**: то же, но для OWIN-based приложений (классический .NET Framework).

### `NSwag.AspNet.WebApi`

- **Роль**: `JsonExceptionFilterAttribute` — фильтр Web API 2.x, сериализующий исключения в JSON.
- **Не** генерирует спеку — это просто удобный фильтр.

## 5. CLI и распространение

### `NSwag.Commands`

- **Роль**: определения команд CLI и Studio. Каждая команда — класс с атрибутами `[Command]`, `[Argument]`.
- **Примеры команд**: `OpenApiToCSharpClientCommand`, `OpenApiToTypeScriptClientCommand`, `AspNetCoreToOpenApiCommand`, `WebApiToOpenApiCommand`.
- Используется и CLI, и WPF-Studio — общая модель.

### `NSwag.ConsoleCore`

- **Роль**: `dotnet nswag` — .NET Core / .NET 6/8 CLI.
- **Дистрибуция**: `dotnet tool install --global NSwag.ConsoleCore` или `<DotNetCliToolReference>`.
- **TFM**: `net6.0`, `net8.0`.

### `NSwag.Console` / `NSwag.Console.x86`

- **Роль**: CLI для .NET Framework 4.6.2 (x64 и x86 варианты).

### `NSwag.Npm`

- **Роль**: обёртка над CLI для npm-экосистемы. `package.json` + `bin/nswag.js`.
- **Установка**: `npm install nswag`.

### `NSwag.MSBuild`

- **Роль**: NuGet-пакет с `.targets`-файлом. Позволяет запускать `nswag run` из MSBuild-таргета.
- **Файл**: `src/NSwag.MSBuild/NSwag.MSBuild.props` (пути к бинарям для разных TFM).

### `NSwag.ApiDescription.Client`

- **Роль**: интеграция с новой моделью ServiceProjectReference в .csproj — генерация клиента при билде без явных MSBuild-таргетов.

### `NSwagStudio` (WPF)

- **Роль**: desktop GUI для конфигурирования генерации.
- **TFM**: `net462` (WPF).
- Использует `NSwag.Commands` — визуальная обёртка над теми же командами, что CLI.

### `NSwagStudio.Installer` / `NSwagStudio.Chocolatey`

- **Роль**: WiX-инсталлятор MSI и Chocolatey-пакет для дистрибуции Studio.

## 6. AssemblyLoader (изоляция)

### `NSwag.AssemblyLoader`

- **Роль**: загрузка Web API assembly в изолированный AppDomain (или ALC на .NET Core) для генерации спеки без запуска приложения целиком.
- Используется CLI-командами `webapi2openapi` и `types2openapi`.

## 7. Тесты

Зеркалят основную структуру:

- `NSwag.Core.Tests`, `NSwag.Core.Yaml.Tests`
- `NSwag.CodeGeneration.Tests`, `NSwag.CodeGeneration.CSharp.Tests`, `NSwag.CodeGeneration.TypeScript.Tests`
- `NSwag.Generation.Tests`, `NSwag.Generation.AspNetCore.Tests`, `NSwag.Generation.WebApi.Tests`
- `NSwag.ConsoleCore.Tests`
- **Отдельный тестовый web-app**: `NSwag.Generation.AspNetCore.Tests.Web` — минимальный ASP.NET Core проект для интеграционных тестов ApiExplorer.

## 8. Samples

- `NSwag.Sample.NET60`, `NSwag.Sample.NET60Minimal`
- `NSwag.Sample.NET80`, `NSwag.Sample.NET80Minimal`
- `NSwag.Sample.Common` — общий код

Показывают минимальные конфигурации для соответствующих версий .NET.

## Иерархия зависимостей (упрощённо)

```
NSwag.Annotations ────────────────────────────────────┐
                                                       │
NSwag.Core ← NSwag.Core.Yaml                          │
    ▲                                                 │
    │                                                 │
NSwag.Generation ← NSwag.Generation.AspNetCore ←──────┤
              ↖ NSwag.Generation.WebApi ←─────────────┤
                                                       │
NSwag.CodeGeneration ← NSwag.CodeGeneration.CSharp    │
                    ↖ NSwag.CodeGeneration.TypeScript │
                                                       │
NSwag.AspNetCore ─────────────────────────────────────┤
NSwag.AspNet.Owin ────────────────────────────────────┤
                                                       │
NSwag.Commands ← NSwag.ConsoleCore, NSwag.Console ←───┤
              ← NSwagStudio                            │
                                                       │
                       все ссылаются на ───────────────┘
                                                       ▼
                                                   NJsonSchema
```
