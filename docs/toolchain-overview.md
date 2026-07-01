# NSwag + NJsonSchema — единый toolchain

## Что решают эти библиотеки вместе

Пара библиотек покрывает полный жизненный цикл OpenAPI/Swagger в экосистеме .NET:

1. **Backend разработчик** пишет ASP.NET Core контроллеры
2. **NSwag** через рефлексию читает контроллеры и через `ApiExplorer` собирает описание endpoint'ов
3. **NSwag + NJsonSchema** генерируют OpenAPI 3.0 (или Swagger 2.0) JSON/YAML документ
4. Тот же документ **NSwag** сериализует и раздаёт через ASP.NET Core middleware (`UseOpenApi`, `UseSwaggerUi`)
5. Frontend разработчик (или backend в другом сервисе) через **NSwag CLI / MSBuild / Studio** генерирует типизированный клиент (C# или TypeScript) из этого OpenAPI-документа
6. **NJsonSchema** внутри отвечает за преобразование JSON Schema (которая описывает DTO'шки в OpenAPI) в C#/TypeScript-классы

Итого: один документ (OpenAPI) — единая правда контракта, два конца автогенерации (спека из кода + код из спеки).

## Разделение ответственности

```
┌─────────────────────────────────────────────────────────────┐
│                        NSwag                                │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ OpenAPI-специфика:                                    │  │
│  │  - модель OpenApiDocument (paths, operations, params) │  │
│  │  - генерация спеки из ASP.NET/WebApi контроллеров     │  │
│  │  - middleware для раздачи спеки и Swagger UI          │  │
│  │  - генерация HTTP-клиентов (C# / TypeScript)          │  │
│  │  - CLI, MSBuild targets, NSwagStudio (WPF)            │  │
│  └───────────────────────────────────────────────────────┘  │
└──────────────────────────────┬──────────────────────────────┘
                               │ depends on
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                     NJsonSchema                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ JSON Schema:                                          │  │
│  │  - модель JsonSchema (Draft 4/6/7/2019-09/2020-12)    │  │
│  │  - валидация JSON против схемы                        │  │
│  │  - генерация JsonSchema из .NET-типа (рефлексия)      │  │
│  │  - генерация C#/TypeScript классов из JsonSchema      │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

**Почему разделены:** JSON Schema — самостоятельный стандарт со своей моделью (типы, `$ref`, композиция, валидация). OpenAPI использует JSON Schema для описания schemas, но добавляет свои сущности (paths, operations, parameters, responses, security schemes). Разделение позволяет использовать NJsonSchema отдельно от OpenAPI (например, только для валидации JSON).

## Пример конвейера в реальной команде

**Сценарий 1: backend публикует спеку, фронт генерирует TypeScript-клиент**

```
[C# ASP.NET Core проект]
   │
   │ services.AddOpenApiDocument()   ─── NSwag.AspNetCore
   │ app.UseOpenApi()                    NSwag.Generation.AspNetCore
   │
   ▼
[HTTP: GET /swagger/v1/swagger.json]     NSwag.Core (сериализация)
   │
   │ nswag openapi2tsclient
   │       /input:https://api/swagger/v1/swagger.json
   │       /output:api-client.ts
   │
   ▼
[TypeScript-клиент]                      NSwag.CodeGeneration.TypeScript
                                         NJsonSchema.CodeGeneration.TypeScript
```

**Сценарий 2: contract-first — OpenAPI-файл в git, оба конца генерируются из него**

```
[api.yaml в репозитории]
   │
   ├─ nswag openapi2cscontroller ──→ [ASP.NET Core Controllers.cs]
   │                                  (NSwag.CodeGeneration.CSharp)
   │
   └─ nswag openapi2tsclient    ──→ [Angular / Fetch клиент.ts]
                                     (NSwag.CodeGeneration.TypeScript)
```

## Модель зависимостей

- NSwag ссылается на **NJsonSchema как NuGet-пакет** (11.0.x на момент изучения), не как на project reference.
- Это значит: NSwag и NJsonSchema релизятся независимо, но NSwag должен поддерживать конкретную мажорную версию NJsonSchema.
- Обе библиотеки multi-target: `netstandard2.0`, `net462`, `net6.0`, `net8.0` — покрывают весь актуальный .NET-контур.

## Где точно **не пересекаются**

- **ASP.NET интеграция** — только в NSwag (`NSwag.AspNetCore`, `NSwag.AspNet.Owin`, `NSwag.AspNet.WebApi`). NJsonSchema не знает про HTTP/контроллеры.
- **CLI и Studio** — только в NSwag. NJsonSchema — библиотека без собственных исполняемых surface'ов.
- **YAML** — есть в обеих (`NSwag.Core.Yaml`, `NJsonSchema.Yaml`), но независимо, каждая для своей модели.

## Дальше

- Детали внутреннего устройства NSwag: [nswag-architecture.md](nswag-architecture.md)
- Детали внутреннего устройства NJsonSchema: [njsonschema-architecture.md](njsonschema-architecture.md)
