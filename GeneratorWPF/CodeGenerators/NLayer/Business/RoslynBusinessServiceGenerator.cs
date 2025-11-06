using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Text;


namespace GeneratorWPF.CodeGenerators.NLayer.Business;

public partial class RoslynBusinessServiceGenerator
{
    private readonly AppSetting _appSetting;
    private readonly DtoRepository _dtoRepository;
    private readonly DtoFieldRepository _dtoFieldRepository;
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly DtoFieldRelationsRepository _dtoFieldRelationsRepository;
    public RoslynBusinessServiceGenerator(AppSetting appSetting)
    {
        _appSetting = appSetting;
        _entityRepository = new EntityRepository();
        _dtoRepository = new DtoRepository();
        _dtoFieldRepository = new DtoFieldRepository();
        _fieldRepository = new FieldRepository();
        _dtoFieldRelationsRepository = new DtoFieldRelationsRepository();
    }

    public string GeneraterAbstract(Entity entity, List<Dto> dtos)
    {
        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).ToList();

        // 1) Method List
        var methodList = new List<MethodDeclarationSyntax>();

        #region Get Entity
        methodList.Add(GeneratorGetEntityMethodOfAbstract(entity.Name));
        methodList.Add(GeneratorGetEntityListMethodOfAbstract(entity.Name));
        #endregion

        #region Get Generic
        methodList.Add(GeneratorGetGenericMethodOfAbstract(entity.Name));
        methodList.Add(GeneratorGetGenericListMethodOfAbstract(entity.Name));
        #endregion

        #region Get Select List
        bool isThereOneUnique = entity.Fields.Count(f => f.IsUnique) == 1;
        if (isThereOneUnique)
        {
            Field? textField = entity.GetSelectListTextField();
            if (textField != default)
            {
                methodList.Add(GeneratorGetSelectListMethodOfAbstract(entity.Name));
            }
        }
        #endregion

        #region Get
        methodList.Add(GeneratorGetMethodByEntityOfAbstract(entity.Name, "GetAsync", uniqueFields));
        methodList.Add(GeneratorGetAllMethodByEntityOfAbstract(entity.Name, "GetAllAsync"));
        methodList.Add(GeneratorGetListMethodByEntityOfAbstract(entity.Name, "GetListAsync"));
        #endregion

        #region GetBasic
        var basicResponseDto = dtos.FirstOrDefault(f => f.Id == entity.BasicResponseDtoId);
        bool isThereBasicResponseDto = basicResponseDto != null;
        if (isThereBasicResponseDto)
        {
            methodList.Add(GeneratorGetMethodByDtoOfAbstract(basicResponseDto!.Name, "GetByBasicAsync", uniqueFields));
            methodList.Add(GeneratorGetAllMethodByDtoOfAbstract(basicResponseDto.Name, "GetAllByBasicAsync"));
            methodList.Add(GeneratorGetListMethodByDtoOfAbstract(basicResponseDto.Name, "GetListByBasicAsync"));
        }
        #endregion

        #region GetDetail
        var detailResponseDto = dtos.FirstOrDefault(f => f.Id == entity.DetailResponseDtoId);
        if (detailResponseDto != null)
        {
            methodList.Add(GeneratorGetMethodByDtoOfAbstract(detailResponseDto!.Name, "GetByDetailAsync", uniqueFields));
            methodList.Add(GeneratorGetAllMethodByDtoOfAbstract(detailResponseDto.Name, "GetAllByDetailAsync"));
            methodList.Add(GeneratorGetListMethodByDtoOfAbstract(detailResponseDto.Name, "GetListByDetailAsync"));
        }
        #endregion

        #region Other Read Dtos
        var readResponseDtos = dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read && f.Id != entity.BasicResponseDtoId && f.Id != entity.DetailResponseDtoId);
        if (readResponseDtos != null && readResponseDtos.Any())
        {
            foreach (var readDto in readResponseDtos)
            {
                methodList.Add(GeneratorGetMethodByDtoOfAbstract(readDto!.Name, $"Get{readDto.Name}Async", uniqueFields));
                methodList.Add(GeneratorGetAllMethodByDtoOfAbstract(readDto.Name, $"GetAll{readDto.Name}Async"));
                methodList.Add(GeneratorGetListMethodByDtoOfAbstract(readDto.Name, $"GetList{readDto.Name}Async"));
            }
        }
        #endregion

        #region Create
        var createDto = dtos.FirstOrDefault(f => f.Id == entity.CreateDtoId);

        string createArgType = createDto != null ? createDto.Name : entity.Name;
        string createReturnType = isThereBasicResponseDto ? basicResponseDto!.Name : entity.Name;

        methodList.Add(GeneratorCreateMethodOfAbstract(createReturnType, createArgType));
        #endregion

        #region Update
        var updateDto = dtos.FirstOrDefault(f => f.Id == entity.UpdateDtoId);

        string updateArgType = updateDto != null ? updateDto.Name : entity.Name;
        string updateReturnType = isThereBasicResponseDto ? basicResponseDto!.Name : entity.Name;

        methodList.Add(GeneratorUpdateMethodOfAbstract(updateReturnType, updateArgType));
        #endregion

        #region Delete
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        bool isThereDeleteDto = deleteDto != null;

        string deleteArgType = isThereDeleteDto ? deleteDto!.Name : string.Empty;

        methodList.Add(GeneratorDeleteMethodOfAbstract(deleteArgType, isThereDeleteDto, uniqueFields));
        #endregion

        #region UndoDelete
        if(entity.SoftDeletable == true)
        {
            methodList.Add(GeneratorUndoDeleteMethodOfAbstract(entity.Name, uniqueFields));
        }
        #endregion

        #region Datatable Methods
        methodList.Add(GeneratorDatatableClientSideMethodOfAbstract(entity.Name));
        methodList.Add(GeneratorDatatableServerSideMethodOfAbstract(entity.Name));
        Dto? reportDto = entity.ReportDtoId != default ? _dtoRepository.Get(f => f.Id == entity.ReportDtoId) : default;
        if (reportDto != default)
        {
            methodList.Add(GeneratorDatatableClientSideByDtoOfAbstract(reportDto.Name, "DatatableClientSideByReportAsync"));
            methodList.Add(GeneratorDatatableServerSideByDtoOfAbstract(reportDto.Name, "DatatableServerSideByReportAsync"));
        }
        #endregion

        // 2) Interface
        var InterfaceDeclaration = SyntaxFactory
            .InterfaceDeclaration($"I{entity.Name}Service")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"IServiceBase<{entity.Name}>")),
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"IServiceBaseAsync<{entity.Name}>")))
            .AddMembers(methodList.ToArray());

        // 3) NameSpace
        var namespaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName("Business.Abstract"))
            .AddMembers(InterfaceDeclaration);

        // 4) Compilation Unit
        var usings = new List<UsingDirectiveSyntax>()
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc.Rendering")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq.Expressions")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.BaseRequestModels")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Business.ServiceBase")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.Datatable")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.Pagination")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Entities")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Model"))
        };
        if (dtos != null && dtos.Any())
        {
            usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"Model.Dtos.{entity.Name}_")));
        }

        var compilationUnit = SyntaxFactory
            .CompilationUnit()
            .AddUsings(usings.ToArray())
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();

        return compilationUnit.ToFullString();
    }

    public string GeneraterConcrete(Entity entity, List<Dto> dtos)
    {
        string serviceName = $"{entity.Name}Service";
        string repoTypeName = $"I{entity.Name}Repository";
        string repoArgName = $"{entity.Name}Repository".ToCamelCase();

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).ToList();

        // 1) Attribute List
        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ExceptionHandler"))
            )
        );

        // 2) Method List
        var methodList = new List<MethodDeclarationSyntax>();

        #region Get Entity
        methodList.Add(GeneratorGetEntityMethodOfConcrete(entity.Name));
        methodList.Add(GeneratorGetEntityListMethodOfConcrete(entity.Name));
        #endregion

        #region Get Generic
        methodList.Add(GeneratorGetGenericMethodOfConcrete(entity.Name));
        methodList.Add(GeneratorGetGenericListMethodOfConcrete(entity.Name));
        #endregion

        #region Get Select List
        bool isThereOneUnique = entity.Fields.Count(f => f.IsUnique) == 1;
        if (isThereOneUnique)
        {
            Field? textField = entity.GetSelectListTextField();

            if (textField != default)
            {
                Field uniqueField = entity.Fields.First(f => f.IsUnique);
                methodList.Add(GeneratorGetSelectListMethodOfConcrete(entity.Name, uniqueField.Name, textField.Name));
            }
        }
        #endregion

        #region Get
        methodList.Add(GeneratorGetMethodByEntityOfConcrete(entity.Name, "GetAsync", uniqueFields));
        methodList.Add(GeneratorGetAllMethodByEntityOfConcrete(entity.Name, "GetAllAsync"));
        methodList.Add(GeneratorGetListMethodByEntityOfConcrete(entity.Name, "GetListAsync"));
        #endregion

        #region GetBasic
        var basicResponseDto = dtos.FirstOrDefault(f => f.Id == entity.BasicResponseDtoId);
        bool isThereBasicResponseDto = basicResponseDto != null;
        if (isThereBasicResponseDto)
        {
            var dtoFieldIdsOfBasicResponse = basicResponseDto!.DtoFields.Select(f => f.Id).ToList();
            //var isThereIncludeOfBasicResponse = _dtoFieldRelationsRepository.IsExist(f => dtoFieldIdsOfBasicResponse.Contains(f.DtoFieldId)); // efcheck
            var isThereIncludeOfBasicResponse = _dtoFieldRelationsRepository.GetAll().Any(f => dtoFieldIdsOfBasicResponse.Contains(f.DtoFieldId));

            methodList.Add(GeneratorGetMethodByDtoOfConcrete("GetByBasicAsync", basicResponseDto!, uniqueFields, isThereIncludeOfBasicResponse));
            methodList.Add(GeneratorGetAllMethodByDtoOfConcrete("GetAllByBasicAsync", basicResponseDto, isThereIncludeOfBasicResponse));
            methodList.Add(GeneratorGetListMethodByDtoOfConcrete("GetListByBasicAsync", basicResponseDto, isThereIncludeOfBasicResponse));
        }
        #endregion

        #region GetDetail
        var detailResponseDto = dtos.FirstOrDefault(f => f.Id == entity.DetailResponseDtoId);
        if (detailResponseDto != null)
        {
            var dtoFieldIdsOfDetailResponse = detailResponseDto.DtoFields.Select(f => f.Id).ToList();
            //var isThereIncludeOfDetailResponse = _dtoFieldRelationsRepository.IsExist(f => dtoFieldIdsOfDetailResponse.Contains(f.DtoFieldId)); // efcheck
            var isThereIncludeOfDetailResponse = _dtoFieldRelationsRepository.GetAll().Any(f => dtoFieldIdsOfDetailResponse.Contains(f.DtoFieldId));

            methodList.Add(GeneratorGetMethodByDtoOfConcrete("GetByDetailAsync", detailResponseDto!, uniqueFields, isThereIncludeOfDetailResponse));
            methodList.Add(GeneratorGetAllMethodByDtoOfConcrete("GetAllByDetailAsync", detailResponseDto, isThereIncludeOfDetailResponse));
            methodList.Add(GeneratorGetListMethodByDtoOfConcrete("GetListByDetailAsync", detailResponseDto, isThereIncludeOfDetailResponse));
        }
        #endregion

        #region Other Read Dtos
        var readResponseDtos = dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read && f.Id != entity.BasicResponseDtoId && f.Id != entity.DetailResponseDtoId);
        if (readResponseDtos != null && readResponseDtos.Any())
        {
            foreach (var readDto in readResponseDtos)
            {
                var dtoFieldIdsOfReadDto = readDto.DtoFields.Select(f => f.Id).ToList();
                //var isThereIncludeOfReadDto = _dtoFieldRelationsRepository.IsExist(f => dtoFieldIdsOfReadDto.Contains(f.DtoFieldId)); // efcheck
                var isThereIncludeOfReadDto = _dtoFieldRelationsRepository.GetAll().Any(f => dtoFieldIdsOfReadDto.Contains(f.DtoFieldId));

                methodList.Add(GeneratorGetMethodByDtoOfConcrete($"Get{readDto.Name}Async", readDto!, uniqueFields, isThereIncludeOfReadDto));
                methodList.Add(GeneratorGetAllMethodByDtoOfConcrete($"GetAll{readDto.Name}Async", readDto, isThereIncludeOfReadDto));
                methodList.Add(GeneratorGetListMethodByDtoOfConcrete($"GetList{readDto.Name}Async", readDto, isThereIncludeOfReadDto));
            }
        }
        #endregion

        #region Create
        methodList.Add(GeneratorCreateMethodOfConcrete(entity, dtos));
        #endregion

        #region Update
        methodList.Add(GeneratorUpdateMethodOfConcrete(entity, dtos, uniqueFields));
        #endregion

        #region Delete
        methodList.Add(GeneratorDeleteMethodOfConcrete(entity, dtos, uniqueFields));
        #endregion

        #region UndoDelete
        if (entity.SoftDeletable == true)
        {
            methodList.Add(GeneratorUndoDeleteMethodOfConcrete(entity, uniqueFields));
        }
        #endregion

        #region Datatable Methods
        methodList.Add(GeneratorDatatableClientSideMethodOfConcrete(entity));
        methodList.Add(GeneratorDatatableServerSideMethodOfConcrete(entity));
        Dto? reportDto = entity.ReportDtoId != default ? _dtoRepository.Get(f => f.Id == entity.ReportDtoId, include: i => i.Include(x => x.DtoFields)) : default;
        if (reportDto != default)
        {
            var dtoFieldIdsOfDetailResponse = reportDto.DtoFields.Select(f => f.Id).ToList();
            //var isThereIncludeOfReportDto = _dtoFieldRelationsRepository.IsExist(f => dtoFieldIdsOfDetailResponse.Contains(f.DtoFieldId)); // efcheck
            var isThereIncludeOfReportDto = _dtoFieldRelationsRepository.GetAll().Any(f => dtoFieldIdsOfDetailResponse.Contains(f.DtoFieldId));

            methodList.Add(GeneratorDatatableClientSideByDtoOfConcrete("DatatableClientSideByReportAsync", reportDto, isThereIncludeOfReportDto));
            methodList.Add(GeneratorDatatableServerSideByDtoOfConcrete("DatatableServerSideByReportAsync", reportDto, isThereIncludeOfReportDto));
        }
        #endregion


        // 3) Constructor
        var constructor = SyntaxFactory.ConstructorDeclaration(serviceName)
             .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
             .AddParameterListParameters(
                 SyntaxFactory.Parameter(SyntaxFactory.Identifier(repoArgName)).WithType(SyntaxFactory.ParseTypeName(repoTypeName)),
                 SyntaxFactory.Parameter(SyntaxFactory.Identifier("mapper")).WithType(SyntaxFactory.ParseTypeName("IMapper"))
             )
             .WithInitializer(
                 SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
                     .AddArgumentListArguments(
                         SyntaxFactory.Argument(SyntaxFactory.IdentifierName(repoArgName)),
                         SyntaxFactory.Argument(SyntaxFactory.IdentifierName("mapper")))
             )
             .WithBody(SyntaxFactory.Block());

        // 4) Class
        var classDeclaration = SyntaxFactory
            .ClassDeclaration(serviceName)
            .AddAttributeLists(attributeList)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"ServiceBase<{entity.Name}, {repoTypeName}>")),
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"I{serviceName}"))
            )
            .AddMembers(constructor)
            .AddMembers(methodList.ToArray());

        // 5) NameSpace
        var namespaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName("Business.Concrete"))
            .AddMembers(classDeclaration);

        // 6) Compilation Unit
        var usings = new List<UsingDirectiveSyntax>()
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("AutoMapper")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Business.Abstract")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Business.ServiceBase")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.BaseRequestModels")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Model")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.CrossCuttingConcerns")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.Datatable")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.Pagination")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("DataAccess.Abstract")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc.Rendering")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.EntityFrameworkCore")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Entities")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq.Expressions"))
        };
        if (dtos != null && dtos.Any())
        {
            usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"Model.Dtos.{entity.Name}_")));
        }

        var compilationUnit = SyntaxFactory
            .CompilationUnit()
            .AddUsings(usings.ToArray())
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();

        return compilationUnit.ToFullString();
    }

    public string GeneraterAuthServiceConcrete()
    {
        var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);
        string IdentityUserType = identityTypeConfigs.IdentityUserType; 

        // response user model for login, signup
        Entity? userEntity = _appSetting.IsThereUser ? _entityRepository.Get(
                f => f.Id == _appSetting.UserEntityId, 
                include: i => i.Include(x => x.Fields).ThenInclude(ti => ti.FieldType).Include(x => x.Dtos)) : null;
        
        bool isThereUserDtoForResponse = false;
        string userModelType = IdentityUserType;
        if (userEntity != null)
        {
            if (userEntity.Dtos != null)
            {
                var baseRespDto = userEntity.BasicResponseDtoId != default ? userEntity.Dtos.FirstOrDefault(f => f.Id == userEntity.BasicResponseDtoId) : default;
                var detailRespDto = userEntity.DetailResponseDtoId != default ? userEntity.Dtos.FirstOrDefault(f => f.Id == userEntity.DetailResponseDtoId) : default;
                var firstReadDto = userEntity.Dtos.FirstOrDefault(f => f.CrudTypeId == (int)CrudTypeEnums.Read);

                if (baseRespDto != default)
                {
                    isThereUserDtoForResponse = true;
                    userModelType = baseRespDto.Name;
                }
                else if (detailRespDto != default)
                {
                    isThereUserDtoForResponse = true;
                    userModelType = detailRespDto.Name;
                }
                else if (firstReadDto != default)
                {
                    isThereUserDtoForResponse = true;
                    userModelType = firstReadDto.Name;
                }
            }
        }


        // 1) Field List
        var fieldList = new List<FieldDeclarationSyntax>
        {
            CreatePrivateField("IUnitOfWork", "_unitOfWork", isReadOnly: true),
            CreatePrivateField("ITokenService", "_tokenService", isReadOnly: true),
            CreatePrivateField($"UserManager<{IdentityUserType}>", "_userManager", isReadOnly: true),
            CreatePrivateField($"SignInManager<{IdentityUserType}>", "_signInManager", isReadOnly: true),
            CreatePrivateField("HttpContextManager", "_httpContextManager", isReadOnly: true),
            CreatePrivateField("IMapper", "_mapper", isReadOnly: true),
        };


        // 2) Constructor
        var parameterList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("unitOfWork")).WithType(SyntaxFactory.ParseTypeName("IUnitOfWork")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("tokenService")).WithType(SyntaxFactory.ParseTypeName("ITokenService")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("userManager")).WithType(SyntaxFactory.ParseTypeName($"UserManager<{IdentityUserType}>")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("signInManager")).WithType(SyntaxFactory.ParseTypeName($"SignInManager<{IdentityUserType}>")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("httpContextManager")).WithType(SyntaxFactory.ParseTypeName("HttpContextManager")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("mapper")).WithType(SyntaxFactory.ParseTypeName("IMapper")),
        };
        var statementList = new List<StatementSyntax>()
        {
            SyntaxFactory.ParseStatement("_unitOfWork = unitOfWork;"),
            SyntaxFactory.ParseStatement("_tokenService = tokenService;"),
            SyntaxFactory.ParseStatement("_userManager = userManager;"),
            SyntaxFactory.ParseStatement("_signInManager = signInManager;"),
            SyntaxFactory.ParseStatement("_httpContextManager = httpContextManager;"),
            SyntaxFactory.ParseStatement("_mapper = mapper;"),
        };

        var constructor = SyntaxFactory.ConstructorDeclaration("AuthService")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(parameterList.ToArray())
            .WithBody(SyntaxFactory.Block(statementList.ToArray()));


        // 1) Method List
        var methodList = new List<MethodDeclarationSyntax>()
        {
            GeneratorLoginAsync(userModelType, isThereUserDtoForResponse),
            GeneratorSignUpAsync(userModelType, isThereUserDtoForResponse, IdentityUserType),
            GeneratorRefreshAuthAsync(userModelType, isThereUserDtoForResponse),
            GeneratorLoginWebBaseAsync(IdentityUserType, isThereUserDtoForResponse),
            GeneratorSignUpWebBaseAsync(IdentityUserType, isThereUserDtoForResponse, IdentityUserType),
            GeneratorGetClaimsAsync(IdentityUserType, isThereUserDtoForResponse)
        };


        // 4) Class
        var classDeclaration = SyntaxFactory
            .ClassDeclaration("AuthService")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IAuthService")))
            .AddMembers(fieldList.ToArray())
            .AddMembers(constructor)
            .AddMembers(methodList.ToArray());

        // 5) NameSpace
        var namespaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName("Business.Concrete"))
            .AddMembers(classDeclaration);

        // 6) Compilation Unit
        var usings = new List<UsingDirectiveSyntax>()
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("AutoMapper")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Business.Abstract")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Business.Utils.TokenService")),

            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.Auth")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.CrossCuttingConcerns")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.ExceptionHandle.Exceptions")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.HttpContextManager")),

            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("DataAccess.UoW")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Identity")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Auth.Login")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Auth.RefreshAuth")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Auth.SignUp")),

            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Entities")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Security.Claims")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Security.Cryptography"))
        };
        if (userEntity != null && userEntity.Dtos != null && userEntity.Dtos.Any())
        {
            usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"Model.Dtos.{userEntity.Name}_")));
        }

        var compilationUnit = SyntaxFactory
            .CompilationUnit()
            .AddUsings(usings.ToArray())
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();

        return compilationUnit.ToFullString();
    }


    private MethodDeclarationSyntax GeneratorLoginAsync(string userModelType, bool isThereUserDtoForResponse)
    {
        var sb = new StringBuilder();
        sb.AppendLine("public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default)");
        sb.AppendLine("{");
        sb.AppendLine("    User? user = await _userManager.FindByEmailAsync(loginRequest.Email);");
        sb.AppendLine("    if (user == null)");
        sb.AppendLine("        throw new BusinessException(\"The email address is not exist.\", description: $\"Requester email address: {loginRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("    bool isPasswordValid = await _userManager.CheckPasswordAsync(user, loginRequest.Password);");
        sb.AppendLine("    if (!isPasswordValid)");
        sb.AppendLine("        throw new BusinessException(\"Password does not correct.\", description: $\"Requester email address: {loginRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("    IList<string> roles = await _userManager.GetRolesAsync(user);");
        sb.AppendLine("    IList<Claim> claims = await GetClaimsAsync(user, roles);");
        sb.AppendLine("    AccessToken accessToken = _tokenService.GenerateAccessToken(claims);");
        sb.AppendLine("    RefreshToken refreshToken = _tokenService.GenerateRefreshToken(user);");
        sb.AppendLine();
        sb.AppendLine("    string? ipAddress = _httpContextManager.GetClientIp();");
        sb.AppendLine("    if (string.IsNullOrEmpty(ipAddress))");
        sb.AppendLine("        throw new GeneralException(\"Ip address could not found for login.\", description: $\"Requester email address: {loginRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("    await _unitOfWork.RefreshTokens.DeleteAndSaveAsync(");
        sb.AppendLine("        where: f => f.UserId == user.Id && f.IpAddress.Trim() == ipAddress.Trim(),");
        sb.AppendLine("        cancellationToken);");
        sb.AppendLine("    await _unitOfWork.RefreshTokens.AddAndSaveAsync(refreshToken, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("    if (_httpContextManager.IsMobile())");
        sb.AppendLine("    {");
        sb.AppendLine("        return new LoginTrustedResponse");
        sb.AppendLine("        {");
        sb.AppendLine("            AccessToken = accessToken,");
        sb.AppendLine("            RefreshToken = refreshToken.Token,");
        if (isThereUserDtoForResponse)
        {
            sb.AppendLine($"\t\t\tUser = _mapper.Map<{userModelType}>(user),");
        }
        else
        {
            sb.AppendLine($"\t\t\tUser = user");
        }
        sb.AppendLine("            Roles = roles");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("    else");
        sb.AppendLine("    {");
        sb.AppendLine("        _httpContextManager.AddRefreshTokenToCookie(refreshToken.Token, refreshToken.ExpirationUtc);");
        sb.AppendLine("        return new LoginResponse");
        sb.AppendLine("        {");
        sb.AppendLine("            AccessToken = accessToken,");
        if (isThereUserDtoForResponse)
        {
            sb.AppendLine($"\t\t\tUser = _mapper.Map<{userModelType}>(user),");
        }
        else
        {
            sb.AppendLine($"\t\t\tUser = user");
        }
        sb.AppendLine("            Roles = roles");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var method = SyntaxFactory.ParseMemberDeclaration(sb.ToString()) as MethodDeclarationSyntax;
        if (method == null) throw new InvalidOperationException("Method parse failed.");


        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
             SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("Validation"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName("LoginRequest"))
                        )
                    )
                )
            )
        };

        method = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)));
        return method;
    }

    private MethodDeclarationSyntax GeneratorSignUpAsync(string userModelType, bool isThereUserDtoForResponse, string identityUserType)
    {
        var sb = new StringBuilder();

        sb.AppendLine("public async Task<SignUpResponse> SignUpAsync(SignUpRequest signUpRequest, CancellationToken cancellationToken = default)");
        sb.AppendLine("{");
        sb.AppendLine("    try");
        sb.AppendLine("    {");
        sb.AppendLine("        await _unitOfWork.BeginTransactionAsync(cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        var userExist = await _userManager.FindByEmailAsync(signUpRequest.Email);");
        sb.AppendLine("        if (userExist != null)");
        sb.AppendLine("            throw new BusinessException(\"The email address is already in use.\", description: $\"Requester email address: {signUpRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine($"        var user = _mapper.Map<{identityUserType}>(signUpRequest);");
        sb.AppendLine("        user.UserName = $\"{signUpRequest.Email}_{DateTime.UtcNow:yyyyMMddHHmmss}\";");
        sb.AppendLine();
        sb.AppendLine("        var result = await _userManager.CreateAsync(user, signUpRequest.Password);");
        sb.AppendLine("        if (!result.Succeeded)");
        sb.AppendLine("            throw new GeneralException(string.Join(\"\\n\", result.Errors.Select(e => e.Description)), description: $\"User cannot be created. Requester email: {signUpRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("        var roleResult = await _userManager.AddToRoleAsync(user, \"User\");");
        sb.AppendLine("        if (!roleResult.Succeeded)");
        sb.AppendLine("            throw new GeneralException(\"Failed to assign role.\", description: $\"Requester email address: {signUpRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("        IList<string> roles = await _userManager.GetRolesAsync(user);");
        sb.AppendLine("        IList<Claim> claims = await GetClaimsAsync(user, roles);");
        sb.AppendLine("        AccessToken accessToken = _tokenService.GenerateAccessToken(claims);");
        sb.AppendLine("        RefreshToken refreshToken = _tokenService.GenerateRefreshToken(user);");
        sb.AppendLine();
        sb.AppendLine("        await _unitOfWork.RefreshTokens.AddAndSaveAsync(refreshToken, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        await _unitOfWork.CommitTransactionAsync(cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        if (_httpContextManager.IsMobile())");
        sb.AppendLine("        {");
        sb.AppendLine("            return new SignUpTrustedResponse");
        sb.AppendLine("            {");
        sb.AppendLine("                AccessToken = accessToken,");
        sb.AppendLine("                RefreshToken = refreshToken.Token,");
        if (isThereUserDtoForResponse)
        {
            sb.AppendLine($"\t\t\tUser = _mapper.Map<{userModelType}>(user),");
        }
        else
        {
            sb.AppendLine($"\t\t\tUser = user");
        }
        sb.AppendLine("                Roles = roles");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            _httpContextManager.AddRefreshTokenToCookie(refreshToken.Token, refreshToken.ExpirationUtc);");
        sb.AppendLine("            return new SignUpResponse");
        sb.AppendLine("            {");
        sb.AppendLine("                AccessToken = accessToken,");
        if (isThereUserDtoForResponse)
        {
            sb.AppendLine($"\t\t\tUser = _mapper.Map<{userModelType}>(user),");
        }
        else
        {
            sb.AppendLine($"\t\t\tUser = user");
        }
        sb.AppendLine("                Roles = roles");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    catch (Exception)");
        sb.AppendLine("    {");
        sb.AppendLine("        await _unitOfWork.RollbackTransactionAsync(cancellationToken);");
        sb.AppendLine("        throw;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var method = SyntaxFactory.ParseMemberDeclaration(sb.ToString()) as MethodDeclarationSyntax;
        if (method == null) throw new InvalidOperationException("Method parse failed.");

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
             SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("Validation"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName("SignUpRequest"))
                        )
                    )
                )
            )
        };

        method = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)));
        return method;
    }

    private MethodDeclarationSyntax GeneratorRefreshAuthAsync(string userModelType, bool isThereUserDtoForResponse)
    {
        var sb = new StringBuilder();

        sb.AppendLine("public async Task<RefreshAuthResponse> RefreshAuthAsync(RefreshAuthRequest refreshAuthRequest, CancellationToken cancellationToken = default)");
        sb.AppendLine("{");
        sb.AppendLine("    try");
        sb.AppendLine("    {");
        sb.AppendLine("        await _unitOfWork.BeginTransactionAsync(cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        if (!_httpContextManager.IsMobile())");
        sb.AppendLine("        {");
        sb.AppendLine("            refreshAuthRequest.RefreshToken = _httpContextManager.GetRefreshTokenFromCookie();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var user = await _unitOfWork.Users.GetAsync(");
        sb.AppendLine("            where: f => f.Id == refreshAuthRequest.UserId,");
        sb.AppendLine("            cancellationToken: cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        if (user == null)");
        sb.AppendLine("            throw new GeneralException(\"User cannot found for refresh auth!\", description: $\"Requester userId: {refreshAuthRequest.UserId}\");");
        sb.AppendLine();
        sb.AppendLine("        string? ipAddress = _httpContextManager.GetClientIp();");
        sb.AppendLine("        if (string.IsNullOrEmpty(ipAddress))");
        sb.AppendLine("            throw new GeneralException(\"Ip address could not readed for refresh auth.\");");
        sb.AppendLine();
        sb.AppendLine("        DateTime nowOnUtc = DateTime.UtcNow;");
        sb.AppendLine("        ICollection<RefreshToken>? refreshTokens = await _unitOfWork.RefreshTokens.GetAllAsync(");
        sb.AppendLine("            where: f =>");
        sb.AppendLine("                f.UserId == refreshAuthRequest.UserId &&");
        sb.AppendLine("                f.TTL > 0 &&");
        sb.AppendLine("                f.ExpirationUtc > nowOnUtc &&");
        sb.AppendLine("                f.IpAddress.ToLowerInvariant().Trim() == ipAddress.ToLowerInvariant().Trim(),");
        sb.AppendLine("            cancellationToken: cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        if (refreshTokens == null)");
        sb.AppendLine("            throw new GeneralException(\"There is no available refresh token.\");");
        sb.AppendLine();
        sb.AppendLine("        RefreshToken? refreshToken = refreshTokens.FirstOrDefault(f => f.Token.Trim() == refreshAuthRequest.RefreshToken);");
        sb.AppendLine("        if (refreshToken == null)");
        sb.AppendLine("            throw new GeneralException(\"There is no available refresh token.\");");
        sb.AppendLine();
        sb.AppendLine("        refreshToken.Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));");
        sb.AppendLine("        refreshToken.TTL -= 1;");
        sb.AppendLine();
        sb.AppendLine("        await _unitOfWork.RefreshTokens.DeleteAndSaveAsync(");
        sb.AppendLine("            where: f => f.Id != refreshToken.Id && f.UserId == user.Id && f.IpAddress.Trim() == ipAddress.Trim(),");
        sb.AppendLine("            cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        await _unitOfWork.RefreshTokens.UpdateAndSaveAsync(refreshToken, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        IList<string> roles = await _userManager.GetRolesAsync(user);");
        sb.AppendLine("        IList<Claim> claims = await GetClaimsAsync(user, roles);");
        sb.AppendLine("        AccessToken accessToken = _tokenService.GenerateAccessToken(claims);");
        sb.AppendLine();
        sb.AppendLine("        await _unitOfWork.CommitTransactionAsync(cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        if (_httpContextManager.IsMobile())");
        sb.AppendLine("        {");
        sb.AppendLine("            return new RefreshAuthTrustedResponse");
        sb.AppendLine("            {");
        sb.AppendLine("                RefreshToken = refreshToken.Token,");
        sb.AppendLine("                AccessToken = accessToken,");
        if (isThereUserDtoForResponse)
        {
            sb.AppendLine($"\t\t\tUser = _mapper.Map<{userModelType}>(user),");
        }
        else
        {
            sb.AppendLine($"\t\t\tUser = user");
        }
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            _httpContextManager.AddRefreshTokenToCookie(refreshToken.Token, refreshToken.ExpirationUtc);");
        sb.AppendLine("            return new RefreshAuthResponse");
        sb.AppendLine("            {");
        sb.AppendLine("                AccessToken = accessToken,");
        if (isThereUserDtoForResponse)
        {
            sb.AppendLine($"\t\t\tUser = _mapper.Map<{userModelType}>(user),");
        }
        else
        {
            sb.AppendLine($"\t\t\tUser = user");
        }
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    catch (Exception)");
        sb.AppendLine("    {");
        sb.AppendLine("        await _unitOfWork.RollbackTransactionAsync(cancellationToken);");
        sb.AppendLine("        throw;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var method = SyntaxFactory.ParseMemberDeclaration(sb.ToString()) as MethodDeclarationSyntax;
        if (method == null) throw new InvalidOperationException("Method parse failed.");

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ExceptionHandler")),
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("Validation"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName("RefreshAuthRequest"))
                        )
                    )
                )
            )
        };

        method = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)));
        return method;
    }

    private MethodDeclarationSyntax GeneratorLoginWebBaseAsync(string userModelType, bool isThereUserDtoForResponse)
    {
        var sb = new StringBuilder();
        sb.AppendLine("public async Task LoginWebBaseAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default)");
        sb.AppendLine("{");
        sb.AppendLine("    var user = await _userManager.FindByEmailAsync(loginRequest.Email);");
        sb.AppendLine("    if (user == null) throw new BusinessException(\"The email address is not exist.\", description: $\"Requester email address: {loginRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("    var result = await _signInManager.PasswordSignInAsync(user, loginRequest.Password, isPersistent: true, lockoutOnFailure: false);");
        sb.AppendLine();
        sb.AppendLine("    if (result.IsLockedOut)");
        sb.AppendLine("    {");
        sb.AppendLine("        throw new BusinessException(\"Your account is locked.\", description: $\"Requester email address: {loginRequest.Email}\");");
        sb.AppendLine("    }");
        sb.AppendLine("    else if (!result.Succeeded)");
        sb.AppendLine("    {");
        sb.AppendLine("        throw new BusinessException(\"Invalid login information..\", description: $\"Requester email address: {loginRequest.Email}\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var method = SyntaxFactory.ParseMemberDeclaration(sb.ToString()) as MethodDeclarationSyntax;
        if (method == null) throw new InvalidOperationException("Method parse failed.");


        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
             SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("Validation"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName("LoginRequest"))
                        )
                    )
                )
            )
        };

        method = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)));
        return method;
    }

    private MethodDeclarationSyntax GeneratorSignUpWebBaseAsync(string userModelType, bool isThereUserDtoForResponse, string identityUserType)
    {
        var sb = new StringBuilder();

        sb.AppendLine("public async Task SignUpWebBaseAsync(SignUpRequest signUpRequest, CancellationToken cancellationToken = default)");
        sb.AppendLine("{");
        sb.AppendLine("    var userExist = await _userManager.FindByEmailAsync(signUpRequest.Email);");
        sb.AppendLine("    if (userExist != null) throw new BusinessException(\"The email address is already in use.\", description: $\"Requester email address: {signUpRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine($"    var user = _mapper.Map<{identityUserType}>(signUpRequest);");
        sb.AppendLine("    user.UserName = $\"{signUpRequest.Email}_{DateTime.UtcNow:yyyyMMddHHmmss}\";");
        sb.AppendLine();
        sb.AppendLine("    var result = await _userManager.CreateAsync(user, signUpRequest.Password);");
        sb.AppendLine("    if (!result.Succeeded) throw new GeneralException(string.Join(\"\\n\", result.Errors.Select(e => e.Description)), description: $\"User cannot be created. Requester email: {signUpRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("    var roleResult = await _userManager.AddToRoleAsync(user, \"User\");");
        sb.AppendLine("    if (!roleResult.Succeeded) throw new GeneralException(\"Failed to assign role.\", description: $\"Requester email address: {signUpRequest.Email}\");");
        sb.AppendLine();
        sb.AppendLine("    var resultSignIn = await _signInManager.PasswordSignInAsync(user, signUpRequest.Password, isPersistent: true, lockoutOnFailure: false);");
        sb.AppendLine();
        sb.AppendLine("    if (resultSignIn.IsLockedOut)");
        sb.AppendLine("    {");
        sb.AppendLine("        throw new BusinessException(\"Your account is locked.\", description: $\"Requester email address: {signUpRequest.Email}\");");
        sb.AppendLine("    }");
        sb.AppendLine("    else if (!resultSignIn.Succeeded)");
        sb.AppendLine("    {");
        sb.AppendLine("        throw new BusinessException(\"Invalid login information..\", description: $\"Requester email address: {signUpRequest.Email}\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var method = SyntaxFactory.ParseMemberDeclaration(sb.ToString()) as MethodDeclarationSyntax;
        if (method == null) throw new InvalidOperationException("Method parse failed.");

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
             SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("Validation"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName("SignUpRequest"))
                        )
                    )
                )
            )
        };

        method = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)));
        return method;
    }

    private MethodDeclarationSyntax GeneratorGetClaimsAsync(string identityUserType, bool isThereUserDtoForResponse)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"private async Task<IList<Claim>> GetClaimsAsync({identityUserType} user, IList<string> roles)");
        sb.AppendLine("{");
        sb.AppendLine("    List<Claim> claimList = new List<Claim>()");
        sb.AppendLine("    {");
        sb.AppendLine("        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),");
        sb.AppendLine("        // new Claim(ClaimTypes.Name, $\"{user.Name} {user.LastName}\")");
        sb.AppendLine("    };");
        sb.AppendLine("");
        sb.AppendLine("    if (!string.IsNullOrEmpty(user.Email))");
        sb.AppendLine("        claimList.Add(new Claim(ClaimTypes.Email, user.Email));");
        sb.AppendLine("");
        sb.AppendLine("    IList<Claim>? persistentClaims = await _userManager.GetClaimsAsync(user);");
        sb.AppendLine("    claimList.AddRange(persistentClaims);");
        sb.AppendLine("");
        sb.AppendLine("    IEnumerable<Claim>? roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));");
        sb.AppendLine("    claimList.AddRange(roleClaims);");
        sb.AppendLine("");
        sb.AppendLine("    return claimList;");
        sb.AppendLine("}");

        var method = SyntaxFactory.ParseMemberDeclaration(sb.ToString()) as MethodDeclarationSyntax;
        if (method == null) throw new InvalidOperationException("Method parse failed.");

        return method;
    }



    #region Helpers Abstract
    // Entity Get Methods
    private MethodDeclarationSyntax GeneratorGetEntityMethodOfAbstract(string entityName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<{entityName}?>"), SyntaxFactory.Identifier("GetAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorGetEntityListMethodOfAbstract(string entityName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>?"))
                .WithDefault(
                    SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<ICollection<{entityName}>?>"), SyntaxFactory.Identifier("GetListAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }


    // Generic Get Methods
    private MethodDeclarationSyntax GeneratorGetGenericMethodOfAbstract(string entityName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        var typeParameterList = SyntaxFactory.TypeParameterList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.TypeParameter("TResponse")));

        var constraintClause = SyntaxFactory.TypeParameterConstraintClause("TResponse")
            .WithConstraints(
                SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(
                    SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("IDto"))));

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<TResponse?>"), "GetAsync")
            .WithTypeParameterList(typeParameterList)
            .AddParameterListParameters(paramList.ToArray())
            .AddConstraintClauses(constraintClause)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorGetGenericListMethodOfAbstract(string entityName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>?"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        var typeParameterList = SyntaxFactory.TypeParameterList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.TypeParameter("TResponse")));

        var constraintClause = SyntaxFactory.TypeParameterConstraintClause("TResponse")
            .WithConstraints(
                SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(
                    SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("IDto"))));

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<ICollection<TResponse>?>"), "GetListAsync")
            .WithTypeParameterList(typeParameterList)
            .AddParameterListParameters(paramList.ToArray())
            .AddConstraintClauses(constraintClause)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }


    // Get Select List Method
    private MethodDeclarationSyntax GeneratorGetSelectListMethodOfAbstract(string entityName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>?"))
                .WithDefault(
                    SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<SelectList>"), SyntaxFactory.Identifier("GetSelectListAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }


    // Entity Get Methods
    private MethodDeclarationSyntax GeneratorGetMethodByEntityOfAbstract(string entityName, string methodName, List<Field> uniqueFields)
    {
        var paramList = new List<ParameterSyntax>();

        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }

        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        );

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<{entityName}?>"), SyntaxFactory.Identifier(methodName))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorGetAllMethodByEntityOfAbstract(string entityName, string methodName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest?")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<ICollection<{entityName}>?>"), SyntaxFactory.Identifier(methodName))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorGetListMethodByEntityOfAbstract(string entityName, string methodName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicPaginationRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<PaginationResponse<{entityName}>>"), SyntaxFactory.Identifier(methodName))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }


    // Dto Get Methods
    private MethodDeclarationSyntax GeneratorGetMethodByDtoOfAbstract(string dtoName, string name, List<Field> uniqueFields)
    {
        var paramList = new List<ParameterSyntax>();

        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }

        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        );

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<{dtoName}?>"), SyntaxFactory.Identifier(name))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorGetAllMethodByDtoOfAbstract(string dtoName, string name)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest?")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<ICollection<{dtoName}>?>"), SyntaxFactory.Identifier(name))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorGetListMethodByDtoOfAbstract(string dtoName, string name)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicPaginationRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<PaginationResponse<{dtoName}>>"), SyntaxFactory.Identifier(name))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }


    // Create Update Delete
    private MethodDeclarationSyntax GeneratorCreateMethodOfAbstract(string retrunName, string argName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName(argName)
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<{retrunName}>"), SyntaxFactory.Identifier("CreateAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorUpdateMethodOfAbstract(string retrunName, string argName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName(argName)
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<{retrunName}>"), SyntaxFactory.Identifier("UpdateAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorDeleteMethodOfAbstract(string argName, bool isThereDto, List<Field> uniqueFields)
    {
        var paramList = new List<ParameterSyntax>();

        if (isThereDto)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                    .WithType(SyntaxFactory.IdentifierName(argName))
            );
        }
        else
        {
            foreach (var field in uniqueFields)
            {
                paramList.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                        .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
                );
            }
        }
        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
            .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
            .WithDefault(SyntaxFactory.EqualsValueClause(
                SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
            )
        ));

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task"), SyntaxFactory.Identifier("DeleteAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorUndoDeleteMethodOfAbstract(string retrunName, List<Field> uniqueFields)
    {
        var paramList = new List<ParameterSyntax>();
        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }
        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
            .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
            .WithDefault(SyntaxFactory.EqualsValueClause(
                SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
            )
        ));
        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<{retrunName}>"), SyntaxFactory.Identifier("UndoDeleteAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }


    // Datatable Methods
    private MethodDeclarationSyntax GeneratorDatatableClientSideMethodOfAbstract(string entityName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<DatatableResponseClientSide<{entityName}>>"), SyntaxFactory.Identifier("DatatableClientSideAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorDatatableServerSideMethodOfAbstract(string entityName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicDatatableServerSideRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<DatatableResponseServerSide<{entityName}>>"), SyntaxFactory.Identifier("DatatableServerSideAsync"))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }


    // Datatable Dto Methods
    private MethodDeclarationSyntax GeneratorDatatableClientSideByDtoOfAbstract(string dtoName, string methodName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<DatatableResponseClientSide<{dtoName}>>"), SyntaxFactory.Identifier(methodName))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax GeneratorDatatableServerSideByDtoOfAbstract(string dtoName, string methodName)
    {
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicDatatableServerSideRequest")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        return SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<DatatableResponseServerSide<{dtoName}>>"), SyntaxFactory.Identifier(methodName))
            .AddParameterListParameters(paramList.ToArray())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }
    #endregion



    #region Helpers Concrete
    // Entity Get Methods
    private MethodDeclarationSyntax GeneratorGetEntityMethodOfConcrete(string entityName)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("where"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("where"))),
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_GetAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<{entityName}?>"),
                SyntaxFactory.Identifier("GetAsync")
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }

    private MethodDeclarationSyntax GeneratorGetEntityListMethodOfConcrete(string entityName)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>?"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("where"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("where"))),
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_GetListAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<ICollection<{entityName}>?>"),
                SyntaxFactory.Identifier("GetListAsync")
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }


    // Generic Get Methods
    private MethodDeclarationSyntax GeneratorGetGenericMethodOfConcrete(string entityName)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("where"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("where"))),
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("_GetAsync"))
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("TResponse"))
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        var typeParameterList = SyntaxFactory.TypeParameterList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.TypeParameter("TResponse")));

        var constraintClause = SyntaxFactory.TypeParameterConstraintClause("TResponse")
            .WithConstraints(
                SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(
                    SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("IDto"))));


        // 6) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<TResponse?>"),
                SyntaxFactory.Identifier("GetAsync")
            )
            .WithTypeParameterList(typeParameterList)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .AddConstraintClauses(constraintClause)
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }

    private MethodDeclarationSyntax GeneratorGetGenericListMethodOfConcrete(string entityName)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>?"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("where"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("where"))),
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("_GetListAsync"))
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("TResponse"))
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        var typeParameterList = SyntaxFactory.TypeParameterList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.TypeParameter("TResponse")));

        var constraintClause = SyntaxFactory.TypeParameterConstraintClause("TResponse")
            .WithConstraints(
                SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(
                    SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("IDto"))));


        // 6) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<ICollection<TResponse>?>"),
                SyntaxFactory.Identifier("GetListAsync")
            )
            .WithTypeParameterList(typeParameterList)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .AddConstraintClauses(constraintClause)
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }


    // Get Select List Method
    private MethodDeclarationSyntax GeneratorGetSelectListMethodOfConcrete(string entityName, string uniqueName, string textName)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("where"))
                .WithType(SyntaxFactory.IdentifierName($"Expression<Func<{entityName}, bool>>?"))
                .WithDefault(
                    SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword)))),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };


        // 2) Arguments of method call  
        var awaitInvocation = SyntaxFactory.AwaitExpression(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("_GetListAsync"))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"s => new{{ s.{uniqueName}, s.{textName} }}"))
                        .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("select"))),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("where"))
                        .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("where"))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                        .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking"))),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                        .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
                )
        );


        var newSelectList = SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.IdentifierName("SelectList"))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(awaitInvocation),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(uniqueName))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(textName)))
                );

        // 4) Method Call Decleration by Result 
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(newSelectList))
                    )
                )
        );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);


        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<SelectList>"),
                SyntaxFactory.Identifier("GetSelectListAsync")
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }


    // Entity Get Methods
    private MethodDeclarationSyntax GeneratorGetMethodByEntityOfConcrete(string entityName, string methodName, List<Field> uniqueFields)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>();
        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }
        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        );

        // 2) If Statements
        List<IfStatementSyntax> ifStatements = new();
        foreach (var field in uniqueFields)
        {
            ifStatements.Add(CreateIfDefaultCheckCondition(field.Name.ToCamelCase()));
        }

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            CreateWhereRule(uniqueFields),
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_GetAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 6) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 7) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.AddRange(ifStatements);
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<{entityName}?>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }

    private MethodDeclarationSyntax GeneratorGetAllMethodByEntityOfConcrete(string entityName, string methodName)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest?")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request?.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request?.Sorts"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("sorts"))),
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_GetListAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<ICollection<{entityName}>?>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorGetListMethodByEntityOfConcrete(string entityName, string methodName)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicPaginationRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.PaginationRequest"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("paginationRequest"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Sorts"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("sorts"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_PaginationAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<PaginationResponse<{entityName}>>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }



    // Dto Get Methods
    private MethodDeclarationSyntax GeneratorGetMethodByDtoOfConcrete(string methodName, Dto dto, List<Field> uniqueFields, bool isThereInclude)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>();
        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }
        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        );

        // 2) If Statements
        List<IfStatementSyntax> ifStatements = new();
        foreach (var field in uniqueFields)
        {
            ifStatements.Add(CreateIfDefaultCheckCondition(field.Name.ToCamelCase()));
        }

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            CreateWhereRule(uniqueFields)
        };

        if (isThereInclude) arguments.Add(CreateIncludeRule(dto));

        arguments.Add(
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking")))
        );
        arguments.Add(
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        );

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("_GetAsync"))
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName(dto.Name))
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 6) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 7) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.AddRange(ifStatements);
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<{dto.Name}?>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }

    private MethodDeclarationSyntax GeneratorGetAllMethodByDtoOfConcrete(string methodName, Dto dto, bool isThereInclude)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest?")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request?.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request?.Sorts"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("sorts")))
        };

        if (isThereInclude) arguments.Add(CreateIncludeRule(dto));

        arguments.Add(
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("tracking")))
        );
        arguments.Add(
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        );

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("_GetListAsync"))
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName(dto.Name))
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<ICollection<{dto.Name}>?>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorGetListMethodByDtoOfConcrete(string methodName, Dto dto, bool isThereInclude)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicPaginationRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.PaginationRequest"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("paginationRequest"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Sorts"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("sorts")))
        };

        if (isThereInclude) arguments.Add(CreateIncludeRule(dto));

        arguments.Add(
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        );

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("_PaginationAsync"))
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName(dto.Name))
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<PaginationResponse<{dto.Name}>>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }


    // Create Update Delete
    private MethodDeclarationSyntax GeneratorCreateMethodOfConcrete(Entity entity, List<Dto> dtos)
    {
        var basicResponseDto = dtos.FirstOrDefault(f => f.Id == entity.BasicResponseDtoId);
        bool isThereBasicResponseDto = basicResponseDto != null;

        var createDto = dtos.FirstOrDefault(f => f.Id == entity.CreateDtoId);
        bool isThereCreateDto = createDto != null;

        string argType = isThereCreateDto ? createDto!.Name : entity.Name;
        string returnType = isThereBasicResponseDto ? basicResponseDto!.Name : entity.Name;

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>();
        if (isThereCreateDto)
        {
            attributeList.Add(
                SyntaxFactory.Attribute(
                    SyntaxFactory.IdentifierName("Validation"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.TypeOfExpression(
                                    SyntaxFactory.IdentifierName(createDto!.Name)
                                )
                            )
                        )
                    )
                )
            );
        }

        // 2) Parameter List
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName(argType)
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 3) Method Call
        List<IdentifierNameSyntax> identifierNameSyntaxes = new List<IdentifierNameSyntax>();
        if (isThereCreateDto)
            identifierNameSyntaxes.Add(SyntaxFactory.IdentifierName(createDto!.Name));

        if (isThereBasicResponseDto)
            identifierNameSyntaxes.Add(SyntaxFactory.IdentifierName(basicResponseDto!.Name));

        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    identifierNameSyntaxes.Count > 0 ?
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("_AddAsync"))
                            .AddTypeArgumentListArguments(identifierNameSyntaxes.ToArray())
                        :
                        SyntaxFactory.IdentifierName("_AddAsync")
                )
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request")),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                )
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));


        // 6) Method Decleration
        if (attributeList.Count > 0)
        {
            return SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.ParseTypeName($"Task<{returnType}>"),
                    SyntaxFactory.Identifier("CreateAsync")
                )
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                )
                .AddParameterListParameters(paramList.ToArray())
                .WithBody(
                    SyntaxFactory.Block(
                        methodCallDecleration,
                        returnStatement
                    )
                );
        }
        else
        {

            return SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.ParseTypeName($"Task<{returnType}>"),
                    SyntaxFactory.Identifier("CreateAsync")
                )
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                )
                .AddParameterListParameters(paramList.ToArray())
                .WithBody(
                    SyntaxFactory.Block(
                        methodCallDecleration,
                        returnStatement
                    )
                );
        }
    }

    private MethodDeclarationSyntax GeneratorUpdateMethodOfConcrete(Entity entity, List<Dto> dtos, List<Field> uniqueFields)
    {
        var basicResponseDto = dtos.FirstOrDefault(f => f.Id == entity.BasicResponseDtoId);
        bool isThereBasicResponseDto = basicResponseDto != null;

        var updateDto = dtos.FirstOrDefault(f => f.Id == entity.UpdateDtoId);
        bool isThereUpdateDto = updateDto != null;

        string argType = isThereUpdateDto ? updateDto!.Name : entity.Name;
        string returnType = isThereBasicResponseDto ? basicResponseDto!.Name : entity.Name;

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>();
        if (isThereUpdateDto)
        {
            attributeList.Add(
                SyntaxFactory.Attribute(
                    SyntaxFactory.IdentifierName("Validation"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.TypeOfExpression(
                                    SyntaxFactory.IdentifierName(updateDto!.Name)
                                )
                            )
                        )
                    )
                )
            );
        }

        // 2) Parameter List
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName(argType)
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };


        // 3) Arguments of method call
        var argumentsOfMethod = new List<ArgumentSyntax>();
        if (isThereUpdateDto)
        {
            argumentsOfMethod.Add(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
                    .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("updateModel"))));
            argumentsOfMethod.Add(CreateWhereRule(uniqueFields, "request"));
        }
        else
        {
            argumentsOfMethod.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
                    .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("entity"))));
            argumentsOfMethod.Add(CreateWhereRule(uniqueFields, "request"));
        }
        argumentsOfMethod.Add(
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken"))));

        // 4) Method Call
        List<IdentifierNameSyntax> identifierNameSyntaxes = new List<IdentifierNameSyntax>();
        if (isThereUpdateDto)
            identifierNameSyntaxes.Add(SyntaxFactory.IdentifierName(updateDto!.Name));

        if (isThereBasicResponseDto)
            identifierNameSyntaxes.Add(SyntaxFactory.IdentifierName(basicResponseDto!.Name));

        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    identifierNameSyntaxes.Count > 0 ?
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("_UpdateAsync"))
                            .AddTypeArgumentListArguments(identifierNameSyntaxes.ToArray())
                        :
                        SyntaxFactory.IdentifierName("_UpdateAsync")
                )
                .AddArgumentListArguments(argumentsOfMethod.ToArray())
            );

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 6) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 7) Method Decleration
        if (attributeList.Count > 0)
        {
            return SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.ParseTypeName($"Task<{returnType}>"),
                    SyntaxFactory.Identifier("UpdateAsync")
                )
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                )
                .AddParameterListParameters(paramList.ToArray())
                .WithBody(
                    SyntaxFactory.Block(
                        methodCallDecleration,
                        returnStatement
                    )
                );
        }
        else
        {
            return SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.ParseTypeName($"Task<{returnType}>"),
                    SyntaxFactory.Identifier("UpdateAsync")
                )
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                )
                .AddParameterListParameters(paramList.ToArray())
                .WithBody(
                    SyntaxFactory.Block(
                        methodCallDecleration,
                        returnStatement
                    )
                );
        }
    }

    private MethodDeclarationSyntax GeneratorDeleteMethodOfConcrete(Entity entity, List<Dto> dtos, List<Field> uniqueFields)
    {
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        bool isThereDeleteDto = deleteDto != null;

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>();
        if (isThereDeleteDto)
        {
            attributeList.Add(
                SyntaxFactory.Attribute(
                    SyntaxFactory.IdentifierName("Validation"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.TypeOfExpression(
                                    SyntaxFactory.IdentifierName(deleteDto!.Name)
                                )
                            )
                        )
                    )
                )
            );
        }

        // 2) Parameters
        var paramList = new List<ParameterSyntax>();

        if (isThereDeleteDto)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                    .WithType(SyntaxFactory.IdentifierName(deleteDto!.Name))
            );
        }
        else
        {
            foreach (var field in uniqueFields)
            {
                paramList.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                        .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
                );
            }
        }

        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        );


        // 3) If Statements
        List<IfStatementSyntax> ifStatements = new();
        if (isThereDeleteDto == false)
        {
            foreach (var field in uniqueFields)
            {
                ifStatements.Add(CreateIfDefaultCheckCondition(field.Name.ToCamelCase()));
            }
        }

        // 4) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            CreateWhereRule(uniqueFields, isThereDeleteDto ? "request" : null),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_DeleteAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.ExpressionStatement(awaitInvocation);

        // 7) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.AddRange(ifStatements);
        bodyStatements.Add(methodCallDecleration);

        if (attributeList.Count > 0)
        {
            return SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.ParseTypeName("Task"),
                    SyntaxFactory.Identifier("DeleteAsync")
                )
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                )
                .AddParameterListParameters(paramList.ToArray())
                .WithBody(SyntaxFactory.Block(bodyStatements));
        }
        else
        {
            return SyntaxFactory
               .MethodDeclaration(
                   SyntaxFactory.ParseTypeName("Task"),
                   SyntaxFactory.Identifier("DeleteAsync")
               )
               .AddModifiers(
                   SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                   SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
               )
               .AddParameterListParameters(paramList.ToArray())
               .WithBody(SyntaxFactory.Block(bodyStatements));
        }
    }

    private MethodDeclarationSyntax GeneratorUndoDeleteMethodOfConcrete(Entity entity, List<Field> uniqueFields)
    {
        // 1) Parameter List
        var paramList = new List<ParameterSyntax>();
        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }
        paramList.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        );

        // 2) If Statements
        List<IfStatementSyntax> ifStatements = new(); 
        foreach (var field in uniqueFields)
        {
            ifStatements.Add(CreateIfDefaultCheckCondition(field.Name.ToCamelCase()));
        } 

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            CreateWhereRule(uniqueFields, null),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };


        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_UndoDeleteAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 6) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 7) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.AddRange(ifStatements);
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnStatement);
        
        // 8) Method Decleration 
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<{entity.Name}>"),
                SyntaxFactory.Identifier("UndoDeleteAsync")
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }

    // Datatable Methods
    private MethodDeclarationSyntax GeneratorDatatableClientSideMethodOfConcrete(Entity entity)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Sorts"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("sorts"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };


        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_DatatableClientSideAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<DatatableResponseClientSide<{entity.Name}>>"),
                SyntaxFactory.Identifier("DatatableClientSideAsync")
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorDatatableServerSideMethodOfConcrete(Entity entity)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicDatatableServerSideRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.GetDatatableRequest()"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("datatableRequest"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        };


        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("_DatatableServerSideAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<DatatableResponseServerSide<{entity.Name}>>"),
                SyntaxFactory.Identifier("DatatableServerSideAsync")
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }


    // Datatable Dto Methods
    private MethodDeclarationSyntax GeneratorDatatableClientSideByDtoOfConcrete(string methodName, Dto dto, bool isThereInclude)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest")
            ),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
                )
            )
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Sorts"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("sorts")))
        };
        if (isThereInclude) arguments.Add(CreateIncludeRule(dto));
        arguments.Add(
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        );

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("_DatatableClientSideAsync"))
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName(dto.Name))
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<DatatableResponseClientSide<{dto.Name}>>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorDatatableServerSideByDtoOfConcrete(string methodName, Dto dto, bool isThereInclude)
    {
        // 1) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicDatatableServerSideRequest")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
                .WithDefault(SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))))
        };

        // 2) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.GetDatatableRequest()"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("datatableRequest"))),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request.Filter"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("filter")))
        };
        if (isThereInclude) arguments.Add(CreateIncludeRule(dto));
        arguments.Add(
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("cancellationToken"))
                .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("cancellationToken")))
        );

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("_DatatableServerSideAsync"))
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName(dto.Name))
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 4) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("result"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 5) Return Statement
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));

        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName($"Task<DatatableResponseServerSide<{dto.Name}>>"),
                SyntaxFactory.Identifier(methodName)
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnStatement
                )
            );
    }
    #endregion


    private IfStatementSyntax CreateIfDefaultCheckCondition(string argName)
    {
        var condition = SyntaxFactory.BinaryExpression(
            SyntaxKind.EqualsExpression,
            SyntaxFactory.IdentifierName(argName),
            SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
        );

        var argumentNullException = SyntaxFactory.ThrowStatement(
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("ArgumentNullException"))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof"))
                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(argName)))
                    )
                )
            );

        return SyntaxFactory.IfStatement(
            condition,
            argumentNullException
        );
    }
    private ArgumentSyntax CreateWhereRule(List<Field> uniqueFields, string? sourceName = null)
    {
        bool isThereSource = !(string.IsNullOrEmpty(sourceName) || string.IsNullOrWhiteSpace(sourceName));
        if (isThereSource) sourceName = sourceName!.Trim();
        else sourceName = "";

        var firstField = uniqueFields.First();

        BinaryExpressionSyntax combined = SyntaxFactory.BinaryExpression(
            SyntaxKind.EqualsExpression,
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("f"), SyntaxFactory.IdentifierName(firstField.Name)),
            SyntaxFactory.IdentifierName(isThereSource ? $"{sourceName}.{firstField.Name}" : firstField.Name.ToCamelCase())
        );

        for (int i = 1; i < uniqueFields.Count; i++)
        {
            var nextField = uniqueFields[i];
            var next = SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("f"), SyntaxFactory.IdentifierName(nextField.Name)),
                SyntaxFactory.IdentifierName(isThereSource ? $"{sourceName}.{nextField.Name}" : nextField.Name.ToCamelCase())
            );
            combined = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, combined, next);
        }

        return SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression(combined)
            .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("f"))))
            .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("where")));
    }
    private ArgumentSyntax CreateIncludeRule(Dto dto)
    {
        InvocationExpressionSyntax? chain = null;

        var dto_DtoFields = _dtoFieldRepository.GetAll(f => f.DtoId == dto.Id, include: i => i.Include(x => x.SourceField));
        if (dto_DtoFields != default)
            dto_DtoFields = dto_DtoFields.GroupBy(p => p.SourceField.EntityId).Select(g => g.First()).ToList();

        foreach (var dtoField in dto_DtoFields!)
        {
            var dtoFieldRelations = _dtoFieldRepository.GetDtoFieldRelations(dtoField.Id);
            if (!dtoFieldRelations.Any()) continue;

            var dfrFirst = dtoFieldRelations.First();

            bool controlOfRelationFirst = dfrFirst.Relation.PrimaryField.EntityId == dto.RelatedEntityId;

            string destPropOfFirst = controlOfRelationFirst ? dfrFirst.Relation.PrimaryEntityVirPropName : dfrFirst.Relation.ForeignEntityVirPropName;

            ExpressionSyntax identifierNameSyntax = chain != null ? chain : SyntaxFactory.IdentifierName("i");
            chain = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, identifierNameSyntax, SyntaxFactory.IdentifierName("Include")))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(
                            SyntaxFactory.SimpleLambdaExpression(
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("x"), SyntaxFactory.IdentifierName(destPropOfFirst)))
                        )
                    );

            int lastDestEntityId = controlOfRelationFirst ? dfrFirst.Relation.ForeignField.EntityId : dfrFirst.Relation.PrimaryField.EntityId;

            for (int i = 1; i < dtoFieldRelations.Count; i++)
            {
                var dfr = dtoFieldRelations[i];

                bool controlOfRelation = dfr.Relation.PrimaryField.EntityId == lastDestEntityId;

                string destProp = controlOfRelation ? dfr.Relation.PrimaryEntityVirPropName : dfr.Relation.ForeignEntityVirPropName;

                chain = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, chain, SyntaxFactory.IdentifierName("ThenInclude")))
                        .AddArgumentListArguments(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("x"), SyntaxFactory.IdentifierName(destProp)))
                            )
                        );

                lastDestEntityId = controlOfRelation ? dfr.Relation.ForeignField.EntityId : dfr.Relation.PrimaryField.EntityId;
            }
        }

        return SyntaxFactory.Argument(
            SyntaxFactory.ParenthesizedLambdaExpression(chain!)
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("i"))))
            .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("include")));
    }

    private FieldDeclarationSyntax CreatePrivateField(string type, string name, bool? isReadOnly = false)
    {
        if (isReadOnly == true)
        {
            return SyntaxFactory
            .FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(type))
            .AddVariables(SyntaxFactory.VariableDeclarator(name)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
            );
        }
        return SyntaxFactory
            .FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(type))
            .AddVariables(SyntaxFactory.VariableDeclarator(name)))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
    }
}