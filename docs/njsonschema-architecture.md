# NJsonSchema — архитектура

## Назначение

NJsonSchema — .NET-библиотека для работы с JSON Schema. Четыре независимые функции:

1. **Читать и писать JSON Schema** — Draft 4, 6, 7, 2019-09, 2020-12
2. **Валидировать JSON против схемы** — с подробными ошибками
3. **Генерировать JSON Schema из .NET-типа** — через рефлексию (`JsonSchema.FromType<MyDto>()`)
4. **Генерировать C#/TypeScript код из JSON Schema** — для DTO'шек

Функции независимы: можно использовать только валидацию, только code generation, только чтение — без загрузки всей библиотеки.

Библиотека является **базой NSwag** и других проектов, работающих со схемами.

## Слои

```
┌───────────────────────────────────────────────────────┐
│  Code Generation (C# / TypeScript)                    │
│  NJsonSchema.CodeGeneration           — база          │
│  NJsonSchema.CodeGeneration.CSharp    — C# генератор  │
│  NJsonSchema.CodeGeneration.TypeScript — TS генератор │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  Schema Generation (from .NET types)                  │
│  Reflection → JsonSchemaGenerator → JsonSchema        │
│  (JsonSchemaGenerator внутри NJsonSchema.csproj)      │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  Validation                                           │
│  JsonSchemaValidator → ValidationError[]              │
│  (внутри NJsonSchema.csproj)                          │
└──────────────────────┬────────────────────────────────┘
                       │
┌──────────────────────▼────────────────────────────────┐
│  Core (schema model + I/O)                            │
│  NJsonSchema         — JsonSchema, JsonSchemaProperty │
│  NJsonSchema.Yaml    — YAML-адаптер                   │
│  NJsonSchema.Annotations — атрибуты                   │
│  NJsonSchema.NewtonsoftJson — Newtonsoft-мост         │
└───────────────────────────────────────────────────────┘
```

## Проекты (`src/*`)

### Ядро

| Проект | Роль |
|---|---|
| **NJsonSchema** | Модель `JsonSchema`, чтение/запись JSON, валидация, генерация из типа |
| **NJsonSchema.Annotations** | Атрибуты: `JsonSchemaIgnoreAttribute`, `JsonSchemaTypeAttribute`, `CanBeNullAttribute` |
| **NJsonSchema.Yaml** | Расширения для YAML (зависит от `YamlDotNet`) |
| **NJsonSchema.NewtonsoftJson** | Отдельный мост к `Newtonsoft.Json` — `NewtonsoftJsonReflectionService`, `NewtonsoftJsonSchemaGenerator` |

### Code Generation

| Проект | Роль |
|---|---|
| **NJsonSchema.CodeGeneration** | Базовые классы: `GeneratorBase`, `TypeResolverBase`, `CodeGeneratorSettingsBase`, модели артефактов |
| **NJsonSchema.CodeGeneration.CSharp** | `CSharpGenerator`, `CSharpTypeResolver`, `CSharpGeneratorSettings`, Liquid-шаблоны |
| **NJsonSchema.CodeGeneration.TypeScript** | `TypeScriptGenerator`, `TypeScriptTypeResolver`, аналогичные шаблоны |

### Тесты и вспомогательное

| Проект | Роль |
|---|---|
| **NJsonSchema.Tests** | Модульные тесты по функциям (Schema, Generation, Validation, Serialization) |
| **NJsonSchema.NewtonsoftJson.Tests** | Тесты Newtonsoft-моста |
| **NJsonSchema.CodeGeneration.Tests** | Общие тесты кодогенерации |
| **NJsonSchema.CodeGeneration.CSharp.Tests** | C# генератор — approval-тесты |
| **NJsonSchema.CodeGeneration.TypeScript.Tests** | TS генератор — approval-тесты |
| **NJsonSchema.Yaml.Tests** | YAML |
| **NJsonSchema.Demo** | Демо-приложение |
| **NJsonSchema.Benchmark** | BenchmarkDotNet, для замеров производительности |

## Ключевые точки входа

