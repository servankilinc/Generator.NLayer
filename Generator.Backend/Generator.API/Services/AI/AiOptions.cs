namespace Generator.API.Services.AI;

public class AiOptions
{
    public const string SectionName = "AIConfiguration";

    public string Provider { get; set; } = "Ollama";
    public string ModelId { get; set; } = "qwen3:4b";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ApiKey { get; set; } = "";
    public int MaxTokens { get; set; } = 4000;

    /// <summary>Upper bound for the manual tool-invocation loop to prevent runaway model calls.</summary>
    public int MaxToolIterations { get; set; } = 8;

    /// <summary>Ollama context window (num_ctx). Ollama's default (4096) is too small for the
    /// system prompt plus all tool definitions and silently truncates the first tools.</summary>
    public int ContextLength { get; set; } = 16384;

    /// <summary>HTTP timeout for LLM calls. Local Ollama models can take several minutes;
    /// the HttpClient default of 100 seconds is not enough.</summary>
    public int RequestTimeoutSeconds { get; set; } = 600;

    public bool IsOpenAI => string.Equals(Provider, "OpenAI", StringComparison.OrdinalIgnoreCase);
}
