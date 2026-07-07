# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

A metadata-driven .NET solution generator. Users define a project model (entities, fields, relations, DTOs, validations, app settings) through a REST API; that metadata is stored in per-project SQLite databases. The generator then emits a complete NLayer .NET solution (Core / Model / DataAccess / Business / API / WebUI layers) to disk by running `dotnet` CLI commands and rendering Scriban `.tpl` templates. A frontend client (expected at `http://localhost:5173`, see the CORS policy in `Generator.API/Program.cs`) consumes the API and receives generation progress over SignalR.

## Commands

```powershell
# Build
dotnet build GeneratorNTier.sln

# Run the API (dev: OpenAPI at /openapi, Scalar UI at /scalar)
dotnet run --project Generator.API

# EF Core migrations (two contexts, both live in Generator.Domain/Migrations)
dotnet ef migrations add <Name> --project Generator.Domain --startup-project Generator.API --context LocalContext
dotnet ef migrations add <Name> --project Generator.Domain --startup-project Generator.API --context ProjectContext
```

There are no test projects and no linting configuration.

**Migration gotcha:** `ProjectContext` is configured dynamically per active project. To scaffold a migration for it, temporarily uncomment the static `OnConfiguring` block at the top of `Generator.Domain/Context/ProjectContext.cs` (and re-comment it afterward).

## Architecture

Two projects, both `net10.0`:

- **Generator.API** — ASP.NET Core minimal-API host. Endpoints are static extension classes in `Generator.API/Endpoints/` (`Map*Endpoints(this IEndpointRouteBuilder)`), each mapped in `Program.cs`. Also hosts the SignalR `CommunicationHub` (`/comunication-hub`) and the AI assistant.
- **Generator.Domain** — everything else: EF Core metadata storage (`Core/Entities`, `Repository/`), and the code generator itself (`CodeGenerators/`).

### Data layer: two DbContexts + active project

- `LocalContext` (`Generator.Domain/Context/LocalContext.cs`) — self-configured SQLite DB (`LocalProjectDatabase.db`) holding the list of Projects plus AI chat persistence (`Conversations`, `ConversationMessages`, cascade-delete, `Conversation.ProjectId` → Project). It migrates itself in its constructor and is instantiated directly with `new LocalContext()` (not DI). `LocalContextDesignTimeFactory` suppresses that constructor migration for `dotnet ef` scaffolding.
- `ProjectContext` — holds all generator metadata (entities, fields, relations, DTOs, validations, AppSettings). One SQLite database **per project**; the connection string is built in `Program.cs` from `IActiveProjectStore.ActiveProject`.
- `IActiveProjectStore` (singleton) holds the currently selected project; clients set it via `POST /activeProject`. Most repositories/endpoints operate against whichever project is active.
- `AppSetting` is registered in DI as a **scoped service** that throws if the row with Id=1 doesn't exist in the active project DB — anything depending on it fails until app settings are saved.

### Generation pipeline

- `IGenerationStep` (`Generator.Domain/CodeGenerators/Pipeline/`) — one implementation per generated layer (`NLayerSolutionGenerator`, `NLayerCoreGenerator`, `NLayerModelGenerator`, `NLayerDataAccessGenerator`, `NLayerBusinessGenerator`, `NLayerAPIService`, `NLayerWebUIGenerator`), each exposing `Name`, `Order`, `ProgressWeight`, and `Execute(appSetting, log)`. All are registered in DI as `IGenerationStep` in `Program.cs`.
- `GenerationPipeline.Run` executes the steps ordered by `Order`, reporting weighted progress and log messages via callbacks. `NLayerGeneratorService` validates `AppSetting` (Path/SolutionName/ProjectName) and runs the pipeline.
- `GET /start-generate` (`GenerationEndpoints.cs`) fires the pipeline in a background `Task.Run` with its own DI scope and streams log/progress to all SignalR clients (`AppendToResults` / `Progress` events).
- Shared generator services in `CodeGenerators/Services/`: `DotnetCliService` (runs `dotnet new/sln/add package/restore`), `TemplateRenderer` (Scriban parse/render with a static cache), `FileSystemService`, `RoslynSyntaxHelper`, `ValidationRuleGenerator`.

