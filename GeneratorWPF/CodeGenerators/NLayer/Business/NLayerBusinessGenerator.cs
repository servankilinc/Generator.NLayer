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

    #region Service
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

            results.Add(AddFile(folderPathAbstract, $"I{entity.Name}Service.cs", code_abstract.ToFullString()));
            results.Add(AddFile(folderPathConcrete, $"{entity.Name}Service.cs", code_concrete.ToFullString()));
        }

        if (_appSetting.IsThereIdentity)
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



            var code_AuthService = CompilationUnit(
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
                    $"{_appSetting.BusinessLayerProjectName}.Utils.TokenService"
                ],
                nspace: NamespaceDeclaration(
                    value: $"{_appSetting.BusinessLayerProjectName}.Concrete",
                    members: [
                        ClassDeclaration(
                            name: "AuthService",
                            modifiers: [SyntaxKind.PublicKeyword],
                            baseTypes: [SyntaxFactory.ParseTypeName("IAuthService")],
                            members: [
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "IUnitOfWork", "_unitOfWork"),
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "ITokenService", "_tokenService"),
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "UserManager<User>", "_userManager"),
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "SignInManager<User>", "_signInManager"),
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "IHttpContextManager", "_httpContextManager"),
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "IValidationService", "_validationService"),
                                FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "IMapper", "_mapper"),

                                ConstructorDeclaration(
                                    modifiers: [SyntaxKind.PublicKeyword],
                                    name: "AuthService",
                                    parameters: [
                                        ParameterDeclaration("IUnitOfWork", "unitOfWork", false),
                                        ParameterDeclaration("ITokenService", "tokenService", false),
                                        ParameterDeclaration("UserManager<User>", "userManager", false),
                                        ParameterDeclaration("SignInManager<User>", "signInManager", false),
                                        ParameterDeclaration("IHttpContextManager", "httpContextManager", false),
                                        ParameterDeclaration("IValidationService", "validationService", false),
                                        ParameterDeclaration("IMapper", "mapper", false)
                                    ],
                                    statements: [
                                        StatementExpression("unitOfWork", "_unitOfWork"),
                                        StatementExpression("tokenService", "_tokenService"),
                                        StatementExpression("userManager", "_userManager"),
                                        StatementExpression("signInManager", "_signInManager"),
                                        StatementExpression("httpContextManager", "_httpContextManager"),
                                        StatementExpression("validationService", "_validationService"),
                                        StatementExpression("mapper", "_mapper")
                                    ]
                                ),

                                MethodDeclaration(
                                    modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                                    name: "LoginAsync",
                                    returnType: $"Task<Result<LoginResponse>>",
                                    parameters: [
                                        ParameterDeclaration("LoginRequest", "loginRequest", true),
                                        ParameterDeclaration("CancellationToken", "cancellationToken", false)
                                    ],
                                    body: @"
                                        var validationResult = await _validationService.ValidateAsync(loginRequest, cancellationToken);
                                        if (!validationResult.IsValid)
                                            return Result<LoginResponse>.Validation(validationResult.Failures);

                                        // 1) Find user by credentials
                                        User? user = null;
                                        if (loginRequest.Email != null)
                                        {
                                            user = await _userManager.FindByEmailAsync(loginRequest.Email);
                                        }
                                        else if (loginRequest.UserName != null)
                                        {
                                            user = await _userManager.FindByNameAsync(loginRequest.UserName);
                                        }
                                        if (user == null)
                                            return Result<LoginResponse>.Failure(message: ""Credentials are incorrect."", metadata: GlobalExtensions.Meta(""Requester Email or Username"", loginRequest.Email ?? loginRequest.UserName));

                                        // 2) Check password
                                        SignInResult checkPassword = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: true);
                                        if (!checkPassword.Succeeded)
                                        {
                                            if (checkPassword.IsLockedOut)
                                                return Result<LoginResponse>.Failure(message: ""Your account is temporarily locked due to multiple failed login attempts."", metadata: GlobalExtensions.Meta(""Requester Email"", loginRequest.Email));
                                            if (checkPassword.RequiresTwoFactor)
                                                return Result<LoginResponse>.Failure(message: ""Two-factor authentication is required to login."", metadata: GlobalExtensions.Meta(""Requester Email"", loginRequest.Email));
                                            if (checkPassword.IsNotAllowed)
                                                return Result<LoginResponse>.Failure(message: ""The user is not allowed to sign in."", metadata: GlobalExtensions.Meta(""Requester Email"", loginRequest.Email));
                                            return Result<LoginResponse>.Failure(message: ""Credentials are incorrect."", metadata: GlobalExtensions.Meta(""Requester Email or Username"", loginRequest.Email ?? loginRequest.UserName));
                                        }

                                        if (!await _signInManager.CanSignInAsync(user))
                                        {
                                            return Result<LoginResponse>.Failure(message: ""You are not allowed to login."", metadata: GlobalExtensions.Meta(""User"", user));
                                        }

                                        // 3) Get user roles and claims
                                        IList<string> roles = await _userManager.GetRolesAsync(user);
                                        IList<Claim> claims = await GetClaimsAsync(user, roles);

                                        // 3) Generate Access Token and Refresh Token
                                        Result<AccessToken> accessToken = _tokenService.GenerateAccessToken(claims);
                                        if (!accessToken.IsSuccess)
                                            return Result<LoginResponse>.Failure(description: ""Access token could not generated"", metadata: GlobalExtensions.Meta(""Access Token Result"", accessToken));

                                        string tokenValue = _tokenService.GenerateRandomNumber();
                                        Result<RefreshToken> refreshToken = _tokenService.GenerateRefreshToken(user, tokenValue, loginRequest.ClientType, loginRequest.DeviceId);
                                        if (!refreshToken.IsSuccess)
                                            return Result<LoginResponse>.Failure(description: ""Refresh token could not generated"", metadata: GlobalExtensions.Meta(""Refresh Token Result"", refreshToken));

                                        // 4) Save Refresh Token and Revoke old ones if deviceId is provided
                                        if (loginRequest.DeviceId != null && loginRequest.DeviceId.HasValue)
                                        {
                                            await _unitOfWork.RefreshTokens.RevokeDeviceRefreshTokensAsync(f => f.DeviceId == loginRequest.DeviceId.Value && f.IsRevoked == false);
                                        }
                                        await _unitOfWork.RefreshTokens.AddAndSaveAsync(refreshToken.Data, cancellationToken);

                                        if (refreshToken.Data.ClientType != ClientType.Web)
                                        {
                                            return Result<LoginResponse>.Success(new LoginTrustedResponse
                                            {
                                                AccessToken = accessToken.Data,
                                                RefreshToken = tokenValue,
                                                DeviceId = refreshToken.Data.DeviceId,
                                                //User = user,
                                                Roles = roles
                                            });
                                        }
                                        else
                                        {
                                            _httpContextManager.AddRefreshTokenToCookie(tokenValue, refreshToken.Data.ExpirationUtc);
                                            return Result<LoginResponse>.Success(new LoginResponse
                                            {
                                                AccessToken = accessToken.Data,
                                                DeviceId = refreshToken.Data.DeviceId,
                                                //User = user,
                                                Roles = roles
                                            });
                                        }
                                    "
                                ),
                                MethodDeclaration(
                                    modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                                    name: "SignUpAsync",
                                    returnType: $"Task<Result<SignUpResponse>>",
                                    parameters: [
                                        ParameterDeclaration("SignUpRequest", "signUpRequest", true),
                                        ParameterDeclaration("CancellationToken", "cancellationToken", false)
                                    ],
                                    body: @"
                                        try
                                        {
                                            var validationResult = await _validationService.ValidateAsync(signUpRequest, cancellationToken);
                                            if (!validationResult.IsValid)
                                                return Result<SignUpResponse>.Validation(validationResult.Failures);

                                            await _unitOfWork.BeginTransactionAsync(cancellationToken);

                                            // 1) Check if user already exists
                                            var userExist = await _userManager.FindByEmailAsync(signUpRequest.Email);
                                            if (userExist != null)
                                                return Result<SignUpResponse>.Failure(message: ""The email address is already in use."", metadata: GlobalExtensions.Meta(""Request Email"", signUpRequest.Email));

                                            userExist = await _userManager.FindByNameAsync(signUpRequest.Email);
                                            if (userExist != null)
                                                return Result<SignUpResponse>.Failure(message: ""The user name is already in use."", metadata: GlobalExtensions.Meta(""Request User Name"", signUpRequest.UserName));

                                            // 2) Create new user
                                            var user = _mapper.Map<User>(signUpRequest);
                                            var result = await _userManager.CreateAsync(user, signUpRequest.Password);
                                            if (!result.Succeeded)
                                                return Result<SignUpResponse>.Failure(description: $""User cannot be created."", metadata: GlobalExtensions.Meta((""Requester Email"", signUpRequest.Email), (""Identity Service Errors"", result)));

                                            // 3) Assign ""User"" role to the new user
                                            var roleResult = await _userManager.AddToRoleAsync(user, ""User"");
                                            if (!roleResult.Succeeded)
                                                return Result<SignUpResponse>.Failure(description: $""Failed to assign role"", metadata: GlobalExtensions.Meta((""Requester Email"", signUpRequest.Email), (""Identity Service Errors"", roleResult)));

                                            // 4) Get user roles and claims
                                            IList<string> roles = await _userManager.GetRolesAsync(user);
                                            IList<Claim> claims = await GetClaimsAsync(user, roles);

                                            // 5) Generate Access Token and Refresh Token
                                            Result<AccessToken> accessToken = _tokenService.GenerateAccessToken(claims);
                                            if (!accessToken.IsSuccess)
                                                return Result<SignUpResponse>.Failure(description: ""Access token could not generated"", metadata: GlobalExtensions.Meta(""Access Token Result"", accessToken));

                                            string tokenValue = _tokenService.GenerateRandomNumber();
                                            Result<RefreshToken> refreshToken = _tokenService.GenerateRefreshToken(user, tokenValue, signUpRequest.ClientType, signUpRequest.DeviceId);
                                            if (!refreshToken.IsSuccess)
                                                return Result<SignUpResponse>.Failure(description: ""Refresh token could not generated"", metadata: GlobalExtensions.Meta(""Refresh Token Result"", refreshToken));

                                            // 6) Save Refresh Token and Revoke old ones if deviceId is provided
                                            if (signUpRequest.DeviceId != null && signUpRequest.DeviceId.HasValue)
                                            {
                                                await _unitOfWork.RefreshTokens.RevokeDeviceRefreshTokensAsync(f => f.DeviceId == signUpRequest.DeviceId.Value && f.IsRevoked == false);
                                            }
                                            await _unitOfWork.RefreshTokens.AddAndSaveAsync(refreshToken.Data, cancellationToken);


                                            await _unitOfWork.CommitTransactionAsync(cancellationToken);

                                            if (signUpRequest.ClientType != ClientType.Web)
                                            {
                                                return Result<SignUpResponse>.Success(new SignUpTrustedResponse
                                                {
                                                    AccessToken = accessToken.Data,
                                                    RefreshToken = tokenValue,
                                                    DeviceId = refreshToken.Data.DeviceId,
                                                    //User = user,
                                                    Roles = roles,
                                                });
                                            }
                                            else
                                            {
                                                _httpContextManager.AddRefreshTokenToCookie(tokenValue, refreshToken.Data.ExpirationUtc);
                                                return Result<SignUpResponse>.Success(new SignUpResponse
                                                {
                                                    AccessToken = accessToken.Data,
                                                    DeviceId = refreshToken.Data.DeviceId,
                                                    //User = user,
                                                    Roles = roles,
                                                });
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                                            throw;
                                        }
                                    "
                                ),
                                MethodDeclaration(
                                    modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                                    name: "RefreshAsync",
                                    returnType: $"Task<Result<RefreshAuthResponse>>",
                                    parameters: [
                                        ParameterDeclaration("RefreshRequest", "refreshAuthRequest", true),
                                        ParameterDeclaration("CancellationToken", "cancellationToken", false)
                                    ],
                                    body: @"
                                        try
                                        {
                                            var validationResult = await _validationService.ValidateAsync(refreshAuthRequest, cancellationToken);
                                            if (!validationResult.IsValid)
                                                return Result<RefreshAuthResponse>.Validation(validationResult.Failures);

                                            await _unitOfWork.BeginTransactionAsync(cancellationToken);

                                            // 1) Set refresh token from cookie if not provided
                                            if (string.IsNullOrWhiteSpace(refreshAuthRequest.RefreshToken))
                                            {
                                                var cookieValue = _httpContextManager.GetRefreshTokenFromCookie();
                                                if (!cookieValue.IsSuccess)
                                                    return Result<RefreshAuthResponse>.Failure(description: ""Refresh auth request cookie not found in cookie"", metadata: GlobalExtensions.Meta(""Cookie Result"", cookieValue.Error.Description));
                                                refreshAuthRequest.RefreshToken = cookieValue.Data;
                                            }
                                            string hashedToken = _tokenService.HashToken(refreshAuthRequest.RefreshToken);

                                            // 2) Find refresh token record
                                            RefreshToken? refreshToken = await _unitOfWork.RefreshTokens.GetAsync(where: f =>
                                                f.UserId == refreshAuthRequest.UserId &&
                                                f.DeviceId == refreshAuthRequest.DeviceId &&
                                                f.Token == hashedToken &&
                                                f.TTL > 0 &&
                                                f.IsRevoked == false &&
                                                f.ExpirationUtc > DateTime.UtcNow,
                                                cancellationToken: cancellationToken
                                            );
                                            if (refreshToken == null)
                                                return Result<RefreshAuthResponse>.Failure(description: ""There is no refresh token that can be used."", metadata: GlobalExtensions.Meta(""Request Model"", refreshAuthRequest));

                                            // 3) Find user
                                            var user = await _unitOfWork.Users.GetAsync(where: f => f.Id == refreshAuthRequest.UserId, cancellationToken: cancellationToken);
                                            if (user == null)
                                                return Result<RefreshAuthResponse>.Failure(description: $""User cannot found for refresh auth, userId: {refreshAuthRequest.UserId}"", metadata: GlobalExtensions.Meta(""Request Model"", refreshAuthRequest));

                                            // 4) Update refresh token 
                                            string tokenValue = _tokenService.GenerateRandomNumber();
                                            refreshToken.Token = _tokenService.HashToken(tokenValue);
                                            refreshToken.TTL -= 1;
                                            await _unitOfWork.RefreshTokens.UpdateAndSaveAsync(refreshToken, cancellationToken);

                                            // 5) revoke old tokens for the device
                                            await _unitOfWork.RefreshTokens.RevokeDeviceRefreshTokensAsync(f => f.DeviceId == refreshAuthRequest.DeviceId && f.IsRevoked == false && f.Id != refreshToken.Id, cancellationToken);

                                            // 6) Get user roles and claims
                                            IList<string> roles = await _userManager.GetRolesAsync(user);
                                            IList<Claim> claims = await GetClaimsAsync(user, roles);

                                            // 7) Generate new access token
                                            Result<AccessToken> accessToken = _tokenService.GenerateAccessToken(claims);
                                            if (!accessToken.IsSuccess)
                                                return Result<RefreshAuthResponse>.Failure(description: ""Access token could not generated"", metadata: GlobalExtensions.Meta(""Access Token Result"", accessToken));

                                            await _unitOfWork.CommitTransactionAsync(cancellationToken);

                                            if (refreshToken.ClientType != ClientType.Web)
                                            {
                                                return Result<RefreshAuthResponse>.Success(new RefreshAuthTrustedResponse
                                                {
                                                    AccessToken = accessToken.Data,
                                                    RefreshToken = tokenValue,
                                                    Roles = roles
                                                });
                                            }
                                            else
                                            {
                                                _httpContextManager.AddRefreshTokenToCookie(tokenValue, refreshToken.ExpirationUtc);
                                                return Result<RefreshAuthResponse>.Success(new RefreshAuthResponse
                                                {
                                                    AccessToken = accessToken.Data,
                                                    Roles = roles
                                                });
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                                            throw;
                                        }
                                    "
                                ),
                                MethodDeclaration(
                                    modifiers: [SyntaxKind.PrivateKeyword, SyntaxKind.AsyncKeyword],
                                    name: "GetClaimsAsync",
                                    returnType: $"Task<IList<Claim>>",
                                    parameters: [
                                        ParameterDeclaration("User", "user", true),
                                        ParameterDeclaration("IList<string>", "roles", false)
                                    ],
                                    body: @"
                                        List<Claim> claimList = new List<Claim>()
                                        {
                                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                            new Claim(ClaimTypes.Name, $""{user.Name} {user.LastName}"")
                                        };

                                        if (!string.IsNullOrEmpty(user.Email))
                                            claimList.Add(new Claim(ClaimTypes.Email, user.Email));

                                        IList<Claim>? persistentClaims = await _userManager.GetClaimsAsync(user);
                                        claimList.AddRange(persistentClaims);

                                        IEnumerable<Claim>? roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));
                                        claimList.AddRange(roleClaims);

                                        // password, role vs. değişdiğinde mevcut tokenları geçersiz kılmak için security stamp eklenebilir
                                        // var securityStamp = await _userManager.GetSecurityStampAsync(user);
                                        // claimList.Add(new Claim(""app_security_stamp_claim"", securityStamp));

                                        return claimList;
                                    "
                                )
                            ]
                        )
                    ]
                )
            );

            results.Add(AddFile(folderPathAbstract, "IAuthService.cs", code_IAuthService.ToFullString()));
            results.Add(AddFile(folderPathConcrete, "AuthService.cs", code_AuthService.ToFullString()));
        }

        return string.Join("\n", results);
    }

    private List<MethodDeclarationSyntax> GenerateAbstractMethods(Entity entity, List<Dto> dtos)
    {
        var methods = new List<MethodDeclarationSyntax>();

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).ToList();
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
                    ParameterDeclaration("DynamicRequest", "request", false),
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

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).OrderBy(f => f.Name).ToList();
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
                    ParameterDeclaration("DynamicRequest", "request", false),
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


    #region Mapping Profile
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
        return AddFile(folderPath, "MappingProfiles.cs", code);
    }
    #endregion

    #region Service Registration
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
        if (_appSetting.IsThereIdentity)
        {
            usings.Add($"{_appSetting.BusinessLayerProjectName}.Utils.TokenService");
        }
        #endregion

        #region Body
        StringBuilder sbBody = new();
        if (_appSetting.IsThereIdentity)
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
            usings: [.. usings],
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
        return AddFile(folderPath, "ServiceRegistration.cs", code.ToFullString());
    }
    #endregion
}