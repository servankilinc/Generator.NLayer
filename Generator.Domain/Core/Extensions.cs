using Humanizer;

namespace Generator.Domain.Core;

public static class Extensions
{
    public static string ToCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;


        if (input.Length < 2)
            return input.ToLowerInvariant();

        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }

    public static string DivideToLabelName(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        input = input.Trim();

        // Eğer sonu "id" ile bitiyorsa kaldır (case-insensitive)
        if (input.EndsWith("id", StringComparison.OrdinalIgnoreCase))
        {
            input = input.Substring(0, input.Length - 2);
        }

        // Humanizer ile camelCase’i "Customer Order" gibi yap
        var result = input.Humanize(LetterCasing.Title);

        return result;
    }
}
