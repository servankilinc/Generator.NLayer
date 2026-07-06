using Generator.Domain.Core;
using Generator.Domain.Core.Entities;

namespace Generator.Domain.CodeGenerators.Services;

/// <summary>
/// Generates FluentValidation rule strings from Validation metadata.
/// Extracted from NLayerGeneratorBase.
/// </summary>
public class ValidationRuleGenerator
{
    public string ValidationRule(Validation validation, string fieldName, string? message)
    {
        string rule = string.Empty;

        switch (validation.ValidatorTypeId)
        {
            case (int)Enums.ValidatorTypes.NotEmpty:
                if (string.IsNullOrEmpty(message)) message = "This field is required.";
                rule = $".NotEmpty().WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.NotNull:
                if (string.IsNullOrEmpty(message)) message = "This field cannot be null.";
                rule = $".NotNull().WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.NotEqual:
                var neqValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.NotEqual_Value)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"The value cannot be {neqValue ?? "?"}.";
                rule = $".NotEqual({neqValue}).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.MaxLength:
                var mxLValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.MaxLength_Max)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"This field must be at most {mxLValue ?? "?"} characters long.";
                rule = $".MaximumLength({mxLValue}).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.MinLength:
                var minLValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.MinLength_Min)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"This field must be at least {minLValue ?? "?"} characters long.";
                rule = $".MinimumLength({minLValue}).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.Range:
                var rngMinValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.Range_Min)?.Value : "?";
                var rngMaxValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.Range_Max)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"Value must be between {rngMinValue ?? "?"} and {rngMaxValue ?? "?"}";
                rule = $".InclusiveBetween({rngMinValue}, {rngMaxValue}).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.Regex:
                if (string.IsNullOrEmpty(message)) message = "The format of this field is invalid.";
                var rgPattern = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.Regex_Pattern)?.Value : "?";
                rule = $".Matches({rgPattern}).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.GreaterThan:
                var gtValue = validation.ValidationParams != null ? validation.ValidationParams.First(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.GreaterThan_Value).Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"Value must be greater than {gtValue ?? "?"}";
                rule = $".GreaterThan({gtValue}).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.LessThan:
                var ltValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.LessThan_Value)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"Value must be less than {ltValue ?? "?"}";
                rule = $".LessThan({ltValue}).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.EmailAddress:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid email address.";
                rule = $".EmailAddress().WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.CreditCard:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid credit card number.";
                rule = $".CreditCard().WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.Phone:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid phone number.";
                rule = $".Matches(@\"^\\+?\\d{{10,15}}$\").WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.Url:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid URL.";
                rule = $".Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.Date:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid date.";
                rule = $".Must(date => date != default).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.Number:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid number.";
                rule = $".Must(amount => decimal.TryParse(amount.ToString(), out _)).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.GuidNotEmpty:
                if (string.IsNullOrEmpty(message)) message = "Field must be a valid guid value";
                rule = $".NotEqual(Guid.Empty).WithMessage(\"{message}\")";
                break;
            case (int)Enums.ValidatorTypes.Length:
                var lengthValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)Enums.ValidatorTypeParams.Length_Value)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"This field must be {lengthValue ?? "?"} characters long.";
                rule = $".Length({lengthValue}).WithMessage(\"{message}\")";
                break;
            default:
                break;
        }
        if (string.IsNullOrWhiteSpace(rule))
            return string.Empty;
        return $"RuleFor(v => v.{fieldName}){rule};";
    }
}
