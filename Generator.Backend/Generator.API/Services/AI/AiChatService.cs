using System.Text.Json;
using Generator.Domain.Context;
using Generator.Domain.Core.Entities.Local;
using Generator.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Generator.API.Services.AI;

/// <summary>
/// Orchestrates the AI assistant chat: builds the chat history, runs the manual
/// tool-invocation loop (inspector tools auto-execute, mutating tools are returned
/// as proposals for user approval), executes approved tool calls, and persists
/// conversations/messages into the local project database.
/// </summary>
public class AiChatService
{
    private const string ProposalMessage = "Aşağıdaki değişiklikleri yapmayı planlıyorum. Lütfen kontrol edip onaylayın.";

    private static readonly Lazy<string?> _systemPrompt = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Services", "AI", "Prompts", "SystemPrompt.md");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    });

    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly IActiveProjectStore _activeProjectStore;
    private readonly ProjectSnapshotService _snapshotService;
    private readonly AiOptions _options;

    public AiChatService(Kernel kernel, IActiveProjectStore activeProjectStore, ProjectSnapshotService snapshotService, IOptions<AiOptions> options)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _activeProjectStore = activeProjectStore;
        _snapshotService = snapshotService;
        _options = options.Value;
    }

    public async Task<AiChatResult> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        using var localContext = new LocalContext();
        var conversation = await ResolveConversationAsync(localContext, request, cancellationToken);

        var chatHistory = BuildChatHistory(request, conversation, _snapshotService.BuildSnapshot());
        var settings = CreateExecutionSettings();

        if (conversation != null)
            AppendMessage(localContext, conversation, "user", request.Prompt);

        for (int iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            var response = await _chatCompletion.GetChatMessageContentAsync(chatHistory, settings, _kernel, cancellationToken);
            chatHistory.Add(response);

            var toolCalls = response.Items.OfType<FunctionCallContent>().ToList();
            if (toolCalls.Count == 0)
            {
                var content = response.Content ?? string.Empty;
                if (conversation != null)
                {
                    AppendMessage(localContext, conversation, "assistant", content);
                    await localContext.SaveChangesAsync(cancellationToken);
                }
                return AiChatResult.FromResponse(content, conversation?.Id);
            }

            var proposedChanges = new List<ProposedToolCallDto>();

            foreach (var toolCall in toolCalls)
            {
                var (pluginName, functionName) = ResolveFunctionIdentity(toolCall.PluginName, toolCall.FunctionName);

                if (AiToolPolicy.IsInspector(functionName))
                {
                    FunctionResultContent resultContent;
                    if (_kernel.Plugins.TryGetFunction(pluginName, functionName, out var function))
                    {
                        try
                        {
                            var result = await function.InvokeAsync(_kernel, toolCall.Arguments, cancellationToken);
                            resultContent = new FunctionResultContent(toolCall, result.GetValue<string>() ?? string.Empty);
                        }
                        catch (Exception ex)
                        {
                            resultContent = new FunctionResultContent(toolCall, $"Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        resultContent = new FunctionResultContent(toolCall, $"Error: Tool '{functionName}' not found.");
                    }
                    chatHistory.Add(resultContent.ToChatMessage());
                }
                else
                {
                    // Mutating tool: do not execute, return it as a proposal for user approval.
                    proposedChanges.Add(new ProposedToolCallDto
                    {
                        Id = toolCall.Id ?? Guid.NewGuid().ToString("N"),
                        PluginName = pluginName,
                        FunctionName = functionName,
                        Arguments = toolCall.Arguments?.ToDictionary(k => k.Key, v => v.Value) ?? new()
                    });
                }
            }

            if (proposedChanges.Count > 0)
            {
                if (conversation != null)
                {
                    AppendMessage(localContext, conversation, "assistant", ProposalMessage, JsonSerializer.Serialize(proposedChanges));
                    await localContext.SaveChangesAsync(cancellationToken);
                }
                return AiChatResult.FromProposals(proposedChanges, conversation?.Id);
            }

            // Only inspector tools were called; loop so the model can use the new context.
        }

        var limitMessage = "The assistant reached the tool-call limit without producing a final answer. Please refine your request and try again.";
        if (conversation != null)
        {
            AppendMessage(localContext, conversation, "assistant", limitMessage);
            await localContext.SaveChangesAsync(cancellationToken);
        }
        return AiChatResult.FromResponse(limitMessage, conversation?.Id);
    }

    public async Task<List<string>> ExecuteToolCallsAsync(AiExecuteRequest request, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();

        foreach (var call in request.ToolCalls)
        {
            var (pluginName, functionName) = ResolveFunctionIdentity(call.PluginName, call.FunctionName);

            if (!_kernel.Plugins.TryGetFunction(pluginName, functionName, out var function))
            {
                results.Add($"Error: Function {pluginName}.{functionName} not found.");
                continue;
            }

            try
            {
                var result = await function.InvokeAsync(_kernel, new KernelArguments(call.Arguments), cancellationToken);
                results.Add(result.GetValue<string>() ?? $"Function {call.FunctionName} executed.");
            }
            catch (Exception ex)
            {
                results.Add($"Error executing {functionName}: {ex.Message}");
            }
        }

        if (request.ConversationId.HasValue)
        {
            using var localContext = new LocalContext();
            var conversation = await localContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, cancellationToken);
            if (conversation != null)
            {
                AppendMessage(localContext, conversation, "tool", string.Join("\n", results), JsonSerializer.Serialize(request.ToolCalls));
                await localContext.SaveChangesAsync(cancellationToken);
            }
        }

        return results;
    }

    /// <summary>
    /// The Microsoft.Extensions.AI bridge reports tool calls with an empty plugin name and a
    /// combined "PluginName_FunctionName" function name; split it back into its parts.
    /// </summary>
    private static (string PluginName, string FunctionName) ResolveFunctionIdentity(string? pluginName, string functionName)
    {
        if (!string.IsNullOrEmpty(pluginName))
            return (pluginName, functionName);

        var separatorIndex = functionName.IndexOf('_');
        if (separatorIndex > 0)
            return (functionName[..separatorIndex], functionName[(separatorIndex + 1)..]);

        return (string.Empty, functionName);
    }

    /// <summary>
    /// Loads the requested conversation, or creates a new one bound to the active project.
    /// Returns null (stateless chat) when no conversation id is given and no project is active.
    /// </summary>
    private async Task<Conversation?> ResolveConversationAsync(LocalContext localContext, AiChatRequest request, CancellationToken cancellationToken)
    {
        if (request.ConversationId.HasValue)
        {
            var existing = await localContext.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, cancellationToken);

            return existing ?? throw new InvalidOperationException($"Conversation '{request.ConversationId}' not found.");
        }

        var projectId = _activeProjectStore.ActiveProject?.Id;
        if (projectId == null)
            return null;

        var conversation = new Conversation
        {
            ProjectId = projectId.Value,
            Title = request.Prompt.Length > 60 ? request.Prompt[..60] : request.Prompt,
            CreateDate = DateTime.Now
        };
        localContext.Conversations.Add(conversation);
        await localContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private static void AppendMessage(LocalContext localContext, Conversation conversation, string role, string content, string? toolCallsJson = null)
    {
        localContext.ConversationMessages.Add(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = role,
            Content = content,
            ToolCallsJson = toolCallsJson,
            CreateDate = DateTime.Now
        });
        conversation.LastMessageDate = DateTime.Now;
    }

    private static ChatHistory BuildChatHistory(AiChatRequest request, Conversation? conversation, string? projectSnapshot)
    {
        var chatHistory = new ChatHistory();

        if (_systemPrompt.Value is not null)
            chatHistory.AddSystemMessage(_systemPrompt.Value);

        // Compact summary of the active project's metadata, rebuilt per request so it is always fresh.
        if (projectSnapshot is not null)
            chatHistory.AddSystemMessage(projectSnapshot);

        if (conversation != null && conversation.Messages.Count > 0)
        {
            // Persisted conversation: replay history from the database.
            foreach (var msg in conversation.Messages.OrderBy(m => m.Id))
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase)) chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)) chatHistory.AddAssistantMessage(msg.Content);
                else if (msg.Role.Equals("tool", StringComparison.OrdinalIgnoreCase)) chatHistory.AddAssistantMessage($"[Applied changes] {msg.Content}");
            }
        }
        else if (request.History != null)
        {
            // Stateless fallback: client-provided history.
            // System messages from the client are ignored on purpose (prompt-injection surface).
            foreach (var msg in request.History)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase)) chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)) chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        chatHistory.AddUserMessage(request.Prompt);
        return chatHistory;
    }

    private PromptExecutionSettings CreateExecutionSettings()
    {
        // Connector-agnostic function calling with manual invocation (human-in-the-loop).
        var functionChoice = FunctionChoiceBehavior.Auto(autoInvoke: false);

        if (_options.IsOpenAI)
        {
            return new OpenAIPromptExecutionSettings
            {
                MaxTokens = _options.MaxTokens,
                FunctionChoiceBehavior = functionChoice
            };
        }

#pragma warning disable SKEXP0070
        return new OllamaPromptExecutionSettings
        {
            NumPredict = _options.MaxTokens,
            FunctionChoiceBehavior = functionChoice,
            // Flows into ChatOptions.AdditionalProperties; OllamaSharp maps "num_ctx" to the
            // request options. Without it Ollama's 4096-token default silently truncates
            // the first tool definitions.
            ExtensionData = new Dictionary<string, object> { ["num_ctx"] = _options.ContextLength }
        };
#pragma warning restore SKEXP0070
    }
}
