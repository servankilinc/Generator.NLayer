using Generator.Domain.Core.Entities;
using Humanizer;
using Generator.Domain.Core;

namespace Generator.Domain.CodeGenerators.Helpers;

public static class HtmlInputGenerator
{
    public static string CreateInputHTML(Field field, int typeId, string? parrentHtmlId = null)
    {
        parrentHtmlId = parrentHtmlId != null ? $"data-dropdown-parent=\"#{parrentHtmlId}\"" : string.Empty;

        if (typeId == 1)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""slct_{field.Name.ToCamelCase()}"">{field.Name.DivideToLabelName()}</label>
                        <select id=""slct_{field.Name.ToCamelCase()}"" class=""autoInitSelect2 form-select form-select-sm form-select-solid"" name=""{field.Name}"" asp-items=""Model.{field.Name.Pluralize()}"" data-control=""select2"" {parrentHtmlId} data-allow-clear=""true"">
                            <option></option>
                        </select>
                    </div>";
        }
        else if (typeId == 2)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""inpt_{field.Name.ToCamelCase()}"">{field.Name.DivideToLabelName()}</label>
                        <input id=""inpt_{field.Name.ToCamelCase()}"" class=""form-control form-control-sm form-control-solid"" name=""{field.Name}"" type=""number""/>
                    </div>";
        }
        else if (typeId == 3)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""inpt_{field.Name.ToCamelCase()}"">{field.Name.DivideToLabelName()}</label>
                        <input id=""inpt_{field.Name.ToCamelCase()}"" class=""form-control form-control-sm form-control-solid"" name=""{field.Name}"" type=""text""/>
                    </div>";
        }
        else if (typeId == 4)
        {
            return $@"
                    <div class=""mb-10 form-check"">
                        <input id=""chckb_{field.Name.ToCamelCase()}"" class=""form-check-input"" name=""{field.Name}"" type=""checkbox"" value=""""/>
                        <label class=""form-check-label fw-semibold"" for=""chckb_{field.Name.ToCamelCase()}"">{field.Name.DivideToLabelName()}</label>
                    </div>";
        }
        else if (typeId == 5)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""dtpick_{field.Name.ToCamelCase()}"">{field.Name.DivideToLabelName()}</label>
                        <input id=""dtpick_{field.Name.ToCamelCase()}"" class=""autoInitFlatPicker form-control form-control-sm form-control-solid"" name=""{field.Name}""/>
                    </div>";
        }
        else if (typeId == 6)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""timepick_{field.Name.ToCamelCase()}"">{field.Name.DivideToLabelName()}</label>
                        <input id=""timepick_{field.Name.ToCamelCase()}"" class=""autoInitFlatPickerOnlyTime form-control form-control-sm form-control-solid"" name=""{field.Name}""/>
                    </div>";
        }
        else if (typeId == 7)
        {
            return $@"
                    <input name=""{field.Name}"" type=""hidden""/>";
        }
        else
        {
            return string.Empty;
        }
    }

    public static string CreateFormInputHTML(Field field, int typeId, string modelName, string? parrentHtmlId = null)
    {
        parrentHtmlId = parrentHtmlId != null ? $"data-dropdown-parent=\"#{parrentHtmlId}\"" : string.Empty;

        if (typeId == 1)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{field.Name}"">{field.Name.DivideToLabelName()}</label>
            <select class=""autoInitSelect2 form-select form-select-solid"" asp-items=""Model.{field.Name.Pluralize()}"" asp-for=""{modelName}.{field.Name}"" data-control=""select2"" {parrentHtmlId} data-allow-clear=""true"">
                <option></option>
            </select>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{field.Name}""></span>
        </div>";
        }
        else if (typeId == 2)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{field.Name}"">{field.Name.DivideToLabelName()}</label>
            <input class=""form-control form-control-solid"" asp-for=""{modelName}.{field.Name}"" type=""number""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{field.Name}""></span>
        </div>";
        }
        else if (typeId == 3)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{field.Name}"">{field.Name.DivideToLabelName()}</label>
            <input class=""form-control form-control-solid"" asp-for=""{modelName}.{field.Name}"" type=""text""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{field.Name}""></span>
        </div>";
        }
        else if (typeId == 4)
        {
            return $@"
        <div class=""col-md-6"">
            <div class=""form-check"">
                <input class=""form-check-input"" asp-for=""{modelName}.{field.Name}"" type=""checkbox"" value=""""/>
                <label class=""form-check-label fw-semibold"" asp-for=""{modelName}.{field.Name}"">{field.Name.DivideToLabelName()}</label>
                <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{field.Name}""></span>
            </div>
        </div>";
        }
        else if (typeId == 5)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{field.Name}"">{field.Name.DivideToLabelName()}</label>
            <input class=""autoInitFlatPicker form-control form-control-solid"" asp-for=""{modelName}.{field.Name}""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{field.Name}""></span>
        </div>";
        }
        else if (typeId == 6)
        {
            return $@"
      <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{field.Name}"">{field.Name.DivideToLabelName()}</label>
            <input class=""autoInitFlatPickerOnlyTime form-control form-control-solid"" asp-for=""{modelName}.{field.Name}""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{field.Name}""></span>
        </div>";
        }
        else if (typeId == 7)
        {
            return $@"
        <input asp-for=""{modelName}.{field.Name}"" type=""hidden""/>";
        }
        else
        {
            return string.Empty;
        }
    }
}
