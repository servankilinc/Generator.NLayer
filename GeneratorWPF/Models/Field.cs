using GeneratorWPF.Extensions;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Models.Signature;
using Humanizer;
using System.Linq;

namespace GeneratorWPF.Models
{
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

        public string GetMapedTypeName()
        {
            return this.FieldTypeId switch
            {
                (int)FieldTypeEnums.Int => "int",
                (int)FieldTypeEnums.String => "string",
                (int)FieldTypeEnums.Long => "long",
                (int)FieldTypeEnums.Float => "float",
                (int)FieldTypeEnums.Double => "double",
                (int)FieldTypeEnums.Bool => "bool",
                (int)FieldTypeEnums.Char => "char",
                (int)FieldTypeEnums.Byte => "byte",
                (int)FieldTypeEnums.DateTime => "DateTime",
                (int)FieldTypeEnums.DateOnly => "DateOnly",
                (int)FieldTypeEnums.Guid => "Guid",
                (int)FieldTypeEnums.TimeSpan => "TimeSpan",
                (int)FieldTypeEnums.TimeOnly => "TimeOnly",
                _ => this.Name
            };
        }

        /// <summary>
        /// 1: Select
        /// 2: Number
        /// 3: Text
        /// 4: CheckBox
        /// 5: DateTime
        /// 6: Undefined
        /// </summary>
        /// <returns></returns>
        public int GetVariableGroup(Dictionary<string, string> selectableRelations) // key: fieldName, value: entityName
        {
            // joined(relational props) props = selectableRelations 
            if (selectableRelations.Any(f => f.Key.Trim().ToLower() == this.Name.Trim().ToLower())) // || this.FieldTypeId == (byte)FieldTypeEnums.Int || this.FieldTypeId == (byte)FieldTypeEnums.Guid
            {
                return 1;
            }
            else if (
                this.FieldTypeId == (byte)FieldTypeEnums.Int ||
                this.FieldTypeId == (byte)FieldTypeEnums.Double ||
                this.FieldTypeId == (byte)FieldTypeEnums.Float ||
                this.FieldTypeId == (byte)FieldTypeEnums.Byte ||
                this.FieldTypeId == (byte)FieldTypeEnums.Long)
            {
                return 2;
            }
            else if (this.FieldTypeId == (byte)FieldTypeEnums.String || this.FieldTypeId == (byte)FieldTypeEnums.Char)
            {
                return 3;
            }
            else if (this.FieldTypeId == (byte)FieldTypeEnums.Bool)
            {
                return 4;
            }
            else if (this.FieldTypeId == (byte)FieldTypeEnums.DateOnly || this.FieldTypeId == (byte)FieldTypeEnums.DateTime)
            {
                return 5;
            }
            else if (this.FieldTypeId == (byte)FieldTypeEnums.TimeSpan || this.FieldTypeId == (byte)FieldTypeEnums.TimeOnly)
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
        public int GetInputKind(int typeId)
        {
            if (typeId == 1)
            {
                return 1;
            }
            else if (typeId == 2 || typeId == 5 || typeId == 6)
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
        /// </summary>
        /// <returns></returns>
        public string CreateInputHTML(int typeId, string? parrentHtmlId = null)
        {
            if (typeId == 1)
            {
                return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""slct_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <select id=""slct_{this.Name.ToCamelCase()}"" class=""autoInitSelect2 form-select form-select-sm form-select-solid"" name=""{this.Name}"" asp-items=""Model.{this.Name.Pluralize()}"" data-control=""select2"" data-dropdown-parent=""#{parrentHtmlId}"" data-allow-clear=""true"">
                            <option></option>
                        </select>
                    </div>
                ";
            }
            else if (typeId == 2)
            {
                return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""inpt_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""inpt_{this.Name.ToCamelCase()}"" class=""form-control form-control-sm form-control-solid"" name=""{this.Name}"" type=""number""/>
                    </div>
                ";
            }
            else if (typeId == 3)
            {
                return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""inpt_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""inpt_{this.Name.ToCamelCase()}"" class=""form-control form-control-sm form-control-solid"" name=""{this.Name}"" type=""text""/>
                    </div>
                ";
            }
            else if (typeId == 4)
            {
                return $@"
                    <div class=""mb-10 form-check"">
                        <input id=""chckb_{this.Name.ToCamelCase()}"" class=""form-check-input"" name=""{this.Name}"" type=""checkbox"" value=""""/>
                        <label class=""form-check-label fw-semibold"" for=""chckb_{this.Name.ToCamelCase()}"">
                            {this.Name.DivideToLabelName()}
                        </label>
                    </div>
                ";
            }
            else if (typeId == 5)
            {
                return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""dtpick_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""dtpick_{this.Name.ToCamelCase()}"" class=""autoInitFlatPicker form-control form-control-sm form-control-solid"" name=""{this.Name}""/>
                    </div>
                ";
            }
            else if (typeId == 6)
            {
                return $@"
                    <div class=""mb-10"">
                        <label class=""form-label fw-semibold"" for=""timepick_{this.Name.ToCamelCase()}"">{this.Name.DivideToLabelName()}</label>
                        <input id=""timepick_{this.Name.ToCamelCase()}"" class=""autoInitFlatPickerOnlyTime form-control form-control-sm form-control-solid"" name=""{this.Name}""/>
                    </div>
                ";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