### Templates

Scriban templates live under `Generator.Domain/CodeGenerators/NLayer/<Layer>/Templates/**/*.tpl`. `TemplateRenderer` reads them from `AppContext.BaseDirectory` at runtime, so every template file must have a `<None Update ... CopyToOutputDirectory>` entry in `Generator.Domain.csproj` — **when adding a new `.tpl` file, add the csproj entry too or it won't be found at runtime.**

### AI assistant

`Generator.API/Services/AI/` uses Microsoft Semantic Kernel (Ollama or OpenAI-compatible provider, configured via the `AIConfiguration` section in `appsettings.json` and bound to `AiOptions`; default is Ollama `qwen3:4b` at `localhost:11434`). The Kernel is DI-registered in `Program.cs` (`AddKernel()` + scoped `KernelPlugin` registrations so plugins get the active project's scoped repositories/DbContext). Plugins in `Services/AI/Plugins/` (`ProjectBuilderPlugin`, `DtoPlugin`, `SettingsPlugin`, `ValidationPlugin`) expose the metadata model as kernel functions; the system prompt is `Services/AI/Prompts/SystemPrompt.md` (copied to output, loaded from `AppContext.BaseDirectory`). The flow is human-in-the-loop, orchestrated by `AiChatService`: read-only functions marked with `[InspectorFunction]` (see `AiToolPolicy`) auto-execute inside a bounded tool loop (`MaxToolIterations`), while mutating tool calls are returned as *proposals* that the client confirms via the execute endpoint. Endpoints under `/api/generation/ai-setup` plus the `/conversation/*` CRUD routes live in `AIEndpoints.cs` as thin wrappers over `AiChatService`. Function calling uses the connector-agnostic `FunctionChoiceBehavior.Auto(autoInvoke: false)` — do not use the legacy OpenAI-only `ToolCallBehavior`, which the Ollama connector ignores. New read-only kernel functions must be tagged `[InspectorFunction]` or they will require user approval.

Conversations are persisted in `LocalContext`: the chat endpoint auto-creates a `Conversation` for the active project on the first message (returns `conversationId`; pass it back to continue — DB history then replaces client-sent history), and `/execute` records results as a `tool` message when given `conversationId`.

`ProjectSnapshotService` (scoped, uses the active project's `ProjectContext`) builds a compact "CURRENT PROJECT STATE" summary (entities+fields, relations, DTOs) that `AiChatService` injects as a second system message on every request, so the model knows the schema without an inspector round-trip. It is best-effort: returns null (and is skipped) when no project is active or the DB is unreachable. Dictionary IDs mentioned in plugin `[Description]`s and `SystemPrompt.md` must match the seeded values in the `Project_InitialCreate` migration (CrudTypes: 1=Read..4=Delete; RelationTypes: only 1=OneToOne, 2=OneToMany; DeleteBehaviorTypes: 1=Cascade, 3=Restrict, 6=SetNull, 7=NoAction) — they were once wrong and caused the model to pick Cascade while intending Restrict. Note: nothing auto-adds an `Id` field at metadata level; `CreateRelation` requires the primary entity to have a field literally named `Id` (the system prompt instructs the model to add it after `CreateEntity`).

Hard-won Ollama constraints (all encoded in `AiOptions`/`AiChatService`, don't regress them):
- `num_ctx` must be raised via `ExtensionData["num_ctx"]` — Ollama's 4096 default silently truncates the first tool definitions and the model claims tools don't exist.
- Kernel function parameters must not be nullable value types (`bool?`, `int?`) — their JSON schema (`"type":["boolean","null"]`) crashes OllamaSharp's tool serialization.
- The Microsoft.Extensions.AI bridge returns tool calls as `FunctionName="Plugin_Function"` with empty `PluginName`; `AiChatService.ResolveFunctionIdentity` splits it — required for both inspector detection and function lookup.
- LLM calls need a long-timeout `HttpClient` (`RequestTimeoutSeconds`); the 100 s default times out on local models.
