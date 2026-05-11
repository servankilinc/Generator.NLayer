using Generator.Domain.Core.Entities.Base;
using Humanizer;

namespace Generator.Domain.Core.Entities;

public class Field : EntityBase
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public int FieldTypeId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsUnique { get; set; }
    public bool IsRequired { get; set; }
    public bool IsList { get; set; }
    public bool Filterable { get; set; }

    public Entity Entity { get; set; } = null!;
    public FieldType FieldType { get; set; } = null!;
    public virtual ICollection<Relation> RelationsPrimary { get; set; } = null!;
    public virtual ICollection<Relation> RelationsForeign { get; set; } = null!;
    public virtual ICollection<DtoField> DtoFields { get; set; } = null!;

    #region Helpers
    public string GetMapedTypeName()
    {
        return this.FieldTypeId switch
        {
            (int)Enums.FieldTypeEnums.Int => "int",
            (int)Enums.FieldTypeEnums.String => "string",
            (int)Enums.FieldTypeEnums.Long => "long",
            (int)Enums.FieldTypeEnums.Float => "float",
            (int)Enums.FieldTypeEnums.Double => "double",
            (int)Enums.FieldTypeEnums.Bool => "bool",
            (int)Enums.FieldTypeEnums.Char => "char",
            (int)Enums.FieldTypeEnums.Byte => "byte",
            (int)Enums.FieldTypeEnums.DateTime => "DateTime",
            (int)Enums.FieldTypeEnums.DateOnly => "DateOnly",
            (int)Enums.FieldTypeEnums.Guid => "Guid",
            (int)Enums.FieldTypeEnums.TimeSpan => "TimeSpan",
            (int)Enums.FieldTypeEnums.TimeOnly => "TimeOnly",
            _ => this.Name
        };
    }

    /// <summary>
    /// 1: Select
    /// 7: Hidden
    /// 2: Number
    /// 3: Text
    /// 4: CheckBox
    /// 5: DateTime
    /// 6: Undefined
    /// </summary>
    /// <returns></returns>
    public int GetVariableGroup(Dictionary<string, string> selectableRelations) // key: fieldName, value: entityName
    {
        if (selectableRelations.Any(f => f.Key.Trim().ToLower() == this.Name.Trim().ToLower()))
        {
            return 1;
        }
        // ilişki tanımları arasında seçilebilir bir ilişki yoksa ve alan benzersiz veya Guid tipindeyse, bu alanı gizli olarak işaretle
        else if (this.IsUnique || this.FieldTypeId == (byte)Enums.FieldTypeEnums.Guid)
        {
            return 7;
        }
        else if (
            this.FieldTypeId == (byte)Enums.FieldTypeEnums.Int ||
            this.FieldTypeId == (byte)Enums.FieldTypeEnums.Double ||
            this.FieldTypeId == (byte)Enums.FieldTypeEnums.Float ||
            this.FieldTypeId == (byte)Enums.FieldTypeEnums.Byte ||
            this.FieldTypeId == (byte)Enums.FieldTypeEnums.Long)
        {
            return 2;
        }
        else if (this.FieldTypeId == (byte)Enums.FieldTypeEnums.String || this.FieldTypeId == (byte)Enums.FieldTypeEnums.Char)
        {
            return 3;
        }
        else if (this.FieldTypeId == (byte)Enums.FieldTypeEnums.Bool)
        {
            return 4;
        }
        else if (this.FieldTypeId == (byte)Enums.FieldTypeEnums.DateOnly || this.FieldTypeId == (byte)Enums.FieldTypeEnums.DateTime)
        {
            return 5;
        }
        else if (this.FieldTypeId == (byte)Enums.FieldTypeEnums.TimeSpan || this.FieldTypeId == (byte)Enums.FieldTypeEnums.TimeOnly)
        {
            return 6;
        }

        return 0;
    }

    /// <summary>
    /// 1: Select
    /// 2: Equals
    /// 3: Contains
    /// 4: CheckBox
    /// </summary>
    /// <returns></returns>
    public int GetDatatableConditionKind(int typeId)
    {
        if (typeId == 1)
        {
            return 1;
        }
        else if (typeId == 2 || typeId == 5 || typeId == 6 || typeId == 7)
        {
            return 2;
        }
        else if (typeId == 3)
        {
            return 3;
        }
        else if (typeId == 4)
        {
            return 4;
        }

        return 0;
    }


    /// <summary>
    /// 1: Select
    /// 2: Number
    /// 3: Text
    /// 4: CheckBox
    /// 5: DateTime
    /// 6: Time
    /// 7: Hidden
    /// </summary>
    /// <returns></returns>
    public string CreateInputHTML(int typeId, string? parrentHtmlId = null)
    {
        parrentHtmlId = parrentHtmlId != null ? $"data-dropdown-parent=\"#{parrentHtmlId}\"" : string.Empty;

        if (typeId == 1)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""slct_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <select id=""slct_{this.Name.ToCamelCase()}"" class=""autoInitSelect2 form-select form-select-sm form-select-solid"" name=""{this.Name}"" asp-items=""Model.{this.Name.Pluralize()}"" data-control=""select2"" {parrentHtmlId} data-allow-clear=""true"">
                            <option></option>
                        </select>
                    </div>";
        }
        else if (typeId == 2)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""inpt_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""inpt_{this.Name.ToCamelCase()}"" class=""form-control form-control-sm form-control-solid"" name=""{this.Name}"" type=""number""/>
                    </div>";
        }
        else if (typeId == 3)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""inpt_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""inpt_{this.Name.ToCamelCase()}"" class=""form-control form-control-sm form-control-solid"" name=""{this.Name}"" type=""text""/>
                    </div>";
        }
        else if (typeId == 4)
        {
            return $@"
                    <div class=""mb-10 form-check"">
                        <input id=""chckb_{this.Name.ToCamelCase()}"" class=""form-check-input"" name=""{this.Name}"" type=""checkbox"" value=""""/>
                        <label class=""form-check-label fw-semibold"" for=""chckb_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                    </div>";
        }
        else if (typeId == 5)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""dtpick_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""dtpick_{this.Name.ToCamelCase()}"" class=""autoInitFlatPicker form-control form-control-sm form-control-solid"" name=""{this.Name}""/>
                    </div>";
        }
        else if (typeId == 6)
        {
            return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""timepick_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""timepick_{this.Name.ToCamelCase()}"" class=""autoInitFlatPickerOnlyTime form-control form-control-sm form-control-solid"" name=""{this.Name}""/>
                    </div>";
        }
        else if (typeId == 7)
        {
            return $@"
                    <input name=""{this.Name}"" type=""hidden""/>";
        }
        else
        {
            return string.Empty;
        }
    }


    /// <summary>
    /// 1: Select
    /// 2: Number
    /// 3: Text
    /// 4: CheckBox
    /// 5: DateTime
    /// 6: Time
    /// 7: Hidden
    /// </summary>
    /// <returns></returns>
    public string CreateFormInputHTML(int typeId, string modelName, string? parrentHtmlId = null)
    {
        parrentHtmlId = parrentHtmlId != null ? $"data-dropdown-parent=\"#{parrentHtmlId}\"" : string.Empty;


        if (typeId == 1)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{this.Name}"">{this.Name.DivideToLabelName()}</label>
            <select class=""autoInitSelect2 form-select form-select-solid"" asp-items=""Model.{this.Name.Pluralize()}"" asp-for=""{modelName}.{this.Name}"" data-control=""select2"" {parrentHtmlId} data-allow-clear=""true"">
                <option></option>
            </select>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{this.Name}""></span>
        </div>";
        }
        else if (typeId == 2)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{this.Name}"">{this.Name.DivideToLabelName()}</label>
            <input class=""form-control form-control-solid"" asp-for=""{modelName}.{this.Name}"" type=""number""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{this.Name}""></span>
        </div>";
        }
        else if (typeId == 3)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{this.Name}"">{this.Name.DivideToLabelName()}</label>
            <input class=""form-control form-control-solid"" asp-for=""{modelName}.{this.Name}"" type=""text""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{this.Name}""></span>
        </div>";
        }
        else if (typeId == 4)
        {
            return $@"
        <div class=""col-md-6"">
            <div class=""form-check"">
                <input class=""form-check-input"" asp-for=""{modelName}.{this.Name}"" type=""checkbox"" value=""""/>
                <label class=""form-check-label fw-semibold"" asp-for=""{modelName}.{this.Name}"">{this.Name.DivideToLabelName()}</label>
                <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{this.Name}""></span>
            </div>
        </div>";
        }
        else if (typeId == 5)
        {
            return $@"
        <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{this.Name}"">{this.Name.DivideToLabelName()}</label>
            <input class=""autoInitFlatPicker form-control form-control-solid"" asp-for=""{modelName}.{this.Name}""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{this.Name}""></span>
        </div>";
        }
        else if (typeId == 6)
        {
            return $@"
      <div class=""col-md-6"">
            <label class=""form-label fw-semibold"" asp-for=""{modelName}.{this.Name}"">{this.Name.DivideToLabelName()}</label>
            <input class=""autoInitFlatPickerOnlyTime form-control form-control-solid"" asp-for=""{modelName}.{this.Name}""/>
            <span class=""form_validation_feedback"" asp-validation-for=""{modelName}.{this.Name}""></span>
        </div>";
        }
        else if (typeId == 7)
        {
            return $@"
        <input asp-for=""{modelName}.{this.Name}"" type=""hidden""/>";
        }
        else
        {
            return string.Empty;
        }
    }
    #endregion
}
