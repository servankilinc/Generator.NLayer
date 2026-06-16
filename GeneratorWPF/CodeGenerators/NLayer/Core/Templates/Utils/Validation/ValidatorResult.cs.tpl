using System.Diagnostics.CodeAnalysis;

namespace {{ core_project_name }}.Utils.Validation;

public class ValidatorResult
{
    [MemberNotNullWhen(false, nameof(Failures))]
    public bool IsValid { get; set; }
    public Dictionary<string, string[]>? Failures { get; set; }

    public ValidatorResult(bool isValid)
    {
        IsValid = isValid;
    }
    public ValidatorResult(bool isValid, Dictionary<string, string[]> failures) : this(isValid)
    {
        Failures = failures;
    }

    #region Static Factory
    public static ValidatorResult Success() => new ValidatorResult(true);
    public static ValidatorResult Failure(Dictionary<string, string[]> failures) => new ValidatorResult(false, failures);
    #endregion
}
