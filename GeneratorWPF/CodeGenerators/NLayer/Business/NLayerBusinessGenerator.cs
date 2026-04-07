using Azure.Core;
using Generator.NTier.CodeGenerators.NLayer.Business.Helpers;
using GeneratorWPF.CodeGenerators.NLayer.Base;
using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Humanizer;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace GeneratorWPF.CodeGenerators.NLayer.Business;

public class NLayerBusinessGenerator : NLayerGeneratorBase
{
    private readonly DtoRepository _dtoRepository;
    private readonly DtoFieldRepository _dtoFieldRepository;
    private readonly EntityRepository _entityRepository;
    public NLayerBusinessGenerator(AppSetting appSetting) : base(appSetting)
    {
        _dtoRepository = new();
        _dtoFieldRepository = new();
        _entityRepository = new();
    }

    public string GenerateMappings()
    {
        var code = CompilationUnit(
            usings: MappingProfilesHelper.GetUsings(_entityRepository, _dtoRepository, _appSetting),
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.BusinessLayerProjectName}.Mappings",
                members: [
                    ClassDeclaration(
                        name: "MappingProfiles",
                        modifiers: [SyntaxKind.PublicKeyword],
                        baseTypes: [SyntaxFactory.ParseTypeName("Profile")],
                        members: [
                            ConstructorDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword],
                                name: "MappingProfiles",
                                block: MappingProfilesHelper.GetRules(_entityRepository, _dtoRepository, _dtoFieldRepository, _appSetting)
                            )
                        ]
                    )
                ]
            )
        ).ToFullString();
        
        string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.BusinessLayerProjectName, "Mappings");
        return AddFile(folderPath, "MappingProfiles", code);
    }

    public string GeneraterService()
    {
        var results = new List<string>();

        var roslynBusinessServiceGenerator = new RoslynBusinessServiceGenerator(_appSetting);

        string folderPathAbstract = Path.Combine(_appSetting.SolutionPath, _appSetting.BusinessLayerProjectName, "Abstract");
        string folderPathConcrete = Path.Combine(_appSetting.SolutionPath, _appSetting.BusinessLayerProjectName, "Concrete");

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields).ThenInclude(y => y.FieldType));

        foreach (var entity in entities)
        {
            var dtos = _dtoRepository.GetAll(
                filter: f => f.RelatedEntityId == entity.Id,
                include: i => i
                    .Include(x => x.DtoFields).ThenInclude(ti => ti.SourceField)
                    .Include(x => x.RelatedEntity).ThenInclude(ti => ti.Fields));

            var code_abstract = CompilationUnit(
                usings: [
                    $"{_appSetting.CoreLayerProjectName}.BaseRequestModels",              
                    $"{_appSetting.CoreLayerProjectName}.Utils.Datatable",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Pagination",
                    $"{_appSetting.CoreLayerProjectName}.Utils.ResultPattern",
                    $"{_appSetting.BusinessLayerProjectName}.Dtos.{entity.Name}.Commands",
                    $"{_appSetting.BusinessLayerProjectName}.Dtos.{entity.Name}.Queries",
                    $"{_appSetting.BusinessLayerProjectName}.Entities",
                    "System.Linq.Expressions"
                ],
                nspace: NamespaceDeclaration(
                    value: $"{_appSetting.BusinessLayerProjectName}.Abstract",
                    members: [
                        InterfaceDeclaration(
                            name: $"I{entity.Name}Service",
                            modifiers: [SyntaxKind.PublicKeyword],
                            members: [..GenerateAbstractMethods(entity, dtos)]
                        )
                    ]
                )
            ).ToFullString();

            string code_concrete = roslynBusinessServiceGenerator.GeneraterConcrete(entity, dtos);

            results.Add(AddFile(folderPathAbstract, $"I{entity.Name}Service", code_abstract));
            results.Add(AddFile(folderPathConcrete, $"{entity.Name}Service", code_concrete));
        }

        if (_appSetting.IsThereIdentiy)
        {
            string code_IAuthService = @"using Model.Auth.Login;
using Model.Auth.RefreshAuth;
using Model.Auth.SignUp;

namespace Business.Abstract;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default);
    Task<SignUpResponse> SignUpAsync(SignUpRequest signUpRequest, CancellationToken cancellationToken = default);
    Task<RefreshAuthResponse> RefreshAuthAsync(RefreshAuthRequest refreshAuthRequest, CancellationToken cancellationToken = default);
    Task LoginWebBaseAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default);
    Task SignUpWebBaseAsync(SignUpRequest signUpRequest, CancellationToken cancellationToken = default);
}";

            results.Add(AddFile(folderPathAbstract, "IAuthService", code_IAuthService));


            string code_AuthService = roslynBusinessServiceGenerator.GeneraterAuthServiceConcrete();

            results.Add(AddFile(folderPathConcrete, "AuthService", code_AuthService));
        }

        return string.Join("\n", results);
    }

    private List<MethodDeclarationSyntax> GenerateAbstractMethods(Entity entity, List<Dto> dtos)
    {
        var methods = new List<MethodDeclarationSyntax>();

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).ToList();
        var uniqueFieldParameters = uniqueFields.Select(f => ParameterDeclaration(f.GetMapedTypeName(), f.Name.ToCamelCase(), true)).ToList();

        var reportDto = dtos.FirstOrDefault(f => f.Id == entity.ReportDtoId);

        #region GET
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetAsync",
            returnType: $"Task<Result<{entity.Name}>>",
            parameters: [ 
                ParameterDeclaration($"Expression<Func<{entity.Name}, bool>>", "where", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetAsync",
            returnType: $"Task<Result<{entity.Name}>>",
            parameters: [
                ..uniqueFieldParameters,
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));

        foreach (var dto in dtos.Where(f=> f.CrudTypeId == (int)CrudTypeEnums.Read))
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: dto.ServiceGetMethodName(entity),
                returnType: $"Task<Result<{dto.Name}>>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                isThereBody: false
            ));
        }
        #endregion

        #region GET LIST
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetListAsync",
            returnType: $"Task<Result<ICollection<{entity.Name}>>>",
            parameters: [
                ParameterDeclaration($"Expression<Func<{entity.Name}, bool>>", "where", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetListAsync",
            returnType: $"Task<Result<ICollection<{entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicRequest", "request", false),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));

        foreach (var dto in dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read))
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: dto.ServiceGetListMethodName(entity),
                returnType: $"Task<Result<ICollection<{dto.Name}>>>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                isThereBody: false
            ));
        }
        #endregion

        #region SELECT LIST
        if (entity.Fields.Count(f => f.IsUnique) == 1)
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "SelectListAsync",
                returnType: "Task<Result<SelectList>>",
                parameters: [
                    ParameterDeclaration($"Expression<Func<{entity.Name}, bool>>", "where", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                isThereBody: false
            ));
        }
        #endregion

        #region CREATE
        var createDto = dtos.FirstOrDefault(f => f.Id == entity.CreateDtoId);
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "CreateAsync",
            returnType: $"Task<Result>",
            parameters: [
                ParameterDeclaration(createDto?.Name ?? entity.Name, "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        #endregion

        #region UPDATE
        var updateDto = dtos.FirstOrDefault(f => f.Id == entity.UpdateDtoId);
        if (entity.UpdateDtoId != default && updateDto != default)
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "GetUpdateModelAsync",
                returnType: $"Task<Result<{updateDto.Name}>>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                isThereBody: false
            ));
        }
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "UpdateAsync",
            returnType: $"Task<Result>",
            parameters: [
                ParameterDeclaration(updateDto?.Name ?? entity.Name, "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        #endregion

        #region DELETE & RESTORE
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DeleteAsync",
            returnType: $"Task<Result>",
            parameters:
                deleteDto != null ? [
                    ParameterDeclaration(deleteDto.Name, "request", true) ,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ] :
                [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
            isThereBody: false
        ));

        if (entity.SoftDeletable) { 
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "RestoreAsync",
                returnType: $"Task<Result>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                isThereBody: false
            ));
        }
        #endregion

        #region PAGINATION
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "PaginationAsync",
            returnType: $"Task<Result<PaginationResponse<{reportDto?.Name ?? entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicPaginationRequest", "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        #endregion

        #region DATATABLE
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableClientSideAsync",
            returnType: $"Task<Result<DatatableResponseClientSide<{reportDto?.Name ?? entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicDatatableRequest", "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableServerSideAsync",
            returnType: $"Task<Result<DatatableResponseServerSide<{reportDto?.Name ?? entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicDatatableRequest", "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        #endregion

        return methods;
    }

    private List<MethodDeclarationSyntax> GenerateConcreteMethods(Entity entity, List<Dto> dtos)
    {
        var methods = new List<MethodDeclarationSyntax>();

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).ToList();
        var uniqueFieldParameters = uniqueFields.Select(f => ParameterDeclaration(f.GetMapedTypeName(), f.Name.ToCamelCase(), true)).ToList();

        var reportDto = dtos.FirstOrDefault(f => f.Id == entity.ReportDtoId);

        #region GET
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetAsync",
            returnType: $"Task<Result<{entity.Name}>>",
            parameters: [
                ParameterDeclaration($"Expression<Func<{entity.Name}, bool>>", "where", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            body: @$"
                var result = await _unitOfWork.{entity.Name.Pluralize()}.GetAsync(
                    where: where, 
                    cancellationToken: cancellationToken
                );
                if (result == null)
                    return Result<{entity.Name}>.NotFound();
                return Result<{entity.Name}>.Success(result);
            "
        ));
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetAsync",
            returnType: $"Task<Result<{entity.Name}>>",
            parameters: [
                ..uniqueFieldParameters,
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            body: @$"
                var result = await _unitOfWork.{entity.Name.Pluralize()}.GetAsync(
                    {entity.WhereRule(uniqueFields)}, 
                    cancellationToken: cancellationToken
                );
                if (result == null)
                    return Result<{entity.Name}>.NotFound();
                return Result<{entity.Name}>.Success(result);
            "
        ));

        foreach (var dto in dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read))
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: dto.ServiceGetMethodName(entity),
                returnType: $"Task<Result<{dto.Name}>>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: @$"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.GetAsync<{dto.Name}>(
                        configurationProvider: _mapper.ConfigurationProvider,                    
                        {entity.WhereRule(uniqueFields)},
                        cancellationToken: cancellationToken
                    );
                    if (result == null)
                        return Result<{dto.Name}>.NotFound();
                    return Result<{dto.Name}>.Success(result);
                "
            ));
        }
        #endregion

        #region GET LIST
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetListAsync",
            returnType: $"Task<Result<ICollection<{entity.Name}>>>",
            parameters: [
                ParameterDeclaration($"Expression<Func<{entity.Name}, bool>>", "where", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            body: @$"
                var result = await _unitOfWork.{entity.Name.Pluralize()}.GetAllAsync(
                    where: where,
                    cancellationToken: cancellationToken
                );
                if (result == null)
                    return Result<ICollection<{entity.Name}>>.NotFound();
                return Result<ICollection<{entity.Name}>>.Success(result);
            "
        ));
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "GetListAsync",
            returnType: $"Task<Result<ICollection<{entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicRequest", "request", false),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            body: $@"
                var result = await _unitOfWork.{entity.Name.Pluralize()}.GetAllAsync(
                    filter: request?.Filter,
                    sorts: request?.Sorts,
                    tracking: false,
                    cancellationToken: cancellationToken
                );
                if (result == null)
                    return Result<ICollection<{entity.Name}>>.NotFound();
                return Result<ICollection<{entity.Name}>>.Success(result);
            "
        ));

        foreach (var dto in dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read))
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: dto.ServiceGetListMethodName(entity),
                returnType: $"Task<Result<ICollection<{dto.Name}>>>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.GetAllAsync<{dto.Name}>(
                        configurationProvider: _mapper.ConfigurationProvider,
                        filter: request?.Filter,
                        sorts: request?.Sorts,
                        cancellationToken: cancellationToken
                    );
                    if (result == null)
                        return Result<ICollection<{dto.Name}>>.NotFound();
                    return Result<ICollection<{dto.Name}>>.Success(result);
                "
            ));
        }
        #endregion

        #region SELECT LIST
        if (entity.Fields.Count(f => f.IsUnique) == 1)
        {
            Field? slctTextField = entity.GetSelectListTextField();
            if (slctTextField != null)
            {
                Field slctUniqueField = entity.Fields.First(f => f.IsUnique);

                methods.Add(MethodDeclaration(
                    modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                    name: "SelectListAsync",
                    returnType: "Task<Result<SelectList>>",
                    parameters: [
                        ParameterDeclaration($"Expression<Func<{entity.Name}, bool>>", "where", true),
                        ParameterDeclaration("CancellationToken", "cancellationToken", false)
                    ],
                    body: $@"
                        var list = await _unitOfWork.{entity.Name.Pluralize()}.GetAllAsync<object>(
                            select: s => new
                            {{
                                s.{slctUniqueField.Name},
                                s.{slctTextField.Name}
                            }},
                            where: where,
                            cancellationToken: cancellationToken
                        );
                        var selectList = new SelectList(list ?? new List<object>(), ""{slctUniqueField.Name}"", ""{slctTextField.Name}"");
                        return Result<SelectList>.Success(selectList);
                    "
                ));
            }
        }
        #endregion

        #region CREATE
        var createDto = dtos.FirstOrDefault(f => f.Id == entity.CreateDtoId);
        if (createDto != null) { 
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "CreateAsync",
                returnType: $"Task<Result>",
                parameters: [
                    ParameterDeclaration(createDto.Name, "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var validationResult = await _validationService.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                        return Result.Validation(validationResult.Failures, description: $""Validation failed for {createDto.Name}"");

                    await _unitOfWork.{entity.Name.Pluralize()}.AddAndSaveAsync(_mapper.Map<{entity.Name}>(request), cancellationToken);
                    return Result.Success();
                "
            ));
        }
        else
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "CreateAsync",
                returnType: $"Task<Result>",
                parameters: [
                    ParameterDeclaration(entity.Name, "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    await _unitOfWork.{entity.Name.Pluralize()}.AddAndSaveAsync(request, cancellationToken);
                    return Result.Success();
                "
            ));
        }
        #endregion

        #region UPDATE
        var updateDto = dtos.FirstOrDefault(f => f.Id == entity.UpdateDtoId);
        if (updateDto != null)
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "GetUpdateModelAsync",
                returnType: $"Task<Result<{updateDto.Name}>>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.GetAsync<{updateDto.Name}>(
                        configurationProvider: _mapper.ConfigurationProvider,
                        {entity.WhereRule(uniqueFields)},
                        cancellationToken: cancellationToken
                    );
                    if (result == null)
                        return Result<{updateDto.Name}>.NotFound();
                    return Result<{updateDto.Name}>.Success(result);
                "
            ));
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "UpdateAsync",
                returnType: $"Task<Result>",
                parameters: [
                    ParameterDeclaration(updateDto?.Name ?? entity.Name, "request", true),
                            ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var validationResult = await _validationService.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                        return Result.Validation(validationResult.Failures);

                    var entity = await _unitOfWork.{entity.Name.Pluralize()}.GetAsync({entity.WhereRule(uniqueFields)}, cancellationToken: cancellationToken);
                    if (entity == null)
                        return Result.NotFound();

                    await _unitOfWork.{entity.Name.Pluralize()}.UpdateAndSaveAsync(_mapper.Map(request, entity), cancellationToken);
                    return Result.Success();
                "
            ));
        }
        else
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "UpdateAsync",
                returnType: $"Task<Result>",
                parameters: [
                    ParameterDeclaration(entity.Name, "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var entity = await _unitOfWork.{entity.Name.Pluralize()}.GetAsync({entity.WhereRule(uniqueFields, "request")}, cancellationToken: cancellationToken);
                    if (entity == null)
                        return Result.NotFound();

                    await _unitOfWork.{entity.Name.Pluralize()}.UpdateAndSaveAsync(_mapper.Map(request, entity), cancellationToken);
                    return Result.Success();
                "
            ));
        }
        #endregion

        #region DELETE & RESTORE
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DeleteAsync",
            returnType: $"Task<Result>",
            parameters:
                deleteDto != null ? [
                    ParameterDeclaration(deleteDto.Name, "request", true) ,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ] :
                [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
            isThereBody: false
        ));

        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "RestoreAsync",
            returnType: $"Task<Result>",
            parameters: [
                ..uniqueFieldParameters,
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        #endregion

        #region PAGINATION
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "PaginationAsync",
            returnType: $"Task<Result<PaginationResponse<{reportDto?.Name ?? entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicPaginationRequest", "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        #endregion

        #region DATATABLE
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableClientSideAsync",
            returnType: $"Task<Result<DatatableResponseClientSide<{reportDto?.Name ?? entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicDatatableRequest", "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        methods.Add(MethodDeclaration(
            modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
            name: "DatatableServerSideAsync",
            returnType: $"Task<Result<DatatableResponseServerSide<{reportDto?.Name ?? entity.Name}>>>",
            parameters: [
                ParameterDeclaration("DynamicDatatableRequest", "request", true),
                ParameterDeclaration("CancellationToken", "cancellationToken", false)
            ],
            isThereBody: false
        ));
        #endregion

        return methods;
    }

    public string GenerateServiceRegistrations(string solutionPath)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        StringBuilder sb = new();
        sb.AppendLine("using Autofac;");
        sb.AppendLine("using Autofac.Extras.DynamicProxy;");
        sb.AppendLine("using Business.Abstract;");
        sb.AppendLine("using Business.Concrete;");
        if (_appSetting.IsThereIdentiy) sb.AppendLine("using Business.Utils.TokenService;");
        sb.AppendLine("using Core.Utils.CrossCuttingConcerns;");
        sb.AppendLine();
        sb.AppendLine("namespace Business;");
        sb.AppendLine();
        sb.AppendLine("public class AutofacModule : Module");
        sb.AppendLine("{");
        sb.AppendLine("\tprotected override void Load(ContainerBuilder builder)");
        sb.AppendLine("\t{");
        if (_appSetting.IsThereIdentiy)
        {
            sb.AppendLine($"\t\tbuilder.RegisterType<TokenService>().As<ITokenService>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(ExceptionHandlerInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
            sb.AppendLine();

            sb.AppendLine($"\t\tbuilder.RegisterType<AuthService>().As<IAuthService>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(ValidationInterceptor), typeof(ExceptionHandlerInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
            sb.AppendLine();
        }
        sb.AppendLine("\t\t// ***** Entity Services *****");
        foreach (var entity in entities)
        {
            sb.AppendLine($"\t\tbuilder.RegisterType<{entity.Name}Service>().As<I{entity.Name}Service>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(ValidationInterceptor), typeof(ExceptionHandlerInterceptor), typeof(CacheRemoveInterceptor), typeof(CacheRemoveGroupInterceptor), typeof(CacheInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
            sb.AppendLine();
        }
        sb.AppendLine("\t}");
        sb.AppendLine("}");

        string code_ServiceRegistration = @"using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Business;

public static class ServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}";


        string folderPath = Path.Combine(solutionPath, "Business");
        results.Add(AddFile(folderPath, "AutofacModule", sb.ToString()));
        results.Add(AddFile(folderPath, "ServiceRegistration", code_ServiceRegistration));

        return string.Join("\n", results);
    }
}