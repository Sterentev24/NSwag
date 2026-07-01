# NSwag — архитектура

## Назначение

NSwag — интегрированный toolchain для OpenAPI 2.0/3.0 в экосистеме .NET. Заменяет и объединяет функциональность двух проектов:

- **Swashbuckle** — генерация спецификации из ASP.NET контроллеров
- **AutoRest** — генерация типизированных клиентов из OpenAPI

Ключевое отличие: NSwag использует **свою модель JSON Schema** (`NJsonSchema`) для более корректной поддержки наследования, enum'ов, обработки `$ref`, композиции — местам, где OpenAPI-спецификация допускает неоднозначности и разные вендоры генерируют разный код.

## Слои (высокоуровнево)

```
┌──────────────────────────────────────────────────────────┐
│  Distribution surface                                    │
│  NSwagStudio (WPF)  │  NSwag.Console  │  NSwag.MSBuild   │
│  NSwag.ConsoleCore  │  NSwag.Npm      │                  │
└─────────────────────┬────────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────────┐
│  Command layer                                           │
│  NSwag.Commands — определения команд CLI/Studio          │
│  (openapi2csclient, openapi2tsclient, aspnetcore2openapi)│
└─────────────────────┬────────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────────┐
│  Code generation layer                                   │
│  NSwag.CodeGeneration              — базовые классы      │
│  NSwag.CodeGeneration.CSharp       — C# client/controller│
│  NSwag.CodeGeneration.TypeScript   — TypeScript client   │
└─────────────────────┬────────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────────┐
│  Spec generation layer                                   │
│  NSwag.Generation                  — базовый генератор   │
│  NSwag.Generation.AspNetCore       — ASP.NET Core / ApiExplorer │
│  NSwag.Generation.WebApi           — Web API 2.x         │
└─────────────────────┬────────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────────┐
│  ASP.NET integration                                     │
│  NSwag.AspNetCore   — middleware (UseOpenApi, UseSwaggerUi)│
│  NSwag.AspNet.Owin  — OWIN middleware                    │
│  NSwag.AspNet.WebApi — JsonExceptionFilterAttribute      │
└─────────────────────┬────────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────────┐
│  Core spec model                                         │
│  NSwag.Core       — OpenApiDocument, чтение/запись JSON  │
│  NSwag.Core.Yaml  — YAML-адаптер                         │
│  NSwag.Annotations — атрибуты для контроллеров           │
└─────────────────────┬────────────────────────────────────┘
                      │ depends on
                      ▼
                 NJsonSchema (NuGet)
```

## Ключевые точки входа

| Функция | Класс / метод | Файл |
|---|---|---|
| Чтение OpenAPI | `OpenApiDocument.FromFileAsync(path)` / `FromJsonAsync(json)` | `src/NSwag.Core/OpenApiDocument.cs` |
| Запись OpenAPI | `document.ToJson()` / `document.ToYaml()` | там же |
| Генерация спеки из ASP.NET Core | `AspNetCoreOpenApiDocumentGenerator` | `src/NSwag.Generation.AspNetCore/AspNetCoreOpenApiDocumentGenerator.cs` |
| Регистрация middleware (services) | `services.AddOpenApiDocument()` | `src/NSwag.AspNetCore/Extensions/NSwagServiceCollectionExtensions.cs` |
| Регистрация middleware (app) | `app.UseOpenApi()`, `app.UseSwaggerUi()` | `src/NSwag.AspNetCore/Extensions/NSwagApplicationBuilderExtensions.cs` |
| C# client generator | `CSharpClientGenerator` | `src/NSwag.CodeGeneration.CSharp/CSharpClientGenerator.cs` |
| C# controller generator | `CSharpControllerGenerator` | `src/NSwag.CodeGeneration.CSharp/CSharpControllerGenerator.cs` |
| TypeScript client generator | `TypeScriptClientGenerator` | `src/NSwag.CodeGeneration.TypeScript/TypeScriptClientGenerator.cs` |

## Способы использования

1. **ASP.NET Core middleware** — рекомендованный. Один `AddOpenApiDocument()` + `UseOpenApi()` — и спека доступна по `/swagger/v1/swagger.json`.
2. **CLI (`nswag`)** — для CI/CD и для генерации клиентов из уже существующей спеки.
3. **MSBuild** — `<PackageReference Include="NSwag.MSBuild">` + `.targets` — генерация происходит при билде проекта.
4. **NSwagStudio** — WPF-приложение для быстрого прототипирования конфигураций генерации.
5. **NPM** — та же CLI, распространяется через npm для JS/TS команд, которые не хотят ставить .NET.
6. **Programmatic C# API** — `new CSharpClientGenerator(document, settings).GenerateFile()`.

## Зависимости

| Пакет | Роль |
|---|---|
| **NJsonSchema** (11.x) | JSON Schema model, code generation base |
| **Newtonsoft.Json** (13.x) | Основная сериализация в NSwag.Core |
| **System.Text.Json** | В некоторых новых модулях (условно по TFM) |
| **Namotion.Reflection** (3.x) | Расширенная рефлексия (XML docs, nullable annotations) |
| **NConsole** | Парсинг CLI-команд |
| **Microsoft.AspNetCore.Mvc.ApiExplorer** | Для генерации из ASP.NET Core |

## Сборка

- **Скрипты**: `build.ps1`, `build.cmd`, `build.sh` — кроссплатформенные обёртки
- **CI**: `azure-pipelines.yml` (Azure DevOps)
- **Мультитаргетинг**:
  - `netstandard2.0` — ядро (`NSwag.Core`)
  - `net462` — WPF Studio, Windows Console
  - `net6.0`, `net8.0` — ConsoleCore, Generation.AspNetCore
- **Требования SDK**: .NET 6.0, 7.0.x, 8.0.100

## Тестирование

9 тестовых проектов, организованных зеркально основным:

- **Модульные**: `NSwag.Core.Tests`, `NSwag.Core.Yaml.Tests`, `NSwag.CodeGeneration.Tests`
- **Интеграционные**: `NSwag.Generation.AspNetCore.Tests` (+ `Tests.Web` — тестовое web-приложение)
- **Кодогенерация**: `NSwag.CodeGeneration.CSharp.Tests`, `NSwag.CodeGeneration.TypeScript.Tests`
- **CLI**: `NSwag.ConsoleCore.Tests`

## Дальше

- Детальная разбивка проектов: [nswag-layers.md](nswag-layers.md)
- Как работает генерация клиентов: [nswag-code-generation.md](nswag-code-generation.md)
- Точки расширения (процессоры, шаблоны): [nswag-extension-points.md](nswag-extension-points.md)
