using System.Reflection;
using Microsoft.SemanticKernel;

namespace Generator.API.Services.AI;

/// <summary>
/// Marks a kernel function as a read-only "inspector" tool that the AI assistant
/// may execute directly without user approval. Functions without this attribute
/// are treated as mutating and are returned to the client as proposals.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InspectorFunctionAttribute : Attribute { }

public static class AiToolPolicy
{
    private static readonly Lazy<HashSet<string>> _inspectorFunctions = new(ScanInspectorFunctions);

    public static bool IsInspector(string functionName) => _inspectorFunctions.Value.Contains(functionName);

    private static HashSet<string> ScanInspectorFunctions()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var methods = typeof(AiToolPolicy).Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetCustomAttribute<InspectorFunctionAttribute>() is not null);

        foreach (var method in methods)
        {
            var kernelFunction = method.GetCustomAttribute<KernelFunctionAttribute>();
            set.Add(kernelFunction?.Name ?? method.Name);
        }

        return set;
    }
}