| Что делаем | Класс / метод | Файл |
|---|---|---|
| Прочитать схему из JSON-строки | `JsonSchema.FromJsonAsync(json)` | `src/NJsonSchema/JsonSchema.cs` |
| Прочитать схему из файла | `JsonSchema.FromFileAsync(path)` | там же |
| Сгенерировать схему из .NET-типа | `JsonSchema.FromType<MyDto>()` | там же |
| Валидировать JSON | `schema.Validate(jsonToken)` → `ICollection<ValidationError>` | `src/NJsonSchema/Validation/JsonSchemaValidator.cs` |
| Генерация схем более гибко | `new JsonSchemaGenerator(settings).Generate(type)` | `src/NJsonSchema/Generation/JsonSchemaGenerator.cs` |
| C# генерация | `new CSharpGenerator(schema, settings).GenerateFile()` | `src/NJsonSchema.CodeGeneration.CSharp/CSharpGenerator.cs` |
| TypeScript генерация | `new TypeScriptGenerator(schema, settings).GenerateFile()` | `src/NJsonSchema.CodeGeneration.TypeScript/TypeScriptGenerator.cs` |

## Newtonsoft.Json vs System.Text.Json

Библиотека поддерживает **обе** JSON-библиотеки, но с оговоркой:

- **Модель JsonSchema** сериализуется в JSON через **Newtonsoft.Json** — это исторически (проект возник до `System.Text.Json`), плюс `Newtonsoft` даёт больше контроля через converters.
- **Рефлексия для генерации схемы из .NET-типа** — здесь два варианта:
  - `NewtonsoftJsonReflectionService` — читает `[JsonProperty]`, `[JsonIgnore]` и другие Newtonsoft-атрибуты. Пакет: `NJsonSchema.NewtonsoftJson`.
  - `SystemTextJsonReflectionService` — читает `[JsonPropertyName]`, `[JsonIgnore]` из `System.Text.Json`. Встроен в `NJsonSchema` (net6.0+).

**Как выбрать**: если твой backend сериализует через `System.Text.Json` — используй встроенный сервис. Если через `Newtonsoft.Json` — подключай `NJsonSchema.NewtonsoftJson` дополнительно.

## Зависимости

| Пакет | Роль |
|---|---|
| **Newtonsoft.Json** | Основная сериализация JsonSchema |
| **Namotion.Reflection** | Расширенная рефлексия: XML docs, nullable annotations, `ContextualType` |
| **YamlDotNet** | YAML (только в `NJsonSchema.Yaml`) |

## Сборка

- **Мультитаргетинг**: `netstandard2.0`, `net462`, `net6.0`, `net8.0`
- **Скрипты**: `build.ps1`, `build.cmd`, `build.sh`
- **CI**: Azure DevOps
- **NUKE**: конфиг в `.nuke/` — оркестратор билда

## Тестирование

Организация тестов зеркалит функции:

- `NJsonSchema.Tests/Schema/` — модель, `$ref`, композиция, наследование
- `NJsonSchema.Tests/Generation/` — генерация схемы из типов
- `NJsonSchema.Tests/Validation/` — валидатор
- `NJsonSchema.Tests/Serialization/` — round-trip JSON

Code generation тестируется через **approval-тесты**: генерируешь код, сравниваешь с сохранённым `.approved.cs` / `.approved.ts` snapshot'ом.

## Отношение к NSwag

NSwag использует NJsonSchema **как NuGet-пакет** (не project reference). NSwag переиспользует:

- **Модель JsonSchema** — OpenApiSchema в NSwag наследуется/агрегирует JsonSchema
- **JsonSchemaGenerator** — для генерации схемы response/request DTO из .NET-типа
- **CSharpGenerator / TypeScriptGenerator** — для генерации DTO-классов в клиентах NSwag
- **CSharpTypeResolver / TypeScriptTypeResolver** — для разрешения имён типов

## Дальше

- Генерация схемы из типа: [njsonschema-schema-generation.md](njsonschema-schema-generation.md)
- Генерация кода из схемы: [njsonschema-code-generation.md](njsonschema-code-generation.md)
