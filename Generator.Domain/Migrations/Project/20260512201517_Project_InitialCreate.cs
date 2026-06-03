using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Generator.Domain.Migrations.Project
{
    /// <inheritdoc />
    public partial class Project_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrudTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrudTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeleteBehaviorTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeleteBehaviorTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldTypeSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldTypeSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidatorTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidatorTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SourceTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldTypes_FieldTypeSources_SourceTypeId",
                        column: x => x.SourceTypeId,
                        principalTable: "FieldTypeSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidatorTypeParams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ValidatorTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidatorTypeParams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidatorTypeParams_ValidatorTypes_ValidatorTypeId",
                        column: x => x.ValidatorTypeId,
                        principalTable: "ValidatorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: true),
                    SolutionName = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    DBConnectionString = table.Column<string>(type: "TEXT", nullable: true),
                    IsThereIdentity = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsThereUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserEntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsThereRole = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleEntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DtoFieldRelations",
                columns: table => new
                {
                    DtoFieldId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationId = table.Column<int>(type: "INTEGER", nullable: false),
                    SequenceNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtoFieldRelations", x => new { x.DtoFieldId, x.RelationId, x.SequenceNo });
                });

            migrationBuilder.CreateTable(
                name: "DtoFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DtoId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceFieldId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsList = table.Column<bool>(type: "INTEGER", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtoFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Validations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DtoFieldId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidatorTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Validations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Validations_DtoFields_DtoFieldId",
                        column: x => x.DtoFieldId,
                        principalTable: "DtoFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Validations_ValidatorTypes_ValidatorTypeId",
                        column: x => x.ValidatorTypeId,
                        principalTable: "ValidatorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationParams",
                columns: table => new
                {
                    ValidationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidatorTypeParamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationParams", x => new { x.ValidationId, x.ValidatorTypeParamId });
                    table.ForeignKey(
                        name: "FK_ValidationParams_Validations_ValidationId",
                        column: x => x.ValidationId,
                        principalTable: "Validations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValidationParams_ValidatorTypeParams_ValidatorTypeParamId",
                        column: x => x.ValidatorTypeParamId,
                        principalTable: "ValidatorTypeParams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatedEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    CrudTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dtos_CrudTypes_CrudTypeId",
                        column: x => x.CrudTypeId,
                        principalTable: "CrudTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableName = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreateDtoId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateDtoId = table.Column<int>(type: "INTEGER", nullable: true),
                    DeleteDtoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReportDtoId = table.Column<int>(type: "INTEGER", nullable: true),
                    BasicResponseDtoId = table.Column<int>(type: "INTEGER", nullable: true),
                    DetailResponseDtoId = table.Column<int>(type: "INTEGER", nullable: true),
                    SoftDeletable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Auditable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Archivable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entities_Dtos_BasicResponseDtoId",
                        column: x => x.BasicResponseDtoId,
                        principalTable: "Dtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entities_Dtos_CreateDtoId",
                        column: x => x.CreateDtoId,
                        principalTable: "Dtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entities_Dtos_DeleteDtoId",
                        column: x => x.DeleteDtoId,
                        principalTable: "Dtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entities_Dtos_DetailResponseDtoId",
                        column: x => x.DetailResponseDtoId,
                        principalTable: "Dtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entities_Dtos_ReportDtoId",
                        column: x => x.ReportDtoId,
                        principalTable: "Dtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entities_Dtos_UpdateDtoId",
                        column: x => x.UpdateDtoId,
                        principalTable: "Dtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsUnique = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsList = table.Column<bool>(type: "INTEGER", nullable: false),
                    Filterable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fields_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Fields_FieldTypes_FieldTypeId",
                        column: x => x.FieldTypeId,
                        principalTable: "FieldTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceLayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Entities_RelatedEntityId",
                        column: x => x.RelatedEntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Services_ServiceLayers_ServiceLayerId",
                        column: x => x.ServiceLayerId,
                        principalTable: "ServiceLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrimaryFieldId = table.Column<int>(type: "INTEGER", nullable: false),
                    ForeignFieldId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeleteBehaviorTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryEntityVirPropName = table.Column<string>(type: "TEXT", nullable: false),
                    ForeignEntityVirPropName = table.Column<string>(type: "TEXT", nullable: false),
                    Control = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relations_DeleteBehaviorTypes_DeleteBehaviorTypeId",
                        column: x => x.DeleteBehaviorTypeId,
                        principalTable: "DeleteBehaviorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Relations_Fields_ForeignFieldId",
                        column: x => x.ForeignFieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relations_Fields_PrimaryFieldId",
                        column: x => x.PrimaryFieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relations_RelationTypes_RelationTypeId",
                        column: x => x.RelationTypeId,
                        principalTable: "RelationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "Control", "DBConnectionString", "IsThereIdentity", "IsThereRole", "IsThereUser", "Path", "ProjectName", "RoleEntityId", "SolutionName", "UserEntityId" },
                values: new object[] { 1, false, "Data Source=.; Initial Catalog=MyGeneratedDatabase; Integrated Security=SSPI; Trusted_Connection=True; TrustServerCertificate=True;", true, false, false, "C:\\GeneratedProjects", "MyProject", null, "MyProject", null });

            migrationBuilder.InsertData(
                table: "CrudTypes",
                columns: new[] { "Id", "Control", "Name" },
                values: new object[,]
                {
                    { 1, false, "Read" },
                    { 2, false, "Create" },
                    { 3, false, "Update" },
                    { 4, false, "Delete" }
                });

            migrationBuilder.InsertData(
                table: "DeleteBehaviorTypes",
                columns: new[] { "Id", "Control", "Description", "Name" },
                values: new object[,]
                {
                    { 1, false, null, "Cascade" },
                    { 2, false, null, "ClientCascade" },
                    { 3, false, null, "Restrict" },
                    { 4, false, null, "ClientSetNull" },
                    { 5, false, null, "ClientNoAction" },
                    { 6, false, null, "SetNull" },
                    { 7, false, null, "NoAction" }
                });

            migrationBuilder.InsertData(
                table: "FieldTypeSources",
                columns: new[] { "Id", "Control", "Name" },
                values: new object[,]
                {
                    { 1, false, "Base" },
                    { 2, false, "Entity" },
                    { 3, false, "Dto" }
                });

            migrationBuilder.InsertData(
                table: "RelationTypes",
                columns: new[] { "Id", "Control", "Name" },
                values: new object[,]
                {
                    { 1, false, "OneToOne" },
                    { 2, false, "OneToMany" }
                });

            migrationBuilder.InsertData(
                table: "ServiceLayers",
                columns: new[] { "Id", "Control", "Name" },
                values: new object[,]
                {
                    { 1, false, "Core" },
                    { 2, false, "Model" },
                    { 3, false, "DataAccess" },
                    { 4, false, "Business" },
                    { 5, false, "Presentation" }
                });

            migrationBuilder.InsertData(
                table: "ValidatorTypes",
                columns: new[] { "Id", "Control", "Description", "Name" },
                values: new object[,]
                {
                    { 1, false, "Field cannot be empty", "NotEmpty" },
                    { 2, false, "Field cannot be null", "NotNull" },
                    { 3, false, "Field cannot be ...", "NotEqual" },
                    { 4, false, "Field cannot exceed maximum length", "MaxLength" },
                    { 5, false, "Value must be within a specific range", "Range" },
                    { 6, false, "Field must have a minimum number of characters", "MinLength" },
                    { 7, false, "Field must match a regular expression", "Regex" },
                    { 8, false, "Value must be greater than a specific number", "GreaterThan" },
                    { 9, false, "Value must be less than a specific number", "LessThan" },
                    { 10, false, "Field must be a valid email address", "EmailAddress" },
                    { 11, false, "Field must be a valid credit card number", "CreditCard" },
                    { 12, false, "Field must be a valid phone number", "Phone" },
                    { 13, false, "Field must be a valid URL", "Url" },
                    { 14, false, "Field must be a valid date", "Date" },
                    { 15, false, "Field must be a valid number", "Number" },
                    { 16, false, "Field mus be a valid guid value", "GuidNotEmpty" },
                    { 17, false, "Field must have a exact number of characters\", \"Length", "Length" }
                });

            migrationBuilder.InsertData(
                table: "FieldTypes",
                columns: new[] { "Id", "Control", "Name", "SourceTypeId" },
                values: new object[,]
                {
                    { 1, false, "Int", 1 },
                    { 2, false, "String", 1 },
                    { 3, false, "Long", 1 },
                    { 4, false, "Float", 1 },
                    { 5, false, "Double", 1 },
                    { 6, false, "Bool", 1 },
                    { 7, false, "Char", 1 },
                    { 8, false, "Byte", 1 },
                    { 9, false, "DateTime", 1 },
                    { 10, false, "DateOnly", 1 },
                    { 11, false, "Guid", 1 },
                    { 12, false, "TimeSpan", 1 },
                    { 13, false, "TimeOnly", 1 }
                });

            migrationBuilder.InsertData(
                table: "ValidatorTypeParams",
                columns: new[] { "Id", "Control", "Key", "ValidatorTypeId" },
                values: new object[,]
                {
                    { 1, false, "Value", 3 },
                    { 2, false, "Max", 4 },
                    { 3, false, "Min", 5 },
                    { 4, false, "Max", 5 },
                    { 5, false, "Min", 6 },
                    { 6, false, "Pattern", 7 },
                    { 7, false, "Value", 8 },
                    { 8, false, "Value", 9 },
                    { 9, false, "Value", 17 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_RoleEntityId",
                table: "AppSettings",
                column: "RoleEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_UserEntityId",
                table: "AppSettings",
                column: "UserEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DtoFieldRelations_RelationId",
                table: "DtoFieldRelations",
                column: "RelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DtoFields_DtoId",
                table: "DtoFields",
                column: "DtoId");

            migrationBuilder.CreateIndex(
                name: "IX_DtoFields_SourceFieldId",
                table: "DtoFields",
                column: "SourceFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Dtos_CrudTypeId",
                table: "Dtos",
                column: "CrudTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dtos_RelatedEntityId",
                table: "Dtos",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_BasicResponseDtoId",
                table: "Entities",
                column: "BasicResponseDtoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_CreateDtoId",
                table: "Entities",
                column: "CreateDtoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_DeleteDtoId",
                table: "Entities",
                column: "DeleteDtoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_DetailResponseDtoId",
                table: "Entities",
                column: "DetailResponseDtoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_ReportDtoId",
                table: "Entities",
                column: "ReportDtoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_UpdateDtoId",
                table: "Entities",
                column: "UpdateDtoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_EntityId",
                table: "Fields",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_FieldTypeId",
                table: "Fields",
                column: "FieldTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTypes_SourceTypeId",
                table: "FieldTypes",
                column: "SourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Relations_DeleteBehaviorTypeId",
                table: "Relations",
                column: "DeleteBehaviorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Relations_ForeignFieldId",
                table: "Relations",
                column: "ForeignFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Relations_PrimaryFieldId_ForeignFieldId",
                table: "Relations",
                columns: new[] { "PrimaryFieldId", "ForeignFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Relations_RelationTypeId",
                table: "Relations",
                column: "RelationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_RelatedEntityId",
                table: "Services",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceLayerId_RelatedEntityId",
                table: "Services",
                columns: new[] { "ServiceLayerId", "RelatedEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationParams_ValidatorTypeParamId",
                table: "ValidationParams",
                column: "ValidatorTypeParamId");

            migrationBuilder.CreateIndex(
                name: "IX_Validations_DtoFieldId",
                table: "Validations",
                column: "DtoFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Validations_ValidatorTypeId",
                table: "Validations",
                column: "ValidatorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidatorTypeParams_ValidatorTypeId",
                table: "ValidatorTypeParams",
                column: "ValidatorTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppSettings_Entities_RoleEntityId",
                table: "AppSettings",
                column: "RoleEntityId",
                principalTable: "Entities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppSettings_Entities_UserEntityId",
                table: "AppSettings",
                column: "UserEntityId",
                principalTable: "Entities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DtoFieldRelations_DtoFields_DtoFieldId",
                table: "DtoFieldRelations",
                column: "DtoFieldId",
                principalTable: "DtoFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DtoFieldRelations_Relations_RelationId",
                table: "DtoFieldRelations",
                column: "RelationId",
                principalTable: "Relations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DtoFields_Dtos_DtoId",
                table: "DtoFields",
                column: "DtoId",
                principalTable: "Dtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DtoFields_Fields_SourceFieldId",
                table: "DtoFields",
                column: "SourceFieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Dtos_Entities_RelatedEntityId",
                table: "Dtos",
                column: "RelatedEntityId",
                principalTable: "Entities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dtos_Entities_RelatedEntityId",
                table: "Dtos");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "DtoFieldRelations");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "ValidationParams");

            migrationBuilder.DropTable(
                name: "Relations");

            migrationBuilder.DropTable(
                name: "ServiceLayers");

            migrationBuilder.DropTable(
                name: "Validations");

            migrationBuilder.DropTable(
                name: "ValidatorTypeParams");

            migrationBuilder.DropTable(
                name: "DeleteBehaviorTypes");

            migrationBuilder.DropTable(
                name: "RelationTypes");

            migrationBuilder.DropTable(
                name: "DtoFields");

            migrationBuilder.DropTable(
                name: "ValidatorTypes");

            migrationBuilder.DropTable(
                name: "Fields");

            migrationBuilder.DropTable(
                name: "FieldTypes");

            migrationBuilder.DropTable(
                name: "FieldTypeSources");

            migrationBuilder.DropTable(
                name: "Entities");

            migrationBuilder.DropTable(
                name: "Dtos");

            migrationBuilder.DropTable(
                name: "CrudTypes");
        }
    }
}
