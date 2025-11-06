using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;

namespace GeneratorWPF.CodeGenerators.NLayer.WebUI;

public class ViewGenerator
{
    private readonly RelationRepository _relationRepository;
    private readonly DtoRepository _dtoRepository;
    private readonly FieldRepository _fieldRepository;
    public ViewGenerator(AppSetting appSetting)
    {
        _relationRepository = new();
        _dtoRepository = new();
        _fieldRepository = new();
    }

    public string GenerateIndexPage(Entity entity)
    {
        List<Field> fieldList = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id, include: i => i.Include(x => x.FieldType));

        List<Field> filterableFields = fieldList.Where(f => f.Filterable).ToList();

        Dto? reportDto = entity.ReportDtoId != default ? _dtoRepository.Get(f => f.Id == entity.ReportDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField.FieldType)) : default;
        bool isThereReportDto = reportDto != default;

        List<ModelFieldInput> modelFilterFields = new List<ModelFieldInput>();
        if (filterableFields.Any()) modelFilterFields = CreateFilterInputsModel(entity, filterableFields);

        StringBuilder sb = new StringBuilder();

        sb.Append($@"
@using WebUI.Models.ViewModels.{entity.Name}_
@model {entity.Name}ViewModel
@{{
    ViewData[""Title""] = ""{entity.Name.Pluralize()}"";
}}");

        // ########## HTML ##########
        sb.AppendLine("\r<div class=\"card\">");

        // ******** HEADER ********
        sb.AppendLine("\t<div class=\"card-header\">");
        if (filterableFields.Any() || entity.SoftDeletable)
        {
            sb.Append(CreateFilterForm(entity, modelFilterFields));
        }
        sb.AppendLine("\t</div>");


        // ******** BODY ********
        sb.AppendLine("\t<div class=\"card-body\">");

        if (isThereReportDto)
        {
            sb.Append(CreateTableByDto(entity, reportDto!));
        }
        else
        {
            sb.Append(CreateTableByEntity(entity, fieldList));
        }
        sb.AppendLine("\t</div>");


        sb.AppendLine("</div>"); 

         
        // ########## SCRIPT ##########
        string datatableMethod = isThereReportDto ?
            CreateTableInitMethodByDto(entity, reportDto!, modelFilterFields) :
            CreateTableInitMethodByEntity(entity, fieldList, modelFilterFields);

        sb.Append($@"
@section Scripts {{
    <script>

        let PageTable;

        $(document).ready(function(){{
            InitilazeTable();
        }})

        function InitilazeTable(e) {{
             {datatableMethod}
        }}
    </script>
}}");



        return sb.ToString();
    }

    public string GenerateCreatePage(Entity entity)
    {
        List<Field> fieldList = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id, include: i => i.Include(x => x.FieldType));

        Dto? createDto = entity.CreateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.CreateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereCreateDto = createDto != default;
        
        List<ModelFieldInput> modelInputs = new List<ModelFieldInput>();
        if (isThereCreateDto)
        {
            modelInputs = CreateFormInputsModelByDto(entity, createDto!, "Create", "CreateModel");
        }
        else
        {
            modelInputs = CreateFormInputsModelByEntity(entity, fieldList, "Create", "CreateModel");
        }
            
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($@"
@using WebUI.Models.ViewModels.{entity.Name}_
@model {entity.Name}CreateViewModel
@{{
    ViewData[""Title""] = ""Create {entity.Name}"";
}}");

        // ########## HTML ##########
        sb.AppendLine("\r<div class=\"card\">");

        // ******** HEADER ********
        sb.AppendLine("\t<div class=\"card-header\">");
        sb.AppendLine($"\t\tCreate {entity.Name}");
        sb.AppendLine("\t</div>");


        // ******** BODY ********
        sb.AppendLine("\t<div class=\"card-body\">");

        sb.AppendLine(CreateForm(entity, fieldList, modelInputs!, "Create", true, createDto));
        
        sb.AppendLine("\t</div>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    public string GenerateCreateFormPage(Entity entity)
    {
        List<Field> fieldList = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id, include: i => i.Include(x => x.FieldType));

        Dto? createDto = entity.CreateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.CreateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereCreateDto = createDto != default;

        List<ModelFieldInput> modelInputs = new List<ModelFieldInput>();
        if (isThereCreateDto)
        {
            modelInputs = CreateFormInputsModelByDto(entity, createDto!, "Create", "CreateModel");
        }
        else
        {
            modelInputs = CreateFormInputsModelByEntity(entity, fieldList, "Create", "CreateModel");
        }

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($@"
@using WebUI.Models.ViewModels.{entity.Name}_
@model {entity.Name}CreateViewModel
@{{
    Layout = null;
}}"); 

        sb.AppendLine(CreateForm(entity, fieldList, modelInputs!, "Create", false, createDto));
         
        return sb.ToString();
    }

    public string GenerateUpdateFormPage(Entity entity)
    {
        List<Field> fieldList = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id, include: i => i.Include(x => x.FieldType));

        Dto? updateDto = entity.UpdateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.UpdateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereUpdateDto = updateDto != default;

        List<ModelFieldInput> modelInputs = new List<ModelFieldInput>();
        if (isThereUpdateDto)
        {
            modelInputs = CreateFormInputsModelByDto(entity, updateDto!, "Update", "UpdateModel");
        }
        else
        {
            modelInputs = CreateFormInputsModelByEntity(entity, fieldList, "Update", "UpdateModel");
        }

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($@"
@using WebUI.Models.ViewModels.{entity.Name}_
@model {entity.Name}UpdateViewModel
@{{
    Layout = null;
}}");

        sb.AppendLine(CreateForm(entity, fieldList, modelInputs!, "Update", false, updateDto));

        return sb.ToString();
    }


    public string GenerateSignUpPage(Entity userEntity)
    {
        StringBuilder sb = new StringBuilder();
        
        List<Field>? fieldList = _fieldRepository.GetAll(
            filter: f => f.EntityId == userEntity.Id && !f.IsUnique && f.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base, 
            include: i => i.Include(x => x.FieldType)
        );

        List<ModelFieldInput> modelInputs = new List<ModelFieldInput>();
        modelInputs = CreateFormInputsModelForSignUp(userEntity, fieldList);
    

        string formBody = CreateSignUpForm(userEntity, fieldList, modelInputs!, true);

        sb.Append($@"
﻿@using Model.Auth.SignUp
@model SignUpRequest
@{{
    Layout = ""_LayoutBase"";
    ViewData[""Title""] = ""SignUp"";
}}

<div class=""row justify-content-center"">
    <div class=""col-lg-4 mt-12"">
        <div class=""card"">
            <div class=""card-header border-bottom mb-6"">
                <div class=""app-brand justify-content-center"">
                    <a asp-controller=""Home"" asp-action=""Index"" class=""d-flex gap-2"">
                        <h4 class=""text-primary m-0"">
                            Create New Account
                        </h4>
                    </a>
                </div>
            </div>
            <div class=""card-body"">
                {formBody}
                <p class=""text-center"">
                    <span>Already have an account?</span>
                    <a asp-controller=""Account"" asp-action=""Login"">
                        <span>Login instead</span>
                    </a>
                </p>
            </div>
        </div>
    </div>
</div>");

        return sb.ToString();
    }


    #region Helpers

    #region Index Helpers
    // Html Helpers
    private string CreateFilterForm(Entity entity, List<ModelFieldInput> modelFilterFields)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var modelFilterField in modelFilterFields)
        {
            sb.Append(modelFilterField.InputCode);
        }

        if (entity.SoftDeletable)
        {
            sb.Append($@"                
                <div class=""col-md-4 col-sm-6 col-12"">
                    <div class=""input-group"">
                        <span class=""input-group-text bg-lightest"">
                            Include Deleted
                        </span>
                        <div class=""input-group-text"">
                            <input name=""IsDeleted"" type=""checkbox"" class=""form-check-input"" />
                        </div>
                    </div>
                </div>");
        }

        return $@"
        <form id=""form_page_filter"">
            <div class=""row row-gap-4"">
                {sb.ToString()}
                <div class=""col-md-3 col-sm-4 col-6"">
                    <button onclick=""InitilazeTable(this)"" type=""button"" class=""btn btn-light"">
                        <span class=""dynamic-content"">
                            <i class=""fa-solid fa-search me-2""></i>
                            Search
                        </span>
                    </button>
                </div>
            </div>
        </form>";
    }

    public string CreateTableByEntity(Entity entity, List<Field> fields)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var field in fields.Where(f => f.IsUnique == false && f.FieldType.SourceTypeId == (byte)FieldTypeSourceEnums.Base))
        {
            string labelText = field.Name.DivideToLabelName();
            sb.AppendLine($"\t\t\t\t\t<th>{labelText}</th>");
        }

        if (entity.Auditable)
        {
            sb.AppendLine("\t\t\t\t\t<th>Create Date</th>");
            sb.AppendLine("\t\t\t\t\t<th>Last Update Date</th>");
        }

        if (entity.SoftDeletable)
        {
            sb.AppendLine("\t\t\t\t\t<th>Status</th>");
            sb.AppendLine("\t\t\t\t\t<th>Delete Date</th>");
        }

        sb.AppendLine("\t\t\t\t\t<th>Actions</th>");

        return $@"
        <table id=""table_page"" class=""table table-hover table-striped"">
            <thead class=""bg-light"">
                <tr> 
{sb.ToString()}
                </tr>
            </thead>
        </table>
";
    }

    public string CreateTableByDto(Entity entity, Dto reportDto)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var dtoField in reportDto.DtoFields.Where(f => f.SourceField.FieldType.SourceTypeId == (byte)FieldTypeSourceEnums.Base))
        {
            string labelText = dtoField.Name.DivideToLabelName();
            sb.AppendLine($"\t\t\t\t\t<th>{labelText}</th>");
        }

        if (entity.Auditable)
        {
            sb.AppendLine("\t\t\t\t\t<th>Create Date</th>");
            sb.AppendLine("\t\t\t\t\t<th>Last Update Date</th>");
        }

        if (entity.SoftDeletable)
        {
            sb.AppendLine("\t\t\t\t\t<th>Status</th>");
            sb.AppendLine("\t\t\t\t\t<th>Delete Date</th>");
        }

        sb.AppendLine("\t\t\t\t\t<th>Actions</th>");


        return $@"
        <table id=""table_page"" class=""table table-hover table-striped"">
            <thead class=""bg-light"">
                <tr> 
{sb.ToString()}
                </tr>
            </thead>
        </table>
";
    }


    // Script Helpers
    private string CreateTableInitMethodByEntity(Entity entity, List<Field> fieldList, List<ModelFieldInput> modelFilterFields)
    {
        StringBuilder sb = new StringBuilder();

        return $@"
            PageTable = DatatableManager.Create({{serverSide: true,
                tableId: 'table_page',
                path: '{entity.Name}/DatatableServerSide',
                method: 'Post',
                buttonElement: e,
                {DynamicFilterObject(entity, modelFilterFields)}
                {ColumnsObjectByEntity(entity, fieldList)}
                {CustomTableButtons(entity)}
            }})";
    }

    private string CreateTableInitMethodByDto(Entity entity, Dto reportDto, List<ModelFieldInput> modelFilterFields)
    {
        return $@"
            PageTable = DatatableManager.Create({{serverSide: true,
                tableId: 'table_page',
                path: '{entity.Name}/DatatableServerSide',
                method: 'Post',
                buttonElement: e,
                {DynamicFilterObject(entity, modelFilterFields)}
                {ColumnsObjectByDto(entity, reportDto)}
                {CustomTableButtons(entity)}
            }})";
    }


    private string DynamicFilterObject(Entity entity, List<ModelFieldInput> modelFilterFields)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var modelFilterField in modelFilterFields)
        {
            // input select
            if (modelFilterField.InputGroup == 1)
            {
                sb.AppendLine($@"
                            {{
                                operator: 'eq',
                                field: ""{modelFilterField.InputName.ToCamelCase()}"",
                                value: $(""select[name='{modelFilterField.InputName}']"").val(),
                            }},");
            }
            // input select
            else if (modelFilterField.InputGroup == 3)
            {
                sb.AppendLine($@"
                            {{
                                operator: 'contains',
                                field: ""{modelFilterField.InputName.ToCamelCase()}"",
                                value: $(""input[name='{modelFilterField.InputName}']"").val(),
                            }},");
            }
            else
            {
                sb.AppendLine($@"
                            {{
                                operator: 'eq',
                                field: ""{modelFilterField.InputName.ToCamelCase()}"",
                                value: $(""input[name='{modelFilterField.InputName}']"").val(),
                            }},");
            }
        }

        if (entity.SoftDeletable)
        {
            sb.AppendLine(@"
                            {
                                operator: 'eq',
                                field: ""IsDeleted"",
                                value: false,
                                logic: 'or',
                                filters: [
                                    {
                                        operator: 'eq',
                                        field: ""IsDeleted"",
                                        value: $(""input[name='IsDeleted']"").val(),
                                    }
                                ]
                            }");
        }

        return $@"
                requestData: {{
                    filter: {{
                        operator: 'base',
                        logic: 'and',
                        filters: [
                             {sb.ToString()}
                        ]
                    }}
                }},";
    }

    private string ColumnsObjectByEntity(Entity entity, List<Field> fields)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var field in fields.Where(f => f.IsUnique == false && f.FieldType.SourceTypeId == (byte)FieldTypeSourceEnums.Base))
        {
            int variableGroup = field.GetVariableGroup();

            // date type
            if (variableGroup == 5)
            {
                sb.AppendLine($@"
                    {{
                        data: '{field.Name.ToCamelCase()}',
                        render: function (data) {{
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }}
                    }},");
            }
            // bool
            else if (variableGroup == 4)
            {
                sb.AppendLine(@$"
                    {{
                        data: '{field.Name.ToCamelCase()}',
                        render: function (data) {{
                            if(data == true) return (`<span class=""badge rounded-pill bg-label-danger""><i class=""fa-solid fa-xmark""></i></span>`);
                            else if(data == false) return (`<span class=""badge rounded-pill bg-label-success""><i class=""fa-solid fa-check""></i></span>`);
                            else return ('');
                        }}
                    }},");
            }
            else
            {
                sb.AppendLine($"\t\t\t\t\t{{data: '{field.Name.ToCamelCase()}' }},");
            }
        }

        if (entity.Auditable)
        {
            sb.AppendLine(@"
                    {
                        data: 'createDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },
                    {
                        data: 'updateDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },");
        }

        if (entity.SoftDeletable)
        {
            sb.AppendLine(@"
                    {
                        data: 'isDeleted',
                        render: function (data) {
                            if(data == true) return (`<span class=""badge rounded-pill bg-label-danger""><i class=""fa-solid fa-xmark""></i></span>`);
                            else if(data == false) return (`<span class=""badge rounded-pill bg-label-success""><i class=""fa-solid fa-check""></i></span>`);
                            else return ('');
                        }
                    },
                    {
                        data: 'deletedDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },");
        }

        sb.AppendLine($@"                    
                    {{
                        data: null,
                        defaultContent: '',
                        searchable: false,
                        createdCell: function (td, cellData, rowData, row, col)
                        {{ 
                            {RowButtons(entity)}
                        }}
                    }}");

        return $@"
                columns: [
                    {sb.ToString()}
                ],";
    }

    private string ColumnsObjectByDto(Entity entity, Dto reportDto)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var dtoField in reportDto.DtoFields.Where(f => f.SourceField.FieldType.SourceTypeId == (byte)FieldTypeSourceEnums.Base))
        {
            int variableGroup = dtoField.SourceField.GetVariableGroup();

            // date type
            if (variableGroup == 5)
            {
                sb.AppendLine($@"
                    {{
                        data: '{dtoField.Name.ToCamelCase()}',
                        render: function (data) {{
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }}
                    }},");
            }
            // bool
            else if (variableGroup == 4)
            {
                sb.AppendLine($@"
                    {{
                        data: '{dtoField.Name.ToCamelCase()}',
                        render: function (data) {{
                            if(data == true) return (`<span class=""badge rounded-pill bg-label-danger""><i class=""fa-solid fa-xmark""></i></span>`);
                            else if(data == false) return (`<span class=""badge rounded-pill bg-label-success""><i class=""fa-solid fa-check""></i></span>`);
                            else return ('');
                        }}
                    }},");
            }
            else
            {
                sb.AppendLine($@"{{data: '{dtoField.SourceField.Name.ToCamelCase()}' }},");
            }
        }

        if (entity.Auditable)
        {
            sb.AppendLine(@"
                    {
                        data: 'createDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },");

            sb.AppendLine(@"
                    {
                        data: 'updateDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },");
        }

        if (entity.SoftDeletable)
        {
            sb.AppendLine(@"
                    {
                        data: 'isDeleted',
                        render: function (data) {
                            if(data == true) return (`<span class=""badge rounded-pill bg-label-danger""><i class=""fa-solid fa-xmark""></i></span>`);
                            else if(data == false) return (`<span class=""badge rounded-pill bg-label-success""><i class=""fa-solid fa-check""></i></span>`);
                            else return ('');
                        }
                    },");

            sb.AppendLine(@"
                    {
                        data: 'deletedDateUtc',
                        render: function (data) {
                            if(data == null) return '';
                            return moment(data).format('DD.MM.YYYY HH:mm');
                        }
                    },");
        }

        sb.AppendLine($@"                    
                    {{
                        data: null,
                        defaultContent: '',
                        searchable: false,
                        createdCell: function (td, cellData, rowData, row, col)
                        {{ 
                            {RowButtons(entity)}
                        }}
                    }}");

        return $@"
                columns: [
                    {sb.ToString()}
                ],";
    }



    private string RowButtons(Entity entity)
    {
        // id: rowData.id
        string uniqueFieldParams = string.Join(", ", entity.Fields.Where(f => f.IsUnique).Select(d => $"\"{d.Name.ToCamelCase()}\": rowData.{d.Name.ToCamelCase()}"));

        string entityLabelName = entity.Name.DivideToLabelName();
      
        if (entity.SoftDeletable == false)
        {
            return $@"
                            DatatableManager.AppendRowButtons(td,
                                [
                                    // 1) Update Button
                                    UIManager.UpdateButtonTable({{
						                title: 'Update {entityLabelName} Informations',
						                formGetterUrl: '{entity.Name}/UpdateForm',
						                requestData: {{
							                {uniqueFieldParams}
						                }},
						                pageTable: PageTable
					                }}),
                                    // 2) Delete Button
					                UIManager.DeleteButtonTable({{
						                requestUrl: '{entity.Name}/Delete',
                                        disable: rowData.isDeleted == true,
						                requestData: {{
                                            {uniqueFieldParams}
						                }},
						                pageTable:PageTable
					                }})
                                ]
                            );";
        }
        return $@"  
                            let deleteHandleButton = rowData.isDeleted == true ?
				                UIManager.UndoDeleteButtonTable({{
					                requestUrl: '{entity.Name}/UndoDelete',
					                requestData: {{
                                        {uniqueFieldParams}
					                }},
					                pageTable:PageTable
				                }}) :
				                UIManager.DeleteButtonTable({{
					                requestUrl: '{entity.Name}/Delete',
					                requestData: {{
						                // deleteModel destegi yok
                                        {uniqueFieldParams}
					                }},
					                pageTable:PageTable
				                }});

                            DatatableManager.AppendRowButtons(td,
                                [
                                    // 1) Update Button
                                    UIManager.UpdateButtonTable({{
						                title: 'Update {entityLabelName} Informations',
						                formGetterUrl: '{entity.Name}/UpdateForm',
						                requestData: {{
							                {uniqueFieldParams}
						                }},
						                pageTable: PageTable
					                }}),
                                    // 2) Delete Button
					                deleteHandleButton
                                ]
                            );";
    }

    private string CustomTableButtons(Entity entity)
    {
        string entityLabelName = entity.Name.DivideToLabelName();

        return $@"
                customButtons:
                [
                    {{
                        text: '<span class=""dynamic-content""><i class=""fa-solid fa-file-circle-plus me-2""></i>Add New {entityLabelName}</span>',
                        className: 'create-new btn btn-primary mx-2',
                        action: (e_btn) =>
                        {{
                            UIManager.InsertModal({{
								title: ""Add New {entityLabelName}"",
								formGetterUrl: '{entity.Name}/CreateForm',
								e_btn:e_btn.currentTarget,
								pageTable: PageTable
							}})
                        }}
                    }}
                ]";
    } 

    // Helper UI Model
    private List<ModelFieldInput> CreateFilterInputsModel(Entity entity, List<Field> fields)
    {
        StringBuilder sb = new StringBuilder();

        Dictionary<string, string> selectableRelations = new Dictionary<string, string>();
        foreach (var field in fields)
        {
            Relation? relation = _relationRepository.Get(
              filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)RelationTypeEnums.OneToMany,
              include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity));

            if (relation == null) continue;

            string selectPropName = field.Name.ToForeignFieldSlectListName(relation.PrimaryField.Entity.Name);
            selectableRelations.Add(field.Name, relation.PrimaryField.Entity.Name);
        }

        List<ModelFieldInput> result = new List<ModelFieldInput>();
        foreach (var field in fields)
        {
            ModelFieldInput modelFilterFieldInput = new ModelFieldInput();

            int variableGroupType = field.GetVariableGroup(selectableRelations);

            string labelText = field.Name.DivideToLabelName();
            string inputName = field.Name;

            modelFilterFieldInput.InputGroup = variableGroupType;
            modelFilterFieldInput.InputName = inputName;
            modelFilterFieldInput.InputName = inputName;

            // ### SelectList ###
            if (variableGroupType == 1)
            {
                var relation = selectableRelations.First(f => f.Key.Trim().ToLower() == field.Name.Trim().ToLower());
                string entityName = relation.Value;
                string listPropName = relation.Key.ToForeignFieldSlectListName(entityName);

                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-4 col-sm-6 col-12"">
                    <div class=""input-group d-flex flex-nowrap"">
                        <span class=""input-group-text bg-lightest"">
                            {labelText}
                        </span>
                        <select name=""{inputName}"" asp-items=""Model.{listPropName}"" class=""autoInitSelect2 form-select"">
                            <option></option>
                        </select>
                    </div>
                </div>";
            }
            // ### Number ###
            else if (variableGroupType == 2)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-4 col-sm-6 col-12"">
                    <div class=""input-group"">
                        <span class=""input-group-text bg-lightest"">
                            {labelText}
                        </span>
                        <input name=""{inputName}"" type=""number"" class=""form-control"" />
                    </div>
                </div>";
            }
            // ### Text ###
            else if (variableGroupType == 3)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-4 col-sm-6 col-12"">
                    <div class=""input-group"">
                        <span class=""input-group-text bg-lightest"">
                            {labelText}
                        </span>
                        <input name=""{inputName}"" type=""text"" class=""form-control"" />
                    </div>
                </div>";
            }
            // ### CheckBox ###
            else if (variableGroupType == 4)
            {
                modelFilterFieldInput.InputCode = $@"                
                <div class=""col-md-4 col-sm-6 col-12"">
                    <div class=""input-group"">
                        <span class=""input-group-text bg-lightest"">
                            {labelText}
                        </span>
                        <div class=""input-group-text"">
                            <input name=""{inputName}"" type=""checkbox"" class=""form-check-input"" />
                        </div>
                    </div>
                </div>";
            }
            // ### Date ###
            else if (variableGroupType == 5)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-4 col-sm-6 col-12"">
                    <div class=""input-group"">
                        <span class=""input-group-text bg-lightest"">
                            {labelText}
                        </span>
                        <input name=""{inputName}"" type=""text"" class=""autoInitDatePicker form-control"" />
                    </div>
                </div>";
            }
            // ### Time ###
            else if (variableGroupType == 6)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-4 col-sm-6 col-12"">
                    <div class=""input-group"">
                        <span class=""input-group-text bg-lightest"">
                            {labelText}
                        </span>
                        <input name=""{inputName}"" type=""time"" class=""form-control"" />
                    </div>
                </div>";
            }

            result.Add(modelFilterFieldInput);
        }

        return result;
    }
    #endregion



    private List<ModelFieldInput> CreateFormInputsModelByEntity(Entity entity, List<Field> fields, string action, string modelName)
    {
        if (!string.IsNullOrWhiteSpace(modelName) && !modelName.EndsWith('.')) modelName = $"{modelName}.";
        StringBuilder sb = new StringBuilder();

        Dictionary<string, string> selectableRelations = new Dictionary<string, string>();
        foreach (var field in fields)
        {
            Relation? relation = _relationRepository.Get(
              filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)RelationTypeEnums.OneToMany,
              include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity));

            if (relation == null) continue;

            string selectPropName = field.Name.ToForeignFieldSlectListName(relation.PrimaryField.Entity.Name);
            selectableRelations.Add(field.Name, relation.PrimaryField.Entity.Name);
        }

        List<ModelFieldInput> result = new List<ModelFieldInput>();
        foreach (var field in fields)
        {
            ModelFieldInput modelFilterFieldInput = new ModelFieldInput();

            int variableGroupType = field.GetVariableGroup(selectableRelations);

            string labelText = field.Name.DivideToLabelName();
            string inputName = field.Name;

            modelFilterFieldInput.InputGroup = variableGroupType;
            modelFilterFieldInput.InputName = inputName;

            // ### SelectList ###
            if (variableGroupType == 1)
            {
                var relation = selectableRelations.First(f => f.Key.Trim().ToLower() == field.Name.Trim().ToLower());
                string entityName = relation.Value;
                string listPropName = relation.Key.ToForeignFieldSlectListName(entityName);

                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <select asp-for=""{modelName}{inputName}"" asp-items=""Model.{listPropName}"" class=""autoInitSelect2 form-select"">
                        <option></option>
                    </select>
                    <span asp-validation-for=""{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### Hidden ###
            else if (field.IsUnique && action == "Update")
            {
                modelFilterFieldInput.InputCode = $@"
                <input asp-for=""{modelName}{inputName}"" type=""hidden"" />";
            }
            // ### Hidden ###
            else if (field.IsUnique && action == "Create")
            {
                modelFilterFieldInput.InputCode = $@"";
            }
            // ### Number ###
            else if (variableGroupType == 2)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""number"" class=""form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### Text ###
            else if (variableGroupType == 3)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""text"" class=""form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### CheckBox ###
            else if (variableGroupType == 4)
            {
                modelFilterFieldInput.InputCode = $@"                
                <div class=""col-md-6"">
                    <div class=""form-check"">
                        <label asp-for=""{modelName}{inputName}"" class=""form-check-label"">
                            {labelText}
                        </label>
                        <input asp-for=""{modelName}{inputName}"" type=""checkbox"" class=""form-check-input"" />
                        <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                    </div>
                </div>";
            }
            // ### Date ###
            else if (variableGroupType == 5)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""text"" class=""autoInitDatePicker form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### Time ###
            else if (variableGroupType == 6)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""time"" class=""form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }

            result.Add(modelFilterFieldInput);
        }

        return result;
    }
    private List<ModelFieldInput> CreateFormInputsModelByDto(Entity entity, Dto dto, string action, string modelName)
    {
        if (!string.IsNullOrWhiteSpace(modelName) && !modelName.EndsWith('.')) modelName = $"{modelName}.";

        StringBuilder sb = new StringBuilder();

        Dictionary<string, string> selectableRelations = new Dictionary<string, string>();
        foreach (var dtoField in dto.DtoFields)
        {
            Relation? relation = _relationRepository.Get(
              filter: f => f.ForeignFieldId == dtoField.SourceField.Id && f.RelationTypeId == (byte)RelationTypeEnums.OneToMany,
              include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity));

            if (relation == null) continue;

            string selectPropName = dtoField.Name.ToForeignFieldSlectListName(relation.PrimaryField.Entity.Name);
            selectableRelations.Add(dtoField.Name, relation.PrimaryField.Entity.Name);
        }

        List<ModelFieldInput> result = new List<ModelFieldInput>();
        foreach (var dtoField in dto.DtoFields)
        {
            ModelFieldInput modelFilterFieldInput = new ModelFieldInput();

            int variableGroupType = selectableRelations.Any(f => f.Key == dtoField.Name) ? 1 : dtoField.SourceField.GetVariableGroup(selectableRelations);

            string labelText = dtoField.SourceField.Name.DivideToLabelName();
            string inputName = dtoField.SourceField.Name;

            modelFilterFieldInput.InputGroup = variableGroupType;
            modelFilterFieldInput.InputName = inputName;
            modelFilterFieldInput.InputName = inputName;

            // ### SelectList ###
            if (variableGroupType == 1)
            {
                var relation = selectableRelations.First(f => f.Key.Trim().ToLower() == dtoField.SourceField.Name.Trim().ToLower());
                string entityName = relation.Value;
                string listPropName = relation.Key.ToForeignFieldSlectListName(entityName);

                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <select asp-for=""{modelName}{inputName}"" asp-items=""Model.{listPropName}"" class=""autoInitSelect2 form-select"">
                        <option></option>
                    </select>
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            else if (dtoField.SourceField.IsUnique && action == "Update")
            {
                modelFilterFieldInput.InputCode = $@"
                <input asp-for=""{modelName}{inputName}"" type=""hidden"" />";
            }
            // ### Number ###
            else if (variableGroupType == 2)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""number"" class=""form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### Text ###
            else if (variableGroupType == 3)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""text"" class=""form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### CheckBox ###
            else if (variableGroupType == 4)
            {
                modelFilterFieldInput.InputCode = $@"                
                <div class=""col-md-6"">
                    <div class=""form-check"">
                        <label asp-for=""{modelName}{inputName}"" class=""form-check-label"">
                            {labelText}
                        </label>
                        <input asp-for=""{modelName}{inputName}"" type=""checkbox"" class=""form-check-input"" />
                        <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                    </div>
                </div>";
            }
            // ### Date ###
            else if (variableGroupType == 5)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""text"" class=""autoInitDatePicker form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### Time ###
            else if (variableGroupType == 6)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""col-md-6"">
                    <label asp-for=""{modelName}{inputName}"" class=""form-label"">
                        {labelText}
                    </label>
                    <input asp-for=""{modelName}{inputName}"" type=""time"" class=""form-control"" />
                    <span asp-validation-for=""{modelName}{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            result.Add(modelFilterFieldInput);
        }

        return result;
    }
    private List<ModelFieldInput> CreateFormInputsModelForSignUp(Entity entity, List<Field> fields)
    {
        StringBuilder sb = new StringBuilder();

        List<ModelFieldInput> result = new List<ModelFieldInput>();
        foreach (var field in fields)
        {
            Relation? relation = _relationRepository.Get(filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)RelationTypeEnums.OneToMany);
            if (relation != null) continue; // ilişkisi olan alan modelde olmasın
             
            ModelFieldInput modelFilterFieldInput = new ModelFieldInput();
             
            int variableGroupType = field.GetVariableGroup();

            string labelText = field.Name.DivideToLabelName();
            string inputName = field.Name;

            modelFilterFieldInput.InputGroup = variableGroupType;
            modelFilterFieldInput.InputName = inputName;

            // ### SelectList ###
            //if (variableGroupType == 1)
            // ### Number ###
            if (variableGroupType == 1 || variableGroupType == 2)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""mb-6"">
                    <label asp-for=""{inputName}"" class=""form-label"">{labelText}</label>
                    <input asp-for=""{inputName}"" type=""number"" class=""form-control"" autofocus />
                    <span asp-validation-for=""{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### Text ###
            else if (variableGroupType == 3)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""mb-6"">
                    <label asp-for=""{inputName}"" class=""form-label"">{labelText}</label>
                    <input asp-for=""{inputName}"" type=""text"" class=""form-control"" autofocus />
                    <span asp-validation-for=""{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }
            // ### CheckBox ###
            else if (variableGroupType == 4)
            {
                modelFilterFieldInput.InputCode = $@"                
                <div class=""mb-6"">
                    <div class=""form-check"">
                        <label asp-for=""{inputName}"" class=""form-check-label"">{labelText}</label>
                        <input asp-for=""{inputName}"" type=""checkbox"" class=""form-check-input"" />
                        <span asp-validation-for=""{inputName}"" class=""form_validation_feedback""></span>
                    </div>
                </div>";
            }
            // ### Date ###
            else if (variableGroupType == 5)
            {
                modelFilterFieldInput.InputCode = $@"
                <div class=""mb-6"">
                    <label asp-for=""{inputName}"" class=""form-label"">{labelText}</label>
                    <input asp-for=""{inputName}"" type=""text"" class=""autoInitDatePicker form-control"" autofocus />
                    <span asp-validation-for=""{inputName}"" class=""form_validation_feedback""></span>
                </div>";
            }

            result.Add(modelFilterFieldInput);
        }

        return result;
    }

    private string CreateForm(Entity entity, List<Field> fields, List<ModelFieldInput> modelFieldInputs, string action, bool submitBtn, Dto? dto = default)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var modelFilterField in modelFieldInputs)
        {
            sb.Append(modelFilterField.InputCode);
        }

        string button = submitBtn ? @"
                <div class=""col-12"">
					<button type=""submit"" class=""btn btn-success"">
						<i class=""fa-solid fa-floppy-disk me-2""></i>
						Save
					</button>
				</div>" : string.Empty;

        return $@"
		<form asp-controller=""{entity.Name}"" asp-action=""{action}"" method=""post"">
            <div class=""row row-gap-4 m-2"">
                {sb.ToString()}
                {button}
            </div>
        </form>";
    }


    private string CreateSignUpForm(Entity entity, List<Field> fields, List<ModelFieldInput> modelFieldInputs, bool submitBtn)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var modelFilterField in modelFieldInputs)
        {
            sb.Append(modelFilterField.InputCode);
        }


        if (!fields.Any(f => f.Name.ToLowerInvariant().Trim() == "email"))
        {
            sb.AppendLine(@"
                    <div class=""mb-6"">
                        <label asp-for=""Email"" class=""form-label"">Email</label>
                        <input asp-for=""Email"" class=""form-control"" autofocus />
                        <span asp-validation-for=""Email""></span>
                    </div>");
        }

        if(!fields.Any(f => f.Name.ToLowerInvariant().Trim() == "password"))
        {
            sb.AppendLine(@"
                    <div>
                        <label asp-for=""Password"" class=""form-label"">Password</label>
                        <div class=""input-group"">
                            <input asp-for=""Password"" type=""password"" class=""form-control"" placeholder=""&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;&#xb7;"" /> 
                            <span asp-validation-for=""Password""></span>
                        </div>
                    </div>");
        }

        return $@"
	        	<form asp-controller=""Account"" asp-action=""SignUp"" method=""post"" class=""mb-6"">
                    {sb.ToString()}
                    <div class=""my-7"">
                        <div class=""form-check mb-0"">
                            <input class=""form-check-input"" type=""checkbox"" id=""terms-conditions"" name=""terms"" />
                            <label class=""form-check-label"" for=""terms-conditions"">
                                I agree to
                                <a href=""javascript:void(0);"">privacy policy & terms</a>
                            </label>
                        </div>
                    </div>
                    <button type=""submit"" class=""btn btn-primary d-grid w-100"">Sign up</button>
                </form>";
    }




    private class ModelFieldInput
    {
        public int InputGroup { get; set; }
        public string InputName { get; set; } = null!;
        public string LabelText { get; set; } = null!;
        public string InputCode { get; set; } = null!;
    }
    #endregion
}
