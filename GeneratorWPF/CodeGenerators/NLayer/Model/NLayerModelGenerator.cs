using GeneratorWPF.CodeGenerators.NLayer.Base;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Models.Statics;
using GeneratorWPF.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GeneratorWPF.CodeGenerators.NLayer.Model;

public class NLayerModelGenerator : NLayerGeneratorBase
{
    private readonly EntityRepository _entityRepository;
    private readonly DtoRepository _dtoRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly RelationRepository _relationRepository;
    private readonly DtoFieldRepository _dtoFieldRepository;
    public NLayerModelGenerator(AppSetting appSetting) : base(appSetting)
    {
        _entityRepository = new();
        _dtoRepository = new();
        _fieldRepository = new();
        _relationRepository = new();
        _dtoFieldRepository = new();
    }

    #region Auth-Models
    public string GenerateAuthModels()
    {
        var results = new List<string>();
        var uniqueFields = _fieldRepository.GetAll(f => f.EntityId == _appSetting.UserEntityId && f.IsUnique);

        #region 1. Login Models
        var loginRequest = CompilationUnit(
            usings: [
                "FluentValidation",
                $"{_appSetting.CoreLayerProjectName}.Utils.CriticalData"
            ],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Auth.Login",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "LoginRequest",
                        members: [
                            PropertyDeclaration("string", "Email", false),
                            PropertyDeclaration("string", "UserName", false),
                            PropertyDeclaration("string", "Password", true, attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("CriticalData"))]),
                            PropertyDeclaration("Guid", "DeviceId", false),
                            PropertyDeclaration("string", "ClientType", true)
                        ]
                    ),
                    ValidatorClassDeclaration(
                        modelName: "LoginRequest",
                        ruleList: [
                            "RuleFor(b => b).Must(b => !string.IsNullOrWhiteSpace(b.Email) || !string.IsNullOrWhiteSpace(b.UserName)).WithMessage(\"Either Email or UserName must be provided.\");",
                            "RuleFor(b => b.UserName).MinimumLength(6).When(b => !string.IsNullOrWhiteSpace(b.UserName));",
                            "RuleFor(b => b.Email).EmailAddress().When(b => !string.IsNullOrWhiteSpace(b.Email));",
                            "RuleFor(b => b.Password).NotNull().NotEmpty().MinimumLength(6);",
                            "RuleFor(b => b.ClientType).NotNull().NotEmpty();"
                        ]
                    )
                ]
            )
        );
        var loginResponse = CompilationUnit(
            usings: [$"{_appSetting.CoreLayerProjectName}.Utils.Auth"],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Auth.Login",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "LoginResponse",
                        members: [
                            PropertyDeclaration("IList<string>", "Roles", false),
                            PropertyDeclaration("AccessToken", "AccessToken", true),
                            PropertyDeclaration("Guid", "DeviceId", true)
                        ]
                    ),
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "LoginTrustedResponse",
                        baseTypes: [SyntaxFactory.ParseTypeName("LoginResponse")],
                        members: [PropertyDeclaration("string", "RefreshToken", true)]
                    )
                ]
            )
        );

        string folderPathLogin = Path.Combine(_appSetting.SolutionPath, _appSetting.ModelLayerProjectName, "Auth", "Login");
        results.Add(AddFile(folderPathLogin, "LoginRequest.cs", loginRequest.ToFullString()));
        results.Add(AddFile(folderPathLogin, "LoginResponse.cs", loginResponse.ToFullString()));
        #endregion

        #region 2. Refresh Auth Models
        var refreshAuthRequest = CompilationUnit(
            usings: ["FluentValidation"],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Auth.Refresh",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshAuthRequest",
                        members: [
                            PropertyDeclaration("string", "RefreshToken", false),
                            PropertyDeclaration("Guid", "DeviceId", true),
                            PropertyDeclaration(uniqueFields.FirstOrDefault()?.GetMapedTypeName() ?? "Guid", "UserId", true)
                        ]
                    ),
                    ValidatorClassDeclaration(
                        modelName: "RefreshAuthRequest",
                        ruleList: [
                            "RuleFor(b => b.UserId).NotNull().NotEmpty();",
                            "RuleFor(b => b.DeviceId).NotNull().NotEqual(Guid.Empty).NotEmpty();"
                        ]
                    )
                ]
            )
        );

        var refreshAuthResponse = CompilationUnit(
            usings: [$"{_appSetting.CoreLayerProjectName}.Utils.Auth"],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Auth.Refresh",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshAuthResponse",
                        members: [
                            PropertyDeclaration("IList<string>", "Roles", false),
                            PropertyDeclaration("AccessToken", "AccessToken", true),
                        ]
                    ),
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshAuthTrustedResponse",
                        baseTypes: [SyntaxFactory.ParseTypeName("RefreshAuthResponse")],
                        members: [PropertyDeclaration("string", "RefreshToken", true)]
                    )
                ]
            )
        );

        string folderPathRefreshAuth = Path.Combine(_appSetting.SolutionPath, _appSetting.ModelLayerProjectName, "Auth", "RefreshAuth");
        results.Add(AddFile(folderPathRefreshAuth, "RefreshAuthRequest.cs", refreshAuthRequest.ToFullString()));
        results.Add(AddFile(folderPathRefreshAuth, "RefreshAuthResponse.cs", refreshAuthResponse.ToFullString()));
        #endregion

        #region 3. Signup Models
        var code_SignUpRequest = CompilationUnit(
            usings: [
                "FluentValidation",
                $"{_appSetting.CoreLayerProjectName}.Utils.CriticalData"
            ],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Auth.SignUp",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "SignUpRequest",
                        members: [
                            PropertyDeclaration("string", "Email", true),
                            PropertyDeclaration("string", "UserName", true),
                            PropertyDeclaration("string", "Password", true, attributes: [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("CriticalData"))]),
                            PropertyDeclaration("Guid", "DeviceId", false),
                            PropertyDeclaration("string", "ClientType", true)
                        ]
                    ),
                    ValidatorClassDeclaration(
                        modelName: "SignUpRequest",
                        ruleList:  [
                            "RuleFor(b => b.Email).NotNull().NotEmpty().EmailAddress();",
                            "RuleFor(b => b.UserName).NotNull().NotEmpty().MinimumLength(6);",
                            "RuleFor(b => b.Password).NotNull().NotEmpty().MinimumLength(6);",
                            "RuleFor(b => b.ClientType).NotNull().NotEmpty();"
                        ]
                    )
                ]
            )
        );

        var code_SignUpResponse = CompilationUnit(
            usings: [$"{_appSetting.CoreLayerProjectName}.Utils.Auth"],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Auth.SignUp",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "SignUpResponse",
                        members: [
                            PropertyDeclaration("IList<string>", "Roles", false),
                            PropertyDeclaration("AccessToken", "AccessToken", true),
                            PropertyDeclaration("Guid", "DeviceId", true)
                        ]
                    ),
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "SignUpTrustedResponse",
                        baseTypes: [SyntaxFactory.ParseTypeName("SignUpResponse")],
                        members: [PropertyDeclaration("string", "RefreshToken", true)]
                    )
                ]
            )
        );

        string folderPathSignup = Path.Combine(_appSetting.SolutionPath, _appSetting.ModelLayerProjectName, "Auth", "SignUp");
        results.Add(AddFile(folderPathSignup, "SignUpRequest.cs", code_SignUpRequest.ToFullString()));
        results.Add(AddFile(folderPathSignup, "SignUpResponse.cs", code_SignUpResponse.ToFullString()));
        #endregion

        return string.Join("\n", results);
    }
    #endregion

    #region Entities
    public string GenerateEntities()
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        foreach (var entity in entities)
        {
            string code = HandleGenerateEntity(entity);

            string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.ModelLayerProjectName, "Entities");
            results.Add(AddFile(folderPath, $"{entity.Name}.cs", code));
        }

        #region RefreshToken
        if (_appSetting.IsThereIdentity)
        {
            var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);

            string code_refreshToken = CompilationUnit(
                usings: [$"{_appSetting.CoreLayerProjectName}.Model"],
                nspace: NamespaceDeclaration(
                    value: $"{_appSetting.ModelLayerProjectName}.Entities",
                    members: [
                        ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RefreshToken",
                        baseTypes: [SyntaxFactory.IdentifierName("IEntity")],
                        members: [
                            PropertyDeclaration("Guid", "Id", true),
                            PropertyDeclaration(identityTypeConfigs.IdentityKeyType, "UserId", true),
                            PropertyDeclaration("Guid", "DeviceId", true),
                            PropertyDeclaration("string", "IpAddress", false),
                            PropertyDeclaration("string", "ClientType", false),
                            PropertyDeclaration("string", "Token", true),
                            PropertyDeclaration("DateTime", "ExpirationUtc", true),
                            PropertyDeclaration("DateTime", "CreateDateUtc", true),
                            PropertyDeclaration("int", "TTL", true),
                            PropertyDeclaration("bool", "IsRevoked", true),
                            PropertyDeclaration($"virtual {identityTypeConfigs.IdentityUserType}?", "User", false)
                        ])
                    ]
                )
            ).ToFullString();

            string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.ModelLayerProjectName, "Entities");
            results.Add(AddFile(folderPath, "RefreshToken.cs", code_refreshToken));
        }
        #endregion

        return string.Join("\n", results);
    }

    private string HandleGenerateEntity(Entity entity)
    {
        // 1) Implemantation List
        List<string> interfaces = new();
        if (_appSetting.IsThereUser && _appSetting.UserEntityId == entity.Id)
            interfaces.Add($"IdentityUser<{entity.Fields.FirstOrDefault(f => f.IsUnique)?.GetMapedTypeName()}>");
        if (_appSetting.IsThereRole && _appSetting.RoleEntityId == entity.Id)
            interfaces.Add($"IdentityRole<{entity.Fields.FirstOrDefault(f => f.IsUnique)?.GetMapedTypeName()}>");

        interfaces.Add(Statics.IEntity);
        if (entity.SoftDeletable) interfaces.Add(Statics.ISoftDeletableEntity);
        if (entity.Archivable) interfaces.Add(Statics.IArchivableEntity);
        if (entity.Auditable) interfaces.Add(Statics.IAuditableEntity);

        // 2) Property List
        List<PropertyDeclarationSyntax> properties = new();

        var baseTypeFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id, include: i => i.Include(x => x.FieldType));
        foreach (var field in baseTypeFields.Where(f => f.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base))
        {
            string fieldTypeName = field.GetMapedTypeName();
            if (field.IsList) fieldTypeName = $"List<{fieldTypeName}>";

            properties.Add(PropertyDeclaration(fieldTypeName, field.Name, field.IsRequired));
        }

        // 3) Implemented İnterface List
        if (entity.Auditable)
        {
            properties.Add(PropertyDeclaration("string", "CreatedBy", false));
            properties.Add(PropertyDeclaration("string", "UpdatedBy", false));
            properties.Add(PropertyDeclaration("DateTime", "CreateDateUtc", false));
            properties.Add(PropertyDeclaration("DateTime", "UpdateDateUtc", false));
        }
        if (entity.SoftDeletable)
        {
            properties.Add(PropertyDeclaration("string", "DeletedBy", false));
            properties.Add(PropertyDeclaration("bool", "IsDeleted"));
            properties.Add(PropertyDeclaration("DateTime", "DeletedDateUtc", false));
        }

        // 4) Virtual Propert List
        HandleVirtualProps(ref properties, entity.Id);


        // 5) Usings
        List<string> usings = new(){
            $"{_appSetting.CoreLayerProjectName}.Model"
        };

        if (_appSetting.UserEntityId == entity.Id || _appSetting.RoleEntityId == entity.Id)
            usings.Add("Microsoft.AspNetCore.Identity");

        return CompilationUnit(
            usings: [.. usings],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Entities",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: entity.Name,
                        baseTypes: [.. interfaces.Select(i => SyntaxFactory.ParseTypeName(i))],
                        members: [..properties]
                    )
                ]
            )
        ).ToFullString();
    }

    private void HandleVirtualProps(ref List<PropertyDeclarationSyntax> propertyList, int entityId)
    {
        var relationsOnPrimary = _relationRepository.GetRelationsOnPrimary(entityId);
        var relationsOnForeign = _relationRepository.GetRelationsOnForeign(entityId);

        foreach (var relation in relationsOnPrimary)
        {
            if (relation.RelationTypeId == (int)RelationTypeEnums.OneToOne)
            {
                propertyList.Add(PropertyDeclaration($"{relation.ForeignField.Entity.Name}?", relation.PrimaryEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
        }
        foreach (var relation in relationsOnForeign)
        {
            if (relation.RelationTypeId == (int)RelationTypeEnums.OneToOne)
            {
                propertyList.Add(PropertyDeclaration($"{relation.PrimaryField.Entity.Name}?", relation.ForeignEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
            else if (relation.RelationTypeId == (int)RelationTypeEnums.OneToMany)
            {
                propertyList.Add(PropertyDeclaration($"{relation.PrimaryField.Entity.Name}?", relation.ForeignEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
        }

        foreach (var relation in relationsOnPrimary)
        {
            if (relation.RelationTypeId == (int)RelationTypeEnums.OneToMany)
            {
                propertyList.Add(PropertyDeclaration($"ICollection<{relation.ForeignField.Entity.Name}>?", relation.PrimaryEntityVirPropName, modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
            }
        }

        if (entityId == _appSetting.UserEntityId)
        {
            propertyList.Add(PropertyDeclaration("ICollection<RefreshToken>?", "RefreshTokens", modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword]));
        }
    }
    #endregion

    #region Dtos
    public string GenerateDtos()
    {
        var results = new List<string>();

        var dtos = _dtoRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.RelatedEntity).ThenInclude(x => x.Fields));

        foreach (var dto in dtos)
        {
            string code = HandleGeneraterDto(dto);

            string commandOrQuery = dto.CrudTypeId == (int)CrudTypeEnums.Read ? "Queries" : "Commands";
            string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.ModelLayerProjectName, "Dtos", dto.RelatedEntity.Name, commandOrQuery);

            results.Add(AddFile(folderPath, $"{dto.Name}.cs", code));
        }
        return string.Join("\n", results);
    }

    private string HandleGeneraterDto(Dto dto)
    {
        var dtoFieldList = _dtoFieldRepository.GetAll(
            filter: f => f.DtoId == dto.Id,
            include: i => i
                .Include(x => x.SourceField).ThenInclude(x => x.FieldType)
                .Include(x => x.SourceField).ThenInclude(x => x.Entity).ThenInclude(x => x.Dtos)
                .Include(x => x.Validations).ThenInclude(x => x.ValidatorType)
                .Include(x => x.Validations).ThenInclude(x => x.ValidationParams)
            );
        bool isExistValidation = dtoFieldList.Any(f => f.Validations != null && f.Validations.Any());

        #region 1) Property List
        List<PropertyDeclarationSyntax> properties = new();

        bool isReportDto = dto.RelatedEntity.ReportDtoId == dto.Id;
        if (isReportDto)
        {
            List<Field> uniqueFields = dto.RelatedEntity.Fields.Where(f => f.IsUnique).ToList();
            foreach (var unqField in uniqueFields)
            {
                // if there is no unique field with same name in dto
                if (dtoFieldList.Any(f => f.SourceFieldId == unqField.Id && f.Name.Trim() == unqField.Name.Trim()) == false)
                    properties.Add(PropertyDeclaration(unqField.GetMapedTypeName(), unqField.Name, true));
            }
        }

        foreach (var dtoField in dtoFieldList)
        {
            string fieldTypeName = dtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base ?
                dtoField.SourceField.GetMapedTypeName() : dtoField.SourceField.FieldType.Name;

            if (dtoField.SourceField.IsList)
                fieldTypeName = $"List<{fieldTypeName}>";
            if (!dtoField.SourceField.IsRequired)
                fieldTypeName = $"{fieldTypeName}?";
            if (dtoField.IsList)
            {
                fieldTypeName = $"List<{fieldTypeName}>";
                if (!dtoField.SourceField.IsRequired)
                    fieldTypeName = $"{fieldTypeName}?";
            }
            //if (!dtoField.IsRequired)
            //    fieldTypeName = $"{fieldTypeName}?";

            properties.Add(PropertyDeclaration(fieldTypeName, dtoField.Name, dtoField.IsRequired));
        }
        if (isReportDto)
        {
            if (dto.RelatedEntity.Auditable)
            {
                properties.Add(PropertyDeclaration("string", "CreatedBy", false));
                properties.Add(PropertyDeclaration("string", "UpdatedBy", false));
                properties.Add(PropertyDeclaration("DateTime", "CreateDateUtc", false));
                properties.Add(PropertyDeclaration("DateTime", "UpdateDateUtc", false));
            }
            if (dto.RelatedEntity.SoftDeletable)
            {
                properties.Add(PropertyDeclaration("string", "DeletedBy", false));
                properties.Add(PropertyDeclaration("bool", "IsDeleted", true));
                properties.Add(PropertyDeclaration("DateTime", "DeletedDateUtc", false));
            }
        }
        #endregion

        #region 2) Usings
        List<string> usings = new(){
            $"{_appSetting.CoreLayerProjectName}.Model"
        };
        if (isExistValidation)
            usings.Add("FluentValidation");
        if (dtoFieldList.Any(f => f.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Entity))
            usings.Add($"{_appSetting.ModelLayerProjectName}.Entities");

        if (dtoFieldList.Any(f => f.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Dto))
        {
            List<int> addedSourceEntites = new() { dto.RelatedEntityId };
            foreach (var dtoField in dtoFieldList.Where(f => f.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Dto))
            {
                if (addedSourceEntites.Any(f => f == dtoField.SourceField.EntityId))
                    continue;

                addedSourceEntites.Add(dtoField.SourceField.EntityId);
                if (dtoField.SourceField.Entity.Dtos?.Any(f => f.CrudTypeId != (int)CrudTypeEnums.Read) == true)
                    usings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{dtoField.SourceField.Entity.Name}.Commands");
                if (dtoField.SourceField.Entity.Dtos?.Any(f => f.CrudTypeId == (int)CrudTypeEnums.Read) == true)
                    usings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{dtoField.SourceField.Entity.Name}.Queries");
            }
        }
        #endregion

        List<ClassDeclarationSyntax> classes = new()
        {
            ClassDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: dto.Name,
                baseTypes: [SyntaxFactory.ParseTypeName("IDto")],
                members: [..properties]
            )
        };

        if (isExistValidation)
        {
            var rules = new List<string>();
            foreach (var dtoField in dtoFieldList)
            {
                if (dtoField.Validations == null || !dtoField.Validations.Any())
                    continue;

                foreach (var validation in dtoField.Validations)
                {
                    string rule = ValidationRule(validation, dtoField.Name, validation.ErrorMessage);
                    if (string.IsNullOrWhiteSpace(rule)) continue;
                    rules.Add(rule);
                }
            }
            classes.Add(ValidatorClassDeclaration(dto.Name, [.. rules]));
        }

        return CompilationUnit(
            usings: [.. usings],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.ModelLayerProjectName}.Dtos.{dto.RelatedEntity.Name}.{(dto.CrudTypeId == (int)CrudTypeEnums.Read ? "Queries" : "Commands")}",
                members: [.. classes]
            )
        ).ToFullString();
    }
    #endregion
}