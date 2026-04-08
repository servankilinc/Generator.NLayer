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
using System.IO;
using System.Text;

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

            List<string> dtoUsings = new();
            if (dtos.Any(f => f.CrudTypeId != (byte)CrudTypeEnums.Read))
                dtoUsings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Commands");
            if (dtos.Any(f => f.CrudTypeId == (byte)CrudTypeEnums.Read))
                dtoUsings.Add($"{_appSetting.ModelLayerProjectName}.Dtos.{entity.Name}.Queries");

            var code_abstract = CompilationUnit(
                usings: [
                    "System.Linq.Expressions",
                    "Microsoft.AspNetCore.Mvc.Rendering",
                    $"{_appSetting.CoreLayerProjectName}.BaseRequestModels",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Datatable",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Pagination",
                    $"{_appSetting.CoreLayerProjectName}.Utils.ResultPattern",
                    $"{_appSetting.ModelLayerProjectName}.Entities",
                    ..dtoUsings
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
            );

            var code_concrete = CompilationUnit(
                usings: [
                    "AutoMapper",
                    "System.Linq.Expressions",
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.AspNetCore.Mvc.Rendering",
                    $"{_appSetting.BusinessLayerProjectName}.Abstract",
                    $"{_appSetting.CoreLayerProjectName}.BaseRequestModels",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Datatable",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Pagination",
                    $"{_appSetting.CoreLayerProjectName}.Utils.ResultPattern",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Validation",
                    $"{_appSetting.DataAccessLayerProjectName}.Abstract",
                    $"{_appSetting.DataAccessLayerProjectName}.UoW",
                    $"{_appSetting.ModelLayerProjectName}.Entities",
                    ..dtoUsings
                ],
                nspace: NamespaceDeclaration(
                    value: $"{_appSetting.BusinessLayerProjectName}.Concrete",
                    members: [
                        ClassDeclaration(
                            name: $"{entity.Name}Service",
                            modifiers: [SyntaxKind.PublicKeyword],
                            baseTypes: [SyntaxFactory.ParseTypeName($"I{entity.Name}Service")],
                            members: [..GenerateConcreteMethods(entity, dtos)]
                        )
                    ]
                )
            );

            results.Add(AddFile(folderPathAbstract, $"I{entity.Name}Service", code_abstract.ToFullString()));
            results.Add(AddFile(folderPathConcrete, $"{entity.Name}Service", code_concrete.ToFullString()));
        }

        if (_appSetting.IsThereIdentiy)
        {
            var code_IAuthService = CompilationUnit(
                usings: [
                    $"{_appSetting.CoreLayerProjectName}.Utils.ResultPattern",
                    $"{_appSetting.ModelLayerProjectName}.Dtos.Auth.Login",
                    $"{_appSetting.ModelLayerProjectName}.Dtos.Auth.Refresh",
                    $"{_appSetting.ModelLayerProjectName}.Dtos.Auth.SignUp",
                ],
                nspace: NamespaceDeclaration(
                    value: $"{_appSetting.BusinessLayerProjectName}.Abstract",
                    members: [
                        InterfaceDeclaration(
                            name: "IAuthService",
                            modifiers: [SyntaxKind.PublicKeyword],
                            members: [
                                MethodDeclaration(
                                    name: "LoginAsync",
                                    returnType: $"Task<Result<LoginResponse>>",
                                    parameters: [
                                        ParameterDeclaration("LoginRequest", "loginRequest", true),
                                        ParameterDeclaration("CancellationToken", "cancellationToken", false)
                                    ],
                                    isThereBody: false
                                ),
                                MethodDeclaration(
                                    name: "SignUpAsync",
                                    returnType: $"Task<Result<SignUpResponse>>",
                                    parameters: [
                                        ParameterDeclaration("SignUpRequest", "signUpRequest", true),
                                        ParameterDeclaration("CancellationToken", "cancellationToken", false)
                                    ],
                                    isThereBody: false
                                ),
                                MethodDeclaration(
                                    name: "RefreshAsync",
                                    returnType: $"Task<Result<RefreshAuthResponse>>",
                                    parameters: [
                                        ParameterDeclaration("RefreshRequest", "refreshAuthRequest", true),
                                        ParameterDeclaration("CancellationToken", "cancellationToken", false)
                                    ],
                                    isThereBody: false
                                )
                            ]
                        )
                    ]
                )
            );



            string code_AuthService = CompilationUnit(
                usings: [
                    $"AutoMapper",
                    $"System.Security.Claims",
                    $"Microsoft.AspNetCore.Identity",
                    $"{_appSetting.CoreLayerProjectName}.Enums",
                    $"{_appSetting.CoreLayerProjectName}.Utils",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Auth",
                    $"{_appSetting.CoreLayerProjectName}.Utils.HttpContextManager",
                    $"{_appSetting.CoreLayerProjectName}.Utils.ResultPattern",
                    $"{_appSetting.CoreLayerProjectName}.Utils.Validation",
                    $"{_appSetting.DataAccessLayerProjectName}.UoW",
                    $"{_appSetting.ModelLayerProjectName}.Auth.Login",
                    $"{_appSetting.ModelLayerProjectName}.Auth.Refresh",
                    $"{_appSetting.ModelLayerProjectName}.Auth.SignUp",
                    $"{_appSetting.ModelLayerProjectName}.Entities",
                    $"{_appSetting.BusinessLayerProjectName}.Abstract",
                    $"{_appSetting.BusinessLayerProjectName}.Utils.TokenService",
                    $"{_appSetting.ModelLayerProjectName}.Dtos.{_appSetting.GetIdentityModelTypeNames().}.Commands",

                ]    
            );

            results.Add(AddFile(folderPathAbstract, "IAuthService", code_IAuthService.ToFullString()));
            results.Add(AddFile(folderPathConcrete, "AuthService", code_AuthService));
        }

        return string.Join("\n", results);
    }

    public string GenerateServiceRegistrations()
    {
        var entities = _entityRepository.GetAll(f => f.Control == false);

        #region Usings
        List<string> usings = new()
        {
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.Configuration",
            $"{_appSetting.BusinessLayerProjectName}.Abstract",
            $"{_appSetting.BusinessLayerProjectName}.Concrete"
        };
        if (_appSetting.IsThereIdentiy)
        {
            usings.Add($"{_appSetting.BusinessLayerProjectName}.Utils.TokenService");
        }
        #endregion

        #region Body
        StringBuilder sbBody = new();
        if (_appSetting.IsThereIdentiy)
        {
            sbBody.AppendLine("services.AddSingleton<ITokenService, TokenService>();");
            sbBody.AppendLine("services.AddScoped<IAuthService, AuthService>();");
            sbBody.AppendLine();
        }
        sbBody.AppendLine("#region ENTITY SERVICES");
        foreach (var entity in entities)
        {
            sbBody.AppendLine($"services.AddScoped<I{entity.Name}Service, {entity.Name}Service>();");
        }
        sbBody.AppendLine("#endregion");
        sbBody.AppendLine();
        sbBody.AppendLine("return services;"); 
        #endregion

        var code = CompilationUnit(
            usings: [..usings],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.BusinessLayerProjectName}",
                members: [
                    ClassDeclaration(
                        name: "ServiceRegistration",
                        modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
                        members: [
                            MethodDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
                                name: "AddBusinessServices",
                                returnType: "IServiceCollection",
                                parameters: [
                                    ParameterDeclaration("IServiceCollection", "services", true, [SyntaxKind.ThisKeyword]),
                                    ParameterDeclaration("IConfiguration", "configuration", true)
                                ],
                                body: sbBody.ToString()
                            )
                        ]
                    )
                ]
            )
        );
         
        string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.BusinessLayerProjectName); 
        return AddFile(folderPath, "ServiceRegistration", code.ToFullString());
    }

    #region Service Methods
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

        if (entity.SoftDeletable)
        {
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
        if (createDto != null)
        {
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

        #region DELETE
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        if (deleteDto != null)
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "DeleteAsync",
                returnType: $"Task<Result>",
                parameters:
                [
                    ParameterDeclaration(deleteDto.Name, "request", true) ,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var validationResult = await _validationService.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                        return Result.Validation(validationResult.Failures, description: $""Validation failed for {deleteDto!.Name}"");
                
                    await _unitOfWork.{entity.Name.Pluralize()}.DeleteAndSaveAsync({entity.WhereRule(uniqueFields, "request")}, cancellationToken);
                    return Result.Success();
                "
            ));
        }
        else
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "DeleteAsync",
                returnType: $"Task<Result>",
                parameters:
                [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    await _unitOfWork.{entity.Name.Pluralize()}.DeleteAndSaveAsync({entity.WhereRule(uniqueFields)}, cancellationToken);
                    return Result.Success();
                "
            ));
        }
        #endregion

        #region RESTORE
        if (entity.SoftDeletable)
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "RestoreAsync",
                returnType: $"Task<Result>",
                parameters: [
                    ..uniqueFieldParameters,
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    await _unitOfWork.{entity.Name.Pluralize()}.RestoreAndSaveAsync({entity.WhereRule(uniqueFields)}, cancellationToken);
                    return Result.Success();
                "
            ));
        }
        #endregion

        #region PAGINATION
        if (reportDto != null)
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "PaginationAsync",
                returnType: $"Task<Result<PaginationResponse<{reportDto.Name}>>>",
                parameters: [
                    ParameterDeclaration("DynamicPaginationRequest", "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.PaginationAsync<{reportDto.Name}>(
                        configurationProvider: _mapper.ConfigurationProvider,
                        paginationRequest: request,
                        {entity.IncludeRule(reportDto, _dtoFieldRepository)},
                        cancellationToken: cancellationToken
                    );
                    return Result<PaginationResponse<{reportDto.Name}>>.Success(result);
                "
            ));
        }
        else
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "PaginationAsync",
                returnType: $"Task<Result<PaginationResponse<{entity.Name}>>>",
                parameters: [
                    ParameterDeclaration("DynamicPaginationRequest", "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.PaginationAsync(
                        paginationRequest: request,
                        cancellationToken: cancellationToken
                    );
                    return Result<PaginationResponse<{entity.Name}>>.Success(result);
                "
            ));
        }
        #endregion

        #region DATATABLE
        if (reportDto != null)
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "DatatableClientSideAsync",
                returnType: $"Task<Result<DatatableResponseClientSide<{reportDto.Name}>>>",
                parameters: [
                    ParameterDeclaration("DynamicDatatableRequest", "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.DatatableClientSideAsync<{reportDto.Name}>(
                        configurationProvider: _mapper.ConfigurationProvider,
                        datatableRequest: request,
                        {entity.IncludeRule(reportDto, _dtoFieldRepository)},
                        cancellationToken: cancellationToken
                    );
                    return Result<DatatableResponseClientSide<{reportDto.Name}>>.Success(result);
                "
            ));
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "DatatableServerSideAsync",
                returnType: $"Task<Result<DatatableResponseServerSide<{reportDto.Name}>>>",
                parameters: [
                    ParameterDeclaration("DynamicDatatableRequest", "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.DatatableServerSideAsync<{reportDto.Name}>(
                        configurationProvider: _mapper.ConfigurationProvider,
                        datatableRequest: request,
                        {entity.IncludeRule(reportDto, _dtoFieldRepository)},
                        cancellationToken: cancellationToken
                    );
                    return Result<DatatableResponseServerSide<{reportDto.Name}>>.Success(result);
                "
            ));
        }
        else
        {
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "DatatableClientSideAsync",
                returnType: $"Task<Result<DatatableResponseClientSide<{entity.Name}>>>",
                parameters: [
                    ParameterDeclaration("DynamicDatatableRequest", "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.DatatableClientSideAsync(
                        datatableRequest: request,
                        cancellationToken: cancellationToken
                    );
                    return Result<DatatableResponseClientSide<{entity.Name}>>.Success(result);
                "
            ));
            methods.Add(MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "DatatableServerSideAsync",
                returnType: $"Task<Result<DatatableResponseServerSide<{entity.Name}>>>",
                parameters: [
                    ParameterDeclaration("DynamicDatatableRequest", "request", true),
                    ParameterDeclaration("CancellationToken", "cancellationToken", false)
                ],
                body: $@"
                    var result = await _unitOfWork.{entity.Name.Pluralize()}.DatatableServerSideAsync(
                        datatableRequest: request,
                        cancellationToken: cancellationToken
                    );
                    return Result<DatatableResponseServerSide<{entity.Name}>>.Success(result);
                "
            ));
        }
        #endregion

        return methods;
    } 
    #endregion
}