using System.ComponentModel;
using Generator.Domain.Context;
using Generator.Domain.Core.Dtos.Validation;
using Generator.Domain.Repository;
using Microsoft.SemanticKernel;

namespace Generator.API.Services.AI.Plugins;

public class ValidationPlugin
{
    private readonly ValidationRepository _validationRepo;
    private readonly ProjectContext _context;

    public ValidationPlugin(ValidationRepository validationRepo, ProjectContext context)
    {
        _validationRepo = validationRepo;
        _context = context;
    }

    [KernelFunction("AddValidation")]
    [Description("Adds a FluentValidation rule for a specific field within a DTO.")]
    public string AddValidation(
        [Description("Name of the target DTO (e.g., CreateProductDto)")] string dtoName,
        [Description("Name of the column/field to apply the rule to (e.g., Price)")] string fieldName,
        [Description("Validator Type ID (e.g., 1=NotEmpty, 2=NotNull, 4=MaxLength, 8=GreaterThan; call GetSystemDictionaryTypes for the full list)")] int validatorTypeId,
        [Description("Error message to display (e.g., Price cannot be empty!)")] string errorMessage)
    {
        try
        {
            // İlgili Dto'yu bul
            var dto = _context.Dtos.FirstOrDefault(d => d.Name.ToLower() == dtoName.ToLower());
            if (dto == null) return $"Error: DTO '{dtoName}' not found.";

            // İlgili DtoField'ı bul
            var dtoField = _context.DtoFields.FirstOrDefault(f => f.DtoId == dto.Id && f.Name.ToLower() == fieldName.ToLower());
            if (dtoField == null) return $"Error: Field '{fieldName}' not found in DTO '{dtoName}'.";

            var validations = _validationRepo.GetUpdateDtos(dtoField.Id);
            validations.Add(new ValidationUpdateDto
            {
                ValidationId = 0, // 0 means new
                DtoFieldId = dtoField.Id,
                ValidatorTypeId = validatorTypeId,
                ErrorMessage = errorMessage
            });

            _validationRepo.Update(validations);
            return $"Validation rule successfully added to '{dtoName}' -> '{fieldName}'.";
        }
        catch (Exception ex)
        {
            return $"Error adding Validation: {ex.Message}";
        }
    }

    [KernelFunction("RemoveValidation")]
    [Description("Removes a specific Validation rule from a field within a DTO.")]
    public string RemoveValidation(
        [Description("Name of the target DTO (e.g., CreateProductDto)")] string dtoName,
        [Description("Name of the column/field (e.g., Price)")] string fieldName,
        [Description("Validator Type ID to remove (e.g., 1=NotEmpty; call GetSystemDictionaryTypes for the full list)")] int validatorTypeId)
    {
        try
        {
            var dto = _context.Dtos.FirstOrDefault(d => d.Name.ToLower() == dtoName.ToLower());
            if (dto == null) return $"Error: DTO '{dtoName}' not found.";

            var dtoField = _context.DtoFields.FirstOrDefault(f => f.DtoId == dto.Id && f.Name.ToLower() == fieldName.ToLower());
            if (dtoField == null) return $"Error: Field '{fieldName}' not found in DTO '{dtoName}'.";

            var validation = _context.Validations.FirstOrDefault(v => 
                v.DtoFieldId == dtoField.Id && v.ValidatorTypeId == validatorTypeId);

            if (validation == null) return $"Error: No matching validation rule found to delete.";

            _context.Validations.Remove(validation);
            _context.SaveChanges();
            
            return $"Validation rule successfully removed from '{dtoName}' -> '{fieldName}'.";
        }
        catch (Exception ex)
        {
            return $"Error removing Validation: {ex.Message}";
        }
    }

    [InspectorFunction]
    [KernelFunction("GetValidations")]
    [Description("Retrieves the full list of existing Validation rules. Use this to see exactly which rules (NotNull, etc.) exist on which DTO fields.")]
    public string GetValidations()
    {
        var validations = _context.Validations.ToList();
        if (!validations.Any()) return "There are no validation rules in the database yet.";

        var validatorTypes = _context.ValidatorTypes.ToList();
        var dtoFields = _context.DtoFields.ToList();
        var dtos = _context.Dtos.ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Existing Validation Rules:");
        foreach (var val in validations)
        {
            var vType = validatorTypes.FirstOrDefault(v => v.Id == val.ValidatorTypeId);
            var df = dtoFields.FirstOrDefault(d => d.Id == val.DtoFieldId);
            var dto = df != null ? dtos.FirstOrDefault(d => d.Id == df.DtoId) : null;

            var dtoName = dto?.Name ?? "UnknownDto";
            var fieldName = df?.Name ?? "UnknownField";
            var ruleName = vType?.Name ?? "UnknownRule";

            sb.AppendLine($"- DTO: {dtoName} | Field: {fieldName} | Rule: {ruleName} | ErrorMsg: {val.ErrorMessage}");
        }

        return sb.ToString();
    }
}
