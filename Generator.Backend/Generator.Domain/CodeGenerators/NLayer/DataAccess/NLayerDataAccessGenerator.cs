using Humanizer;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Generator.Domain.CodeGenerators.Pipeline;
using Generator.Domain.CodeGenerators.Services;
using Generator.Domain.Repository;
using Microsoft.CodeAnalysis;
using Generator.Domain.Core.Entities;
using Generator.Domain.Core;

namespace Generator.Domain.CodeGenerators.NLayer.DataAccess;

public class NLayerDataAccessGenerator : IGenerationStep
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly RelationRepository _relationRepository;

    private readonly FileSystemService _fs;
    private readonly RoslynSyntaxHelper _roslyn;
    private readonly DotnetCliService _cli;
    private readonly TemplateRenderer _templateRenderer;

    public string Name => "DataAccess Layer";
    public int Order => 3;
    public int ProgressWeight => 20;

    public NLayerDataAccessGenerator(
        EntityRepository entityRepository,
        FieldRepository fieldRepository,
        RelationRepository relationRepository,
        FileSystemService fs,
        RoslynSyntaxHelper roslyn, DotnetCliService cli, TemplateRenderer templateRenderer)
    {
        _entityRepository = entityRepository;
        _fieldRepository = fieldRepository;
        _relationRepository = relationRepository;
        _fs = fs;
        _roslyn = roslyn;
        _cli = cli;
        _templateRenderer = templateRenderer;
    }

    public bool Execute(AppSetting appSetting, Action<string> log)
    {
        try
        {
            log(_cli.CreateClassLibraryProject(appSetting, appSetting.DataAccessLayerProjectName, new[] { $"../{appSetting.ModelLayerProjectName}/{appSetting.ModelLayerProjectName}.csproj" }));
            log(_cli.AddPackage(appSetting, "Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.4", appSetting.DataAccessLayerProjectName));
            log(_cli.AddPackage(appSetting, "Microsoft.EntityFrameworkCore.Design --version 10.0.4", appSetting.DataAccessLayerProjectName));
            log(_cli.AddPackage(appSetting, "Microsoft.EntityFrameworkCore.Sqlite --version 10.0.4", appSetting.DataAccessLayerProjectName));
            log(_cli.AddPackage(appSetting, "Microsoft.EntityFrameworkCore.SqlServer --version 10.0.4", appSetting.DataAccessLayerProjectName));
            log(_cli.Restore(appSetting, appSetting.DataAccessLayerProjectName));
            log(_templateRenderer.GenerateStaticFiles(appSetting, "DataAccess", appSetting.DataAccessLayerProjectName));
            log(GenerateRepositories(appSetting));
            log(GenerateUOW(appSetting));
            log(GenerateContext(appSetting));
            log(GenerateServiceRegistration(appSetting));
            return true;
        }
        catch (Exception ex)
        {
            log(ex.Message);
            return false;
        }
    }

    #region Repository
    private string GenerateRepositories(AppSetting appSetting)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        string folderPathAbstract = Path.Combine(appSetting.SolutionPath, appSetting.DataAccessLayerProjectName, "Abstract");
        string folderPathConcrete = Path.Combine(appSetting.SolutionPath, appSetting.DataAccessLayerProjectName, "Concrete");

        foreach (var entity in entities)
        {
            results.Add(_fs.AddFile(folderPathAbstract, $"I{entity.Name}Repository.cs", IRepository(entity.Name, appSetting)));
            results.Add(_fs.AddFile(folderPathConcrete, $"{entity.Name}Repository.cs", Repository(entity.Name, appSetting)));
        }

        if (appSetting.IsThereIdentity)
        {
            var abstractRefreshToeknService = IRepository(entityName: "RefreshToken", appSetting: appSetting,
                methods: [
                    _roslyn.MethodDeclaration(
                        name: "RevokeDeviceRefreshTokens",
                        returnType: "void",
                        parameters: [_roslyn.ParameterDeclaration("Expression<Func<RefreshToken, bool>>", "where", true)],
                        isThereBody: false
                    ),
                    _roslyn.MethodDeclaration(
                        name: "RevokeDeviceRefreshTokensAsync",
                        returnType: "Task",
                        parameters: [
                            _roslyn.ParameterDeclaration("Expression<Func<RefreshToken, bool>>", "where", true),
                            _roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")
                        ],
                        isThereBody: false
                    )
                ]
            );

            var concreteRefreshToeknService = Repository(
                entityName: "RefreshToken", appSetting: appSetting,
                methods: [
                    _roslyn.MethodDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "RevokeDeviceRefreshTokens",
                        returnType: "void",
                        parameters: [_roslyn.ParameterDeclaration("Expression<Func<RefreshToken, bool>>", "where", true)],
                        body: "_context.RefreshTokens.Where(where).ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));"
                    ),
                    _roslyn.MethodDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                        name: "RevokeDeviceRefreshTokensAsync",
                        returnType: "Task",
                        parameters: [
                            _roslyn.ParameterDeclaration("Expression<Func<RefreshToken, bool>>", "where", true),
                            _roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")
                        ],
                        body: "await _context.RefreshTokens.Where(where).ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true), cancellationToken);"
                    )
                ]
            );
            results.Add(_fs.AddFile(folderPathAbstract, "IRefreshTokenRepository.cs", abstractRefreshToeknService));
            results.Add(_fs.AddFile(folderPathConcrete, "RefreshTokenRepository.cs", concreteRefreshToeknService));
        }

        return string.Join("\n", results);
    }

    private string IRepository(string entityName, AppSetting appSetting, MethodDeclarationSyntax[]? methods = null)
    {
        return _roslyn.CompilationUnit(
            usings: [
                "System.Linq.Expressions",
                $"{appSetting.DataAccessLayerProjectName}.Repository",
                $"{appSetting.ModelLayerProjectName}.Entities"
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.DataAccessLayerProjectName}.Abstract",
                members: [
                    _roslyn.InterfaceDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"I{entityName}Repository",
                        baseTypes: [
                            SyntaxFactory.ParseTypeName($"IRepository<{entityName}>"),
                            SyntaxFactory.ParseTypeName($"IRepositoryAsync<{entityName}>")
                        ],
                        members: methods != null ? methods : []
                    )
                ]
            )
        ).ToFullString();
    }

    private string Repository(string entityName, AppSetting appSetting, MethodDeclarationSyntax[]? methods = null)
    {
        return _roslyn.CompilationUnit(
            usings: [
                "System.Linq.Expressions",
                "Microsoft.EntityFrameworkCore",
                $"{appSetting.DataAccessLayerProjectName}.Abstract",
                $"{appSetting.DataAccessLayerProjectName}.Contexts",
                $"{appSetting.DataAccessLayerProjectName}.Repository",
                $"{appSetting.ModelLayerProjectName}.Entities"
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.DataAccessLayerProjectName}.Concrete",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"{entityName}Repository",
                        baseTypes: [
                            SyntaxFactory.ParseTypeName($"RepositoryBase<{entityName}, AppDbContext>"),
                            SyntaxFactory.ParseTypeName($"I{entityName}Repository")
                        ],
                        members: [
                            _roslyn.ConstructorDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword],
                                name: $"{entityName}Repository",
                                parameters: [_roslyn.ParameterDeclaration("AppDbContext", "context")],
                                baseArgs: ["context"]
                            ),
                            ..methods ?? []
                        ]
                    )
                ]
            )
        ).ToFullString();
    }
    #endregion

    #region UnitOfWork
    private string GenerateUOW(AppSetting appSetting)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        string folderPath = Path.Combine(appSetting.SolutionPath, appSetting.DataAccessLayerProjectName, "UoW");

        results.Add(_fs.AddFile(folderPath, "IUnitOfWork.cs", IUnitOfWork(entities, appSetting)));
        results.Add(_fs.AddFile(folderPath, "UnitOfWork.cs", UnitOfWork(entities, appSetting)));

        return string.Join("\n", results);
    }

    private string UnitOfWork(List<Entity> entities, AppSetting appSetting)
    {
        var properties = new List<PropertyDeclarationSyntax>();
        foreach (var entity in entities)
            properties.Add(_roslyn.PropertyDeclaration($"I{entity.Name}Repository", entity.Name.Pluralize(), true, nullableDecleration: false, accessors: [_roslyn.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration), _roslyn.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, [SyntaxKind.PrivateKeyword])]));
        if (appSetting.IsThereIdentity)
            properties.Add(_roslyn.PropertyDeclaration("IRefreshTokenRepository", "RefreshTokens", true, nullableDecleration: false, accessors: [_roslyn.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration), _roslyn.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, [SyntaxKind.PrivateKeyword])]));

        var fileds = new List<FieldDeclarationSyntax>()
        {
            _roslyn.FieldDeclaration([SyntaxKind.PrivateKeyword],"IDbContextTransaction", "_transaction", nullable : true),
            _roslyn.FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "AppDbContext", "_context")
        };

        var constructor = _roslyn.ConstructorDeclaration(
            modifiers: [SyntaxKind.PublicKeyword],
            name: "UnitOfWork",
            parameters: [
                _roslyn.ParameterDeclaration("AppDbContext", "context"),
                ..entities.Select(e => _roslyn.ParameterDeclaration($"I{e.Name}Repository", $"{e.Name}Repository".ToCamelCase())),
            ],
            statements: [
                _roslyn.StatementExpression("_context", "context"),
                ..entities.Select(e => _roslyn.StatementExpression(e.Name.Pluralize(), $"{e.Name}Repository".ToCamelCase())),
            ]
        );
        if (appSetting.IsThereIdentity)
        {
            constructor = constructor.AddParameterListParameters(_roslyn.ParameterDeclaration("IRefreshTokenRepository", "refreshTokenRepository"));
            constructor = constructor.AddBodyStatements(_roslyn.StatementExpression("RefreshTokens", "refreshTokenRepository"));
        }

        var methodsConcrete = new List<MethodDeclarationSyntax>()
        {
            #region Syncronous
		    _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: "SaveChanges",
                returnType: "int",
                body: "return _context.SaveChanges();"
            ),
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: "BeginTransaction",
                returnType: "void",
                body: @"
                    if (_transaction != null) throw new InvalidOperationException(""Transaction already started for begin transaction."");
                    _transaction = _context.Database.BeginTransaction();
                "
            ),
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: "CommitTransaction",
                returnType: "void",
                body: @"
                    if (_transaction == null) throw new InvalidOperationException(""Transaction has not been started for commit transaction."");
                    _transaction.Commit();
                    _transaction.Dispose();
                    _transaction = null;
                "
            ),
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name : "RollbackTransaction",
                returnType : "void",
                body: @"
                    if (_transaction == null) throw new InvalidOperationException(""Transaction has not been started for rollback."");
                    _transaction.Rollback();
                    _transaction.Dispose();
                    _transaction = null;
                "
            ), 
	        #endregion
		
            #region Asyncronous
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "SaveChangesAsync",
                returnType: "Task<int>",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                body: "return await _context.SaveChangesAsync(cancellationToken);"
            ),
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "BeginTransactionAsync",
                returnType: "Task",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                body: @"
                    if (_transaction != null) throw new InvalidOperationException(""Transaction already started for begin transaction."");
                    _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                "
            ),
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "CommitTransactionAsync",
                returnType: "Task",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                body: @"
                    if (_transaction == null) throw new InvalidOperationException(""Transaction has not been started for commit."");
                    await _transaction.CommitAsync(cancellationToken);
                    await _transaction.DisposeAsync();
                    _transaction = null;
                "
            ),
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "RollbackTransactionAsync",
                returnType : "Task",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                body: @"
                    if (_transaction == null) throw new InvalidOperationException(""Transaction has not been started for rollback."");
                    await _transaction.RollbackAsync(cancellationToken);
                    await _transaction.DisposeAsync();
                    _transaction = null;    
                "
            ), 
	        #endregion
		
            #region Dispose
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: "Dispose",
                returnType : "void",
                body: @"
                    if (_transaction != null)
                    {
                        _transaction.Dispose();
                        _transaction = null;
                    }
                    _context.Dispose();
                "
            ),
            _roslyn.MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "DisposeAsync",
                returnType : "ValueTask",
                body: @"
                    if (_transaction != null)
                    {
                        await _transaction.DisposeAsync();
                        _transaction = null;
                    }
                    await _context.DisposeAsync();
                "
            ) 
	        #endregion
        };

        string code_concrete = _roslyn.CompilationUnit(
            usings: [
                $"{appSetting.DataAccessLayerProjectName}.Abstract",
                $"{appSetting.DataAccessLayerProjectName}.Contexts",
                "Microsoft.EntityFrameworkCore.Storage"
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.DataAccessLayerProjectName}.UoW",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "UnitOfWork",
                        baseTypes: [SyntaxFactory.ParseTypeName("IUnitOfWork")],
                        members: [
                            ..fileds,
                            ..properties,
                            constructor,
                            ..methodsConcrete
                        ]
                    )
                ]
            )
        ).ToFullString();
        return code_concrete;
    }

    private string IUnitOfWork(List<Entity> entities, AppSetting appSetting)
    {
        var properties = new List<PropertyDeclarationSyntax>();
        foreach (var entity in entities)
            properties.Add(_roslyn.PropertyDeclaration($"I{entity.Name}Repository", entity.Name.Pluralize(), required: true, modifiers: [], nullableDecleration: false, accessors: [_roslyn.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)]));
        if (appSetting.IsThereIdentity)
            properties.Add(_roslyn.PropertyDeclaration("IRefreshTokenRepository", "RefreshTokens", required: true, modifiers: [], nullableDecleration: false, accessors: [_roslyn.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)]));

        var abstractMethods = new List<MethodDeclarationSyntax>()
        {
            _roslyn.MethodDeclaration(name: "SaveChanges", returnType: "int", isThereBody: false),
            _roslyn.MethodDeclaration(name : "BeginTransaction", returnType : "void", isThereBody: false),
            _roslyn.MethodDeclaration(name : "CommitTransaction", returnType : "void", isThereBody: false),
            _roslyn.MethodDeclaration(name : "RollbackTransaction", returnType : "void", isThereBody: false),

            _roslyn.MethodDeclaration(
                name: "SaveChangesAsync",
                returnType: "Task<int>",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                isThereBody: false
            ),
            _roslyn.MethodDeclaration(
                name: "BeginTransactionAsync",
                returnType: "Task",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                isThereBody: false
            ),
            _roslyn.MethodDeclaration(
                name: "CommitTransactionAsync",
                returnType: "Task",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                isThereBody: false
            ),
            _roslyn.MethodDeclaration(
                name: "RollbackTransactionAsync",
                returnType : "Task",
                parameters: [_roslyn.ParameterDeclaration("CancellationToken", "cancellationToken", true, defaultValue: "default")],
                isThereBody: false
            )
        };

        return _roslyn.CompilationUnit(
            usings: [$"{appSetting.DataAccessLayerProjectName}.Abstract"],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.DataAccessLayerProjectName}.UoW",
                members: [
                    _roslyn.InterfaceDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "IUnitOfWork",
                        baseTypes: [
                            SyntaxFactory.ParseTypeName("IDisposable"),
                            SyntaxFactory.ParseTypeName("IAsyncDisposable")
                        ],
                        members: [
                            ..properties,
                            ..abstractMethods
                        ]
                    )
                ]
            )
        ).ToFullString();
    }
    #endregion

    #region Context
    private string GenerateContext(AppSetting appSetting)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        var identityTypeConfigs = appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);
        string IdentityKeyType = identityTypeConfigs.IdentityKeyType;
        string IdentityUserType = identityTypeConfigs.IdentityUserType;
        string IdentityRoleType = identityTypeConfigs.IdentityRoleType;

        #region DbSets
        var dbSets = entities.Select(e =>
            appSetting.IsThereIdentity && ((e.Id == appSetting.UserEntityId && e.Name == "User") || (e.Id == appSetting.RoleEntityId && e.Name == "Role")) ?
                _roslyn.PropertyDeclaration($"override DbSet<{e.Name}>", e.Name.Pluralize(), true, nullableDecleration: false) :
                _roslyn.PropertyDeclaration($"DbSet<{e.Name}>", e.Name.Pluralize(), true, nullableDecleration: false)
        ).ToList();
        if (appSetting.IsThereIdentity)
            dbSets.Add(_roslyn.PropertyDeclaration("DbSet<RefreshToken>", "RefreshTokens", true, nullableDecleration: false));
        dbSets.Add(_roslyn.PropertyDeclaration("DbSet<Log>", "Logs", true, nullableDecleration: false));
        dbSets.Add(_roslyn.PropertyDeclaration("DbSet<Archive>", "Archives", true, nullableDecleration: false));
        #endregion

        #region Model Builders
        var modelBuilders = new List<StatementSyntax>()
        {
            SyntaxFactory.ParseStatement("base.OnModelCreating(modelBuilder);")
        };

        foreach (var entity in entities)
        {
            char eSc = entity.Name.Trim().ToLowerInvariant()[0];

            var statements = new List<StatementSyntax>();

            #region Totable
            if (!string.IsNullOrEmpty(entity.TableName))
                statements.Add(SyntaxFactory.ParseStatement($"{eSc}.ToTable(\"{entity.TableName}\");"));
            #endregion

            #region HasKey
            if (entity.Fields.Count(f => f.IsUnique) == 1)
            {
                var uniqueField = entity.Fields.First(f => f.IsUnique);
                statements.Add(SyntaxFactory.ParseStatement($"{eSc}.HasKey({eSc} => {eSc}.{uniqueField.Name});"));
            }
            else
            {
                var uniqueFields = entity.Fields.Where(f => f.IsUnique).Select(x => $"{eSc}.{x.Name}");
                string keys = string.Join(",", uniqueFields);
                statements.Add(SyntaxFactory.ParseStatement($"{eSc}.HasKey({eSc} => new {{ {keys} }});"));
            }
            #endregion

            #region Relations
            var relations = _relationRepository.GetRelationsOnPrimary(entity.Id);
            foreach (var relation in relations)
            {
                char f_eSc = relation.ForeignField.Entity.Name.Trim().ToLowerInvariant()[0];
                if (relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToOne)
                {
                    statements.Add(RelationOneToOne(eSc, f_eSc, relation));
                }
                else if (relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToMany)
                {
                    statements.Add(RelationOneToMany(eSc, f_eSc, relation));
                }
            }

            if (appSetting.IsThereUser && entity.Id == appSetting.UserEntityId)
            {
                statements.Add(SyntaxFactory.ParseStatement(@"
                    u.HasMany(u => u.RefreshTokens)
                        .WithOne(r => r.User)
                        .HasForeignKey(r => r.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                "));
            }
            #endregion

            if (entity.SoftDeletable)
                statements.Add(SyntaxFactory.ParseStatement($"{eSc}.HasQueryFilter(f => !f.IsDeleted);"));

            modelBuilders.Add(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("modelBuilder"),
                            SyntaxFactory.GenericName("Entity")
                                .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName(entity.Name))
                        )
                    ).AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.SimpleLambdaExpression(
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier(eSc.ToString())),
                            SyntaxFactory.Block(statements)
                        ))
                    )
                )
            );
        }

        if (appSetting.IsThereIdentity)
        {
            modelBuilders.Add(SyntaxFactory.ParseStatement(@"
                modelBuilder.Entity<RefreshToken>(r =>
                {
                    r.HasKey(r => r.Id);

                    r.HasOne(r => r.User)
                        .WithMany(u => u.RefreshTokens)
                        .HasForeignKey(r => r.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                });")
            );
        }

        modelBuilders.Add(SyntaxFactory.ParseStatement(@"
            modelBuilder.Entity<Log>(l =>
            {
                l.ToTable(""ProjectLogs"");
                l.HasKey(l => l.Id);
            });

            modelBuilder.Entity<Archive>(a =>
            {
                a.ToTable(""ProjectArchives"");
                a.HasKey(a => a.Id);
            });")
        );

        if (appSetting.IsThereIdentity)
        {
            if (!appSetting.IsThereUser)
            {
                modelBuilders.Add(SyntaxFactory.ParseStatement($"modelBuilder.Entity<IdentityUser<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"Users\"); }});"));
            }

            if (!appSetting.IsThereRole)
            {
                List<(string roleName, string roleId, string concurrencyStamp)> defaultRoles = new List<(string roleName, string roleId, string concurrencyStamp)>
                {
                    new ()
                    {
                        roleName = "User",
                        roleId = IdentityKeyType == "int" ? "1" : IdentityKeyType == "string" ? "\"b370875e-34cd-4b79-891c-93ae38f99d11\"" : "new Guid(\"b370875e-34cd-4b79-891c-93ae38f99d11\")",
                        concurrencyStamp = IdentityKeyType == "int" ? "\"1\"" : IdentityKeyType == "string" ? "\"b370875e-34cd-4b79-891c-93ae38f99d11\"" : "new Guid(\"b370875e-34cd-4b79-891c-93ae38f99d11\").ToString()"
                    },
                    new ()
                    {
                        roleName = "Manager",
                        roleId = IdentityKeyType == "int" ? "2" : IdentityKeyType == "string" ? "\"cd6040ef-dacc-4678-9a85-154f12581cff\"" : "new Guid(\"cd6040ef-dacc-4678-9a85-154f12581cff\")",
                        concurrencyStamp = IdentityKeyType == "int" ? "\"2\"" : IdentityKeyType == "string" ? "\"cd6040ef-dacc-4678-9a85-154f12581cff\"" : "new Guid(\"cd6040ef-dacc-4678-9a85-154f12581cff\").ToString()"
                    },
                    new ()
                    {
                        roleName = "Admin",
                        roleId = IdentityKeyType == "int" ? "3" : IdentityKeyType == "string" ? "\"7138ec51-4f9e-4afd-b61b-5a9a4584f5da\"" : "new Guid(\"7138ec51-4f9e-4afd-b61b-5a9a4584f5da\")",
                        concurrencyStamp = IdentityKeyType == "int" ? "\"3\"" : IdentityKeyType == "string" ? "\"7138ec51-4f9e-4afd-b61b-5a9a4584f5da\"" : "new Guid(\"7138ec51-4f9e-4afd-b61b-5a9a4584f5da\").ToString()"
                    },
                    new ()
                    {
                        roleName = "Owner",
                        roleId = IdentityKeyType == "int" ? "4" : IdentityKeyType == "string" ? "\"1f20c152-530e-4064-a39c-bbbed341fe84\"" : "new Guid(\"1f20c152-530e-4064-a39c-bbbed341fe84\")",
                        concurrencyStamp = IdentityKeyType == "int" ? "\"4\"" : IdentityKeyType == "string" ? "\"1f20c152-530e-4064-a39c-bbbed341fe84\"" : "new Guid(\"1f20c152-530e-4064-a39c-bbbed341fe84\").ToString()"
                    }
                };

                string defaultRolesString = string.Join(",\n", defaultRoles.Select(r =>
                    $@"new IdentityRole<{IdentityKeyType}>
                    {{
                        Id = {r.roleId},
                        Name = ""{r.roleName}"",
                        NormalizedName = ""{r.roleName.ToUpperInvariant()}"",
                        ConcurrencyStamp = {r.concurrencyStamp}
                    }}"
                ));

                modelBuilders.Add(SyntaxFactory.ParseStatement(@$"
                    modelBuilder.Entity<IdentityRole<{IdentityKeyType}>>(entity =>
                    {{
                        entity.ToTable(""Roles"");

                        entity.HasData(
                          {defaultRolesString}  
                        );
                    }});
                "));
            }

            modelBuilders.Add(SyntaxFactory.ParseStatement($"modelBuilder.Entity<IdentityUserClaim<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserClaims\"); }});"));
            modelBuilders.Add(SyntaxFactory.ParseStatement($"modelBuilder.Entity<IdentityUserLogin<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserLogins\"); }});"));
            modelBuilders.Add(SyntaxFactory.ParseStatement($"modelBuilder.Entity<IdentityRoleClaim<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"RoleClaims\"); }});"));
            modelBuilders.Add(SyntaxFactory.ParseStatement($"modelBuilder.Entity<IdentityUserRole<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserRoles\"); }});"));
            modelBuilders.Add(SyntaxFactory.ParseStatement($"modelBuilder.Entity<IdentityUserToken<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserTokens\"); }});"));
        }
        #endregion

        var context = _roslyn.CompilationUnit(
            usings: [
                "Microsoft.AspNetCore.Identity",
                "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
                "Microsoft.EntityFrameworkCore",
                $"{appSetting.ModelLayerProjectName}.Entities",
                $"{appSetting.ModelLayerProjectName}.ProjectEntities"
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.DataAccessLayerProjectName}.Contexts",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "AppDbContext",
                        baseTypes: appSetting.IsThereIdentity
                            ? [SyntaxFactory.ParseTypeName($"IdentityDbContext<{IdentityUserType}, {IdentityRoleType}, {IdentityKeyType}>")]
                            : [SyntaxFactory.ParseTypeName("DbContext")],
                        members: [
                            _roslyn.ConstructorDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword],
                                name: "AppDbContext",
                                parameters: [_roslyn.ParameterDeclaration("DbContextOptions<AppDbContext>", "options")],
                                baseArgs: ["options"]
                            ),
                            ..dbSets,
                            _roslyn.MethodDeclaration(
                                modifiers: [SyntaxKind.ProtectedKeyword, SyntaxKind.OverrideKeyword],
                                name: "OnModelCreating",
                                returnType: "void",
                                parameters: [_roslyn.ParameterDeclaration("ModelBuilder", "modelBuilder")],
                                block: SyntaxFactory.Block(modelBuilders)
                            )
                        ]
                    )
                ]
            )
        ).ToFullString();

        string folderPath = Path.Combine(appSetting.SolutionPath, appSetting.DataAccessLayerProjectName, "Contexts");
        results.Add(_fs.AddFile(folderPath, "AppDbContext.cs", context));

        return string.Join("\n", results);
    }

    private StatementSyntax RelationOneToOne(char eSc, char f_eSc, Relation relation)
    {
        return SyntaxFactory.ParseStatement(
            @$"{eSc}.HasOne({eSc} => {eSc}.{relation.PrimaryEntityVirPropName})
                .WithOne({f_eSc} => {f_eSc}.{relation.ForeignEntityVirPropName})
                .HasForeignKey<{relation.ForeignField.Entity.Name}>({f_eSc} => {f_eSc}.{relation.ForeignField.Name})
                .OnDelete({relation.GetOnDeleteType()});
            ");
    }

    private StatementSyntax RelationOneToMany(char eSc, char f_eSc, Relation relation)
    {
        return SyntaxFactory.ParseStatement(
            @$"{eSc}.HasMany({eSc} => {eSc}.{relation.PrimaryEntityVirPropName})
            .WithOne({f_eSc} => {f_eSc}.{relation.ForeignEntityVirPropName})
            .HasForeignKey({f_eSc} => {f_eSc}.{relation.ForeignField.Name})
            .OnDelete({relation.GetOnDeleteType()});"
        );
    }
    #endregion

    #region ServiceRegistration
    private string GenerateServiceRegistration(AppSetting appSetting)
    {
        var sb = new StringBuilder();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        foreach (var entity in entities)
            sb.AppendLine($"services.AddScoped<I{entity.Name}Repository, {entity.Name}Repository>();");
        if (appSetting.IsThereIdentity)
            sb.AppendLine("services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();");

        sb.AppendLine(@"
            #region DB CONTEXT
            services.AddSingleton<AuditInterceptor>();
            services.AddSingleton<ArchiveInterceptor>();
            services.AddSingleton<SoftDeleteInterceptor>();

            services.AddDbContext<AppDbContext>((serviceProvider, opt) =>
            {
                opt.UseSqlServer(configuration.GetConnectionString(""Database""))
                    .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>())
                    .AddInterceptors(serviceProvider.GetRequiredService<ArchiveInterceptor>())
                    .AddInterceptors(serviceProvider.GetRequiredService<SoftDeleteInterceptor>());
            });
            #endregion

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        ");

        var code = _roslyn.CompilationUnit(
            usings: [
                $"{appSetting.DataAccessLayerProjectName}.Abstract",
                $"{appSetting.DataAccessLayerProjectName}.Concrete",
                $"{appSetting.DataAccessLayerProjectName}.Contexts",
                $"{appSetting.DataAccessLayerProjectName}.Interceptors",
                $"{appSetting.DataAccessLayerProjectName}.UoW",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.DependencyInjection",
            ],
            nspace: _roslyn.NamespaceDeclaration(
                value: $"{appSetting.DataAccessLayerProjectName}",
                members: [
                    _roslyn.ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
                        name: "ServiceRegistration",
                        members: [
                            _roslyn.MethodDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
                                name: "AddDataAccessServices",
                                returnType: "IServiceCollection",
                                parameters: [
                                    _roslyn.ParameterDeclaration(modifiers: [SyntaxKind.ThisKeyword], type: "IServiceCollection", name: "services"),
                                    _roslyn.ParameterDeclaration(type: "IConfiguration", name: "configuration")
                                ],
                                body: sb.ToString()
                            )
                        ]
                    )
                ]
             )
         );

        string folderPath = Path.Combine(appSetting.SolutionPath, appSetting.DataAccessLayerProjectName);
        return _fs.AddFile(folderPath, "ServiceRegistration.cs", code.ToFullString());
    }
    #endregion
}









