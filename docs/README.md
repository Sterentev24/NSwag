# NSwag / NJsonSchema — техническая документация

Обзор архитектуры и внутреннего устройства двух связанных репозиториев Rico Suter'а:

- **NSwag** — OpenAPI/Swagger toolchain для .NET, ASP.NET Core и TypeScript
- **NJsonSchema** — библиотека для работы с JSON Schema (базовая зависимость NSwag)

## Файлы

### Обзор

- [toolchain-overview.md](toolchain-overview.md) — как NSwag и NJsonSchema связаны, что делает каждая, как выглядит полный конвейер (контроллеры → спека → клиенты)

### NSwag

- [nswag-architecture.md](nswag-architecture.md) — назначение, слои, ключевые точки входа, зависимости, сборка
- [nswag-layers.md](nswag-layers.md) — детальная разбивка проектов `src/*` по слоям с ответственностью каждого
- [nswag-code-generation.md](nswag-code-generation.md) — как работает генерация C# и TypeScript клиентов (пайплайн, шаблоны, TypeResolver)
- [nswag-extension-points.md](nswag-extension-points.md) — процессоры (`IDocumentProcessor`, `IOperationProcessor`), настройки, Liquid-шаблоны, кастомные type mapper'ы

### NJsonSchema

- [njsonschema-architecture.md](njsonschema-architecture.md) — назначение, слои, точки входа
- [njsonschema-schema-generation.md](njsonschema-schema-generation.md) — генерация JSON Schema из .NET-типов через рефлексию
- [njsonschema-code-generation.md](njsonschema-code-generation.md) — генерация C#/TypeScript кода из JSON Schema

## Как читать документацию

Если задача — **встроить NSwag в проект** (backend или generation клиента):
1. `toolchain-overview.md` — понять полный конвейер
2. `nswag-architecture.md` — выбрать точку входа
3. `nswag-code-generation.md` — если генерируешь клиента
4. `nswag-extension-points.md` — если нужно кастомизировать

Если задача — **разобраться в исходниках** NSwag/NJsonSchema:
1. `nswag-layers.md` + `njsonschema-architecture.md` — карта репозиториев
2. Дальше по конкретной теме

## Источники

Документация написана по результатам изучения репозиториев в:

- `C:\work\NSwag\NSwag`
- `C:\work\NSwag\NJsonSchema`

Дата: 2026-07-01. Версии на момент изучения: NSwag ~14.x, NJsonSchema 11.0.x.
