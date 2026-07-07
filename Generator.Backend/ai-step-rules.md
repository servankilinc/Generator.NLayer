# AI Validation Step — Tasarım ve Kural Dokümanı (Rapor Odaklı)

Bu doküman, generation pipeline'ının sonuna eklenecek **AI Validation Step**'in tasarımını,
kural setini ve rapor modelini tanımlar.

> **Temel ilke: Bu step üretilen projede HİÇBİR kod değişikliği yapmaz.**
> Step salt-okunur (read-only) çalışır: üretilen solution'ı analiz eder, kuralları kontrol eder
> ve detaylı bir rapor üretir. Başarılı kontroller de raporda açıkça **Passed** olarak belirtilir.
> Rapor, kullanıcının dilerse harici bir LLM aracına (Claude, ChatGPT, Copilot vb.) verip
> düzeltmeleri kendi kontrolünde yaptırabileceği, kendi başına yeterli (self-contained) bir
> formatta üretilir — bu yüzden her bulgu dosya yolu, satır numarası, gerekçe ve çözüm önerisini
> içerir.

Kullanıcının verdiği taslak liste, mevcut mimari ve üretilen çıktı gerçeğine göre gözden
geçirilmiş ve düzeltilmiştir (bkz. [Bölüm 2](#2-taslak-listeye-yapılan-düzeltmeler)).

---

## 1. Mimariye Entegrasyon

### 1.1 Step tanımı

- Yeni sınıf: `AiValidationStep : IGenerationStep`
- `Name = "AI Validation"`, `Order = 7` (WebUI'den sonra, pipeline'ın son adımı), `ProgressWeight = 15`
- Mevcut `IGenerationStep` imzası **değiştirilmeyecek** (`bool Execute(AppSetting, Action<string> log)`).
  LLM çağrıları async olduğundan step içinde `GetAwaiter().GetResult()` ile senkronize edilir.
  Bu kabul edilebilir çünkü pipeline zaten `GenerationEndpoints` içinde `Task.Run` ile arka planda,
  kendi DI scope'unda çalışıyor.
- **Step asla pipeline'ı fail etmez.** Kod bu noktada zaten üretilmiştir; validation yalnızca bulgu
  raporlar. `Execute` her koşulda `true` döner (iç hata dahil — hata loglanır, rapor "incomplete"
  işaretlenir).
- **Yazma yetkisi yoktur:** Step üretilen solution klasöründe yalnızca **rapor dosyalarını** yazar
  (`AI-VALIDATION-REPORT.md` + `ai-validation-report.json`). Kaynak koda, csproj'lara,
  appsettings'e hiçbir müdahale yapılmaz.

### 1.2 Katman yerleşimi (SK bağımlılığı sorunu)

Semantic Kernel yalnızca `Generator.API`'de referanslı; generator kodu `Generator.Domain`'de.
Bu ayrımı korumak için:

```
Generator.Domain/CodeGenerators/Pipeline/Validation/
    AiValidationStep.cs               // IGenerationStep implementasyonu (read-only analiz)
    IAiAnalysisClient.cs              // LLM abstraction — Domain SK'dan habersiz kalır
    SolutionScanner.cs                // üretilen solution'ı tarar, context üretir
    SolutionAnalysisContext.cs
    Models/                           // ValidationIssue, RuleResult, AiValidationReport, ...
    Rules/                            // IValidationRule implementasyonları
    Reporting/                        // ReportBuilder (md + json), konsol/SignalR formatlayıcı

Generator.API/Services/AI/Validation/
    KernelAiAnalysisClient.cs         // IAiAnalysisClient — IChatCompletionService üzerinden
    Prompts/ValidationPrompt.md       // csproj'a CopyToOutputDirectory ile eklenecek
```

```csharp
public interface IAiAnalysisClient
{
    /// <summary>Provider konfigüre edilmiş ve erişilebilir mi (kısa timeout'lu health check).</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Tek atımlık analiz çağrısı. Tool/function calling yok; düz completion.</summary>
    Task<string> AnalyzeAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
```

### 1.3 DI kaydı (`Program.cs`)

```csharp
builder.Services.AddScoped<IAiAnalysisClient, KernelAiAnalysisClient>();
builder.Services.AddScoped<SolutionScanner>();
// Kurallar: her kural ayrı kayıt (assembly-scan yerine explicit — mevcut stil bu)
builder.Services.AddScoped<IValidationRule, SolutionStructureRules>();
builder.Services.AddScoped<IValidationRule, DependencyInjectionRules>();
// ... diğer kurallar
builder.Services.AddScoped<IGenerationStep, AiValidationStep>(); // diğer step'lerle aynı satır bloğuna
```

Step **her zaman kayıtlıdır**; LLM yoksa çalışma anında kendini uyarlar (koşullu DI kaydı yerine
runtime davranış tercih edilir — `/start-generate` akışı ve progress hesabı hiç değişmez).

### 1.4 Çalışma koşulu (LLM tanımlı mı?)

Step başında sırayla kontrol edilir:

1. `AIConfiguration:EnableValidationStep` (yeni bool ayar, `AiOptions`'a eklenir, default `true`).
   `false` ise step tamamen atlanır.
2. `KernelAiAnalysisClient.IsAvailableAsync()`:
   - **OpenAI**: `ApiKey` boş değilse kullanılabilir kabul edilir.
   - **Ollama**: `GET {Endpoint}/api/tags` — 5 sn timeout'lu ayrı kısa `HttpClient` ile
     (LLM çağrılarındaki uzun timeout'lu client health check için kullanılmaz).

LLM erişilemezse: **deterministik kurallar yine de çalışır** ve rapor üretilir; yalnızca
`RequiresAi == true` olan kurallar ve skorlama `Skipped ("LLM not available")` işaretlenir.
Rapor bunu gizlemez — hangi kontrolün neden atlandığı raporda görünür.

---

## 2. Taslak Listeye Yapılan Düzeltmeler

Kullanıcının verdiği kontrol listesi üzerinden geçildi; üretilen çıktının gerçeğine uymayan
maddeler aşağıdaki gibi düzeltildi:

| Taslaktaki madde | Durum | Gerekçe |
|---|---|---|
| **CQRS / MediatR kontrolleri** | **Çıkarıldı** | Üretilen solution'da MediatR yok, handler/pipeline behavior/notification kavramı yok. `Commands/Queries` sadece DTO klasör adlandırması. Yerine **Service Layer kontrolleri** (3.4) kondu. |
| **Clean Architecture kontrolleri** | **NLayer'a uyarlandı** | Çıktı Clean Architecture değil, klasik NLayer. "Domain bağımsız mı" yerine NLayer bağımlılık yönü kontrol edilir (3.1). |
| **IEntityTypeConfiguration eksik mi** | **Değiştirildi** | Generator entity konfigürasyonunu ayrı config sınıflarında değil, `OnModelCreating` içinde inline üretir. Kural: OnModelCreating'de her entity için key/ilişki tanımı var mı. |
| **Tahmini compile hatalarını bul** | **Gerçek build ile değiştirildi** | LLM'e derleme hatası tahmin ettirmek güvenilmez. `DotnetCliService` ile gerçek `dotnet build` koşulur (salt-okunur bir doğrulamadır, kod değiştirmez); çıkan hatalar kesin veri olarak rapora girer. |
| **ConfigureAwait gerekli mi** | **Çıkarıldı** | ASP.NET Core uygulamalarında SynchronizationContext yok; `ConfigureAwait(false)` üretilen kodda anlamlı bir kontrol değil. |
| **Secret hardcode / connection string** | **Severity düşürüldü (Warning)** | Generator, appsettings'e bilinçli olarak dev connection string yazar; bu tasarım gereğidir. Kural yalnızca `.cs` dosyalarına gömülü secret/connection string arar; appsettings içindekiler bilgi notu olur. |
| **JWT / Authorization / Authentication eksik mi** | **Koşullu yapıldı** | Auth üretimi `AppSetting` bayraklarına bağlı (ör. RefreshToken/Identity üretimi koşullu). Auth metadata'da kapalıysa bu kurallar `Skipped` işaretlenir, hata sayılmaz. |
| **Otomatik düzeltme (auto-fix)** | **Tamamen çıkarıldı** | Kullanıcı kararı: step kod değiştirmez. Her bulgu, harici bir LLM aracının uygulayabileceği netlikte **çözüm önerisi** taşır; düzeltme kullanıcının kontrolündedir. |
| **Handler registration (MediatR)** | **Çıkarıldı** | Yukarıdaki gibi. |
| Diğer tüm başlıklar | Korundu | Bölüm 3'te kural kataloğuna dönüştürüldü. |

Ayrıca taslağa **eklenen** en önemli yetenek: step, aktif projenin `ProjectContext` metadata'sına
(repository'ler üzerinden) erişir. Böylece jenerik lint değil, **"niyet vs çıktı" karşılaştırması**
yapılır: metadata'daki her Entity/DTO/Relation/Validation için üretilmesi *gereken* dosya ve kayıt
bilinir; eksik olan kesin olarak tespit edilir. Bu kontrollerin çoğu LLM'siz, deterministik yapılır.

---

## 3. Kural Kataloğu

Her kural bir kategoriye aittir, `Deterministic` (Roslyn / dosya sistemi / metadata karşılaştırma)
veya `AI` (LLM analizi) olarak işaretlenir. Deterministik olabilen hiçbir kontrol LLM'e verilmez.
**Tüm kurallar salt-okunurdur** — sonuçları yalnızca rapor beslemek için kullanılır.

### 3.1 Solution Structure — `SOL-xxx` (Deterministic)
- Beklenen 6 proje (Core, Model, DataAccess, Business, API, WebUI) `.sln` içinde var mı, csproj dosyaları diskte var mı.
- Project reference grafiği beklenen NLayer yönüne uyuyor mu:
  `Core ← Model ← DataAccess ← Business ← API/WebUI` (ters yönde referans = **Error**).
- Circular dependency (csproj graf üzerinde döngü tespiti).
- Gereksiz reference (ör. Core'un başka projeye referansı).
- Beklenen NuGet paketleri csproj'larda mevcut mu (AutoMapper, FluentValidation, EF Core, Serilog, JwtBearer…).

### 3.2 Namespace & Folder — `NS-xxx` (Deterministic)
- Namespace == `{ProjeAdı}.{KlasörYolu}` kuralına uyum.
- Duplicate namespace + aynı isimde çakışan tip.
- Dosya adı == içerdiği public tip adı.

### 3.3 Dependency Injection — `DI-xxx` (Deterministic + AI)
- *(Det.)* Metadata'daki her entity için `I{Entity}Service` → `{Entity}Manager` kaydı `ServiceRegistration` içinde var mı.
- *(Det.)* Her repository interface'inin implementasyonu ve kaydı var mı.
- *(Det.)* Kayıtlı ama solution'da kullanılmayan servis var mı.
- *(AI)* Lifetime uygunluğu: DbContext'e dokunan servis Singleton kayıtlıysa **Error**; şüpheli durumlar **Warning**.

### 3.4 Service Layer — `SVC-xxx` (Deterministic) *(CQRS bölümünün yerine)*
- Her entity için Abstract/Concrete servis çifti üretilmiş mi.
- Metadata'daki her CRUD DTO'su için servis metodu var mı (Create/Update/Delete/Read eşleşmesi).
- Servis metodlarında Result pattern (`IResult`/`ResultData`) kullanımı tutarlı mı.

### 3.5 FluentValidation — `VAL-xxx` (Deterministic)
- Metadata'da validation tanımlı her DTO için validator sınıfı üretilmiş mi.
- Validator'lar `AddValidatorsFromAssembly` kapsamında mı (Program.cs kaydı mevcut mu).
- Metadata'daki her kural (`ValidationRepository`) üretilen `RuleFor` çıktısında karşılık buluyor mu.

### 3.6 AutoMapper — `MAP-xxx` (Deterministic)
- Her entity için MappingProfile üretilmiş mi; her DTO'nun `CreateMap`'i var mı.
- `AddAutoMapper` kaydı Program.cs'te mevcut mu.
- Command DTO'ları için ters yön mapping (`ReverseMap` ya da ayrı map) gerekli/mevcut mu.

### 3.7 Entity Framework — `EF-xxx` (Deterministic)
- Metadata'daki her entity için `DbSet` var mı (Log/Archive/RefreshToken dahil, koşullarına göre).
- Her entity'de `Id` alanı / primary key tanımı var mı.
- Metadata'daki her Relation, `OnModelCreating`'de FK + `DeleteBehavior` ile karşılık buluyor mu.
- DeleteBehavior mantık kontrolü (ör. zorunlu FK üzerinde SetNull = **Error**).
- Interceptor'lar (Audit/Archive/SoftDelete) context'e bağlanmış mı.

### 3.8 API — `API-xxx` (Deterministic + AI)
- *(Det.)* Metadata'da CRUD tanımlı her entity için controller/endpoint üretilmiş mi.
- *(Det.)* Route çakışması (aynı verb + aynı route şablonu).
- *(AI)* HTTP verb ve status code kullanımı uygunluğu (ör. Create'in GET olması).

### 3.9 Exception Handling & Logging — `ERR-xxx` (Deterministic)
- `ExceptionHandleMiddleware` üretilmiş ve Program.cs'te `Use...` ile bağlanmış mı.
- Validation exception'ları middleware'de ayrı ele alınıyor mu.
- Serilog konfigürasyonu + `ILoggingService` kaydı mevcut mu; middleware hataları logluyor mu.

### 3.10 Configuration & Security — `CFG-xxx` / `SEC-xxx` (Deterministic + AI)
- *(Det.)* appsettings.json'da ConnectionString, TokenSettings, CacheSettings anahtarları eksiksiz mi; Options Pattern bağlamaları (`Configure<T>` / `GetSection`) karşılıklı tutarlı mı.
- *(Det.)* `.cs` dosyalarına gömülü connection string / secret taraması (**Warning**).
- *(Det., koşullu)* Auth aktifse: JWT ayarları, `AddAuthentication`/`AddAuthorization` ve `UseAuthentication` middleware sırası doğru mu.

### 3.11 Async — `ASY-xxx` (Deterministic + AI)
- *(Det.)* `async` olup `await` içermeyen metodlar; `Task` dönen ama senkron implementasyona sarılmış metodlar; `.Result`/`.Wait()` kullanımı (**Error**).
- *(AI)* Async'e çevrilmesi faydalı senkron I/O noktaları (**Suggestion**).

### 3.12 Code Quality & Performance — `CQ-xxx` / `PERF-xxx` (AI, bütçeli)
- Dead code, duplicate code, uzun metod, magic string/number, SRP ihlali, god object, primitive obsession.
- Gereksiz `ToList()`, N+1 riski (Include'suz navigation erişimi), gereksiz allocation/boxing, LINQ sadeleştirme.
- Bu kategori tamamen **Suggestion** üretir, asla Error değil. Dosya başına özet gönderilir (aşağıda token bütçesi).

### 3.13 Build Analizi — `BLD-xxx` (Deterministic + AI)
- `DotnetCliService` ile solution köküne `dotnet build -clp:ErrorsOnly` koşulur (yalnızca doğrulama; çıktı `bin/obj` dışında hiçbir şeyi değiştirmez).
- Compiler hataları/uyarıları (missing using, missing reference, ambiguous type, nullable CS86xx uyarıları…) doğrudan **Error/Warning** olarak rapora girer — tahmine gerek yok.
- Her hata için LLM'den kısa açıklama + fix önerisi istenir (LLM varsa); öneri rapora yazılır, **uygulanmaz**.

### 3.14 Kod Tutarlılığı — `STY-xxx` (Deterministic + AI)
- *(Det.)* İsimlendirme: interface `I` prefix, dosya/sınıf adı eşleşmesi, klasör kalıpları (Abstract/Concrete, Dtos/{Entity}/{Commands|Queries}).
- *(AI)* Sealed/partial/record önerileri (**Info/Suggestion**).

---

## 4. Rule Engine Tasarımı

### 4.1 Sözleşmeler

```csharp
public interface IValidationRule
{
    string Id { get; }            // "DI-001"
    string Category { get; }      // RuleCategories sabitlerinden
    string DisplayName { get; }
    bool RequiresAi { get; }      // true ise LLM yokken Skipped
    Task<RuleResult> EvaluateAsync(SolutionAnalysisContext context, CancellationToken ct);
}

public static class RuleCategories
{
    public const string SolutionStructure = "Solution Structure";
    public const string DependencyInjection = "Dependency Injection";
    public const string ServiceLayer = "Service Layer";
    public const string EntityFramework = "Entity Framework";
    public const string Validation = "FluentValidation";
    public const string Mapping = "AutoMapper";
    public const string Api = "API";
    public const string ExceptionsAndLogging = "Exception Handling & Logging";
    public const string Configuration = "Configuration";
    public const string Security = "Security";
    public const string Async = "Async";
    public const string CodeQuality = "Code Quality";
    public const string Performance = "Performance";
    public const string Build = "Build";
    public const string Consistency = "Consistency";
}
```

Yeni kural eklemek = `IValidationRule` implemente eden bir sınıf + Program.cs'e bir DI satırı.
AI kuralları `IAiAnalysisClient`'ı constructor'dan alır; deterministik kurallar almaz.

### 4.2 SolutionAnalysisContext

`SolutionScanner` step başında **bir kez** üretir; tüm kurallar bunu paylaşır (her kural diski
yeniden taramaz):

```csharp
public class SolutionAnalysisContext
{
    public AppSetting AppSetting { get; init; }
    public string SolutionPath { get; init; }
    public IReadOnlyList<ProjectInfo> Projects { get; init; }        // csproj + referans grafı + paketler
    public IReadOnlyList<SourceFileInfo> SourceFiles { get; init; }  // path, namespace, tip adları (Roslyn parse)
    public IReadOnlyList<DiRegistration> DiRegistrations { get; init; } // ServiceRegistration + Program.cs'ten
    // Metadata tarafı (niyet): ProjectContext repository'lerinden step başında snapshot alınır
    public IReadOnlyList<Entity> MetadataEntities { get; init; }
    public IReadOnlyList<Dto> MetadataDtos { get; init; }
    public IReadOnlyList<Relation> MetadataRelations { get; init; }
    public string GetFileContent(string relativePath);              // cache'li, SALT-OKUNUR erişim
}
```

### 4.3 Sonuç modelleri (yapılandırılmış — konsol, UI ve harici LLM aynı modeli tüketir)

```csharp
public enum IssueSeverity { Info, Suggestion, Warning, Error }

public class ValidationIssue
{
    public string RuleId { get; set; }
    public IssueSeverity Severity { get; set; }
    public string? FilePath { get; set; }      // solution-relative — harici LLM dosyayı bununla bulur
    public int? LineNumber { get; set; }
    public string Message { get; set; }        // problem nedir
    public string Reason { get; set; }         // neden problem (runtime etkisi dahil)
    public string? SuggestedFix { get; set; }  // uygulanabilir netlikte çözüm önerisi (kod bloğu dahil olabilir)
    public string? CodeSnippet { get; set; }   // problemli satırların kısa alıntısı — rapor tek başına yeterli olsun diye
}

public class RuleResult
{
    public string RuleId { get; set; }
    public string Category { get; set; }
    public string DisplayName { get; set; }
    public bool Passed => !Skipped && Issues.All(i => i.Severity <= IssueSeverity.Suggestion);
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public int ItemsChecked { get; set; }      // "Passed" derken kaç öğenin kontrol edildiği raporda görünsün
    public List<ValidationIssue> Issues { get; set; } = new();
}

public class AiValidationReport
{
    public DateTime GeneratedAtUtc { get; set; }
    public string SolutionName { get; set; }
    public int FilesAnalyzed { get; set; }
    public int ChecksPerformed { get; set; }
    public int Passed { get; set; }            // Passed == true olan kural sayısı
    public int Skipped { get; set; }
    public int Warnings { get; set; }          // Warning severity'li bulgu sayısı
    public int Errors { get; set; }            // Error severity'li bulgu sayısı
    public int Suggestions { get; set; }
    public bool Complete { get; set; }         // step iç hata ile yarım kaldıysa false
    public List<RuleResult> Results { get; set; } = new();
    public ArchitectureScores? Scores { get; set; }   // LLM yoksa null
}

public class ArchitectureScores
{
    public ScoreItem OverallArchitecture { get; set; }   // 0-100 + Justification
    public ScoreItem CodeQuality { get; set; }
    public ScoreItem LayeringCompliance { get; set; }    // "Clean Architecture Uyum" yerine NLayer uyumu
    public ScoreItem Maintainability { get; set; }
    public ScoreItem ProductionReadiness { get; set; }
}
public class ScoreItem { public int Score { get; set; } public string Justification { get; set; } }
```

### 4.4 Token bütçesi (Ollama gerçeği)

Varsayılan model `qwen3:4b` / 16k context ile 100+ dosyalık solution tek prompt'a sığmaz:

- AI kuralları dosyanın tamamını değil, kuralın istediği **seçilmiş kesitleri** alır
  (context'teki selector'lar: "tüm servis metod imzaları", "Program.cs DI bloğu" gibi).
- `AiOptions`'a eklenir: `MaxValidationAiCalls` (default 20), `MaxPromptChars` (default 12000).
  Bütçe dolunca kalan AI kuralları `Skipped("AI budget exceeded")` işaretlenir — rapor bunu gizlemez.
- Skorlama tek çağrıdır: LLM'e dosyalar değil, **aggregate bulgu listesi** gönderilir (küçük prompt).

---

## 5. Rapor Tasarımı

Rapor bu step'in **tek çıktısıdır**; üç kanala yazılır ve harici bir LLM aracına verildiğinde
düzeltmelerin yapılabilmesi için gereken tüm bağlamı taşır.

### 5.1 Kanallar

1. **SignalR (mevcut kanal):** `log` callback'i üzerinden kategori özetleri ve final özet akar.
   Yeni SignalR event'i **eklenmez**; mevcut `AppendToResults`/`Progress` akışı korunur.
2. **Dosya (asıl teslimat):** üretilen solution köküne yazılır:
   - `AI-VALIDATION-REPORT.md` — insan okunur + **LLM'e verilmeye hazır** tam rapor.
   - `ai-validation-report.json` — `AiValidationReport` serileştirmesi; UI detay ekranı bunu tüketir.
3. **Endpoint (opsiyonel, ikinci faz):** `GET /generation/validation-report` — son raporun
   JSON'ını döner (`GenerationEndpoints.cs`'e thin wrapper).

### 5.2 `AI-VALIDATION-REPORT.md` yapısı (harici LLM'e devredilebilir format)

Rapor, "bu dosyayı bir LLM aracına yapıştır, düzeltmeleri yaptır" senaryosuna göre tasarlanır:

- **Başlık bloğu:** solution adı, yol, tarih, analiz kapsamı (dosya/kural sayısı), raporun
  eksiksiz olup olmadığı.
- **Harici araç için yönerge bloğu:** raporun en başında, düzeltme yapacak LLM'e hitap eden kısa
  sabit bir not: *"Aşağıdaki bulgular {SolutionPath} altındaki projeye aittir. Her bulgu dosya
  yolu, gerekçe ve önerilen çözümü içerir. Yalnızca listelenen sorunları düzelt, mimariyi
  (NLayer katman yönü, Result pattern, repository yapısı) değiştirme."*
- **Kategori bölümleri:** her kategori için Passed / Issues Found / Skipped durumu.
  **Başarılı kategoriler de eksiksiz listelenir** ve neyin kontrol edildiği belirtilir —
  "Passed" tek kelime değil, kanıtlı bir ifadedir:

```
## ✔ Entity Framework — Passed
12 entity için DbSet, PK ve 8 relation için FK/DeleteBehavior kontrol edildi. Sorun bulunamadı.

## ✖ Dependency Injection — 2 Issues Found

### [DI-001] UserService kaydı eksik (Error)
- File   : src/Business/ServiceRegistration.cs
- Line   : 24
- Problem: IUserService için DI kaydı yok.
- Reason : UserController constructor'ı IUserService istiyor; ilk istekte
           InvalidOperationException fırlar.
- Snippet:
    services.AddScoped<IProductService, ProductManager>();
    // IUserService kaydı burada bekleniyordu
- Suggested Fix:
    services.AddScoped<IUserService, UserManager>();

## ⏭ Code Quality — Skipped
Sebep: LLM sağlayıcısı erişilebilir değil.
```

- **Final özet + skorlar** (Bölüm 5.3).

### 5.3 Final özet formatı

```
AI Validation Summary
Files Analysed   : 148
Checks Performed : 132
Passed           : 118
Skipped          : 4
Suggestions      : 6
Warnings         : 10
Errors           : 4

Scores
Genel Mimari          : 86/100 — Katman yönleri doğru, 1 gereksiz referans bulundu.
Code Quality          : 78/100 — 6 uzun metod, 3 magic string.
NLayer Uyum           : 92/100 — Tüm bağımlılıklar tek yönlü.
Maintainability       : 81/100 — Mapping ve validator kapsaması tam.
Production Readiness  : 74/100 — Build temiz; 2 güvenlik uyarısı mevcut.
```

Her skorun `Justification` alanı zorunludur; skorlar LLM yoksa üretilmez ve raporda
"Scores: unavailable (LLM not configured)" satırı yer alır.

---

## 6. Çalışma Akışı (Execute içi fazlar)

1. **Availability** — LLM erişilebilirlik kontrolü (1.4). Sonuç ne olursa olsun deterministik analiz devam eder.
2. **Scan** — `SolutionScanner` context'i kurar (`FilesAnalyzed` burada sayılır). Salt-okunur.
3. **Deterministic rules** — kategori kategori koşulur, her kategori bittiğinde `log` ile ara özet basılır.
4. **Build** — gerçek `dotnet build` (yalnızca doğrulama); hatalar `BLD-xxx` bulgularına dönüşür.
5. **AI rules** — bütçe dahilinde; her çağrı `log("🤖 AI analiz: {kategori}")` ile izlenir.
6. **Scoring** — LLM varsa aggregate bulgulardan 5 skor + gerekçe.
7. **Report** — `AI-VALIDATION-REPORT.md` + `ai-validation-report.json` yazılır, final özet SignalR'a basılır.

Akışta düzeltme fazı **yoktur**; bulgular yalnızca raporlanır.

---

## 7. Uygulama Sırasında Uyulacak Kurallar (özet)

1. `IGenerationStep` imzasına ve mevcut step'lere **dokunma**; yeni step diğerleriyle aynı kalıpta yazılır (try/catch + log + bool).
2. AI Validation **hiçbir koşulda** pipeline sonucunu `false` yapmaz.
3. **Üretilen projede kod değişikliği yapılmaz.** Step'in diske yazdığı tek şey iki rapor dosyasıdır; `FileSystemService` üzerinden kaynak dosya yazma/silme çağrısı bu step'te kullanılamaz.
4. Semantic Kernel referansı `Generator.Domain`'e **eklenmez**; `IAiAnalysisClient` abstraction'ı kullanılır.
5. Deterministik yapılabilen hiçbir kontrol LLM'e devredilmez; LLM yalnızca yorum gerektiren kategorilerde ve skorlamada kullanılır.
6. Compile hatası "tahmini" yapılmaz; gerçek `dotnet build` koşulur.
7. Başarılı kontroller raporda **kanıtıyla** yer alır (`ItemsChecked` + ne kontrol edildiğinin özeti); rapor yalnızca sorunların listesi değildir.
8. Her bulgu, raporu alan harici bir LLM'in başka bağlama ihtiyaç duymadan düzeltebileceği kadar bilgi taşır: dosya yolu, satır, gerekçe, snippet, önerilen fix.
9. Prompt dosyaları `.tpl`/`.md` olarak eklenirse csproj'a `CopyToOutputDirectory` girdisi de eklenir (mevcut template kuralı).
10. Kernel function eklenmiyor (chat asistanından bağımsız akış); dolayısıyla `[InspectorFunction]` gereksinimi bu step için geçerli değil.
11. Yeni kontrol eklemek yalnızca yeni `IValidationRule` sınıfı + DI kaydı gerektirmeli — mevcut kurallara dokunulmamalı.
