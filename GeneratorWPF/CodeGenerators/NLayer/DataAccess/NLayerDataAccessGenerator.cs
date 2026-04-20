using Humanizer;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using GeneratorWPF.CodeGenerators.NLayer.Base;
using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Microsoft.CodeAnalysis;

namespace GeneratorWPF.CodeGenerators.NLayer.DataAccess;

public class NLayerDataAccessGenerator : NLayerGeneratorBase
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly RelationRepository _relationRepository;
    public NLayerDataAccessGenerator(AppSetting appSetting) : base(appSetting)
    {
        _entityRepository = new();
        _fieldRepository = new();
        _relationRepository = new();
    }

    #region Repository
    public string GenerateRepositories()
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        string folderPathAbstract = Path.Combine(_appSetting.SolutionPath, _appSetting.DataAccessLayerProjectName, "Abstract");
        string folderPathConcrete = Path.Combine(_appSetting.SolutionPath, _appSetting.DataAccessLayerProjectName, "Concrete");

        foreach (var entity in entities)
        {
            results.Add(AddFile(folderPathAbstract, $"I{entity.Name}Repository.cs", IRepository(entity.Name)));
            results.Add(AddFile(folderPathConcrete, $"{entity.Name}Repository.cs", Repository(entity.Name)));
        }

        if (_appSetting.IsThereIdentity)
        {
            results.Add(AddFile(folderPathAbstract, "IRefreshTokenRepository.cs", IRepository("RefreshToken")));
            results.Add(AddFile(folderPathConcrete, "RefreshTokenRepository.cs", Repository("RefreshToken")));
        }

        return string.Join("\n", results);
    }

    private string IRepository(string entityName)
    {
        return CompilationUnit(
            usings: [
                $"{_appSetting.DataAccessLayerProjectName}.Repository",
                $"{_appSetting.ModelLayerProjectName}.Entities"
            ],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.DataAccessLayerProjectName}.Abstract",
                members: [
                    InterfaceDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"I{entityName}Repository",
                        baseTypes: [
                            SyntaxFactory.ParseTypeName($"IRepository<{entityName}>"),
                            SyntaxFactory.ParseTypeName($"IRepositoryAsync<{entityName}>")
                        ]
                    )
                ]
            )
        ).ToFullString();
    }

    private string Repository(string entityName)
    {
        return CompilationUnit(
            usings: [
                $"{_appSetting.DataAccessLayerProjectName}.Abstract",
                $"{_appSetting.DataAccessLayerProjectName}.Contexts",
                $"{_appSetting.DataAccessLayerProjectName}.Repository",
                $"{_appSetting.ModelLayerProjectName}.Entities"
            ],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.DataAccessLayerProjectName}.Concrete",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: $"{entityName}Repository",
                        baseTypes: [
                            SyntaxFactory.ParseTypeName($"RepositoryBase<{entityName}, AppDbContext>"),
                            SyntaxFactory.ParseTypeName($"I{entityName}Repository")
                        ],
                        members: [
                            ConstructorDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword],
                                name: $"{entityName}Repository",
                                parameters: [ParameterDeclaration("AppDbContext", "context")],
                                baseArgs: ["context"]
                            )
                        ]
                    )
                ]
            )
        ).ToFullString();
    }
    #endregion

    #region UnitOfWork
    public string GenerateUOW()
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.DataAccessLayerProjectName, "UoW");

        results.Add(AddFile(folderPath, "IUnitOfWork.cs", IUnitOfWork(entities)));
        results.Add(AddFile(folderPath, "UnitOfWork.cs", UnitOfWork(entities)));

        return string.Join("\n", results);
    }

    private string UnitOfWork(List<Entity> entities)
    {
        var properties = new List<PropertyDeclarationSyntax>();
        foreach (var entity in entities)
            properties.Add(PropertyDeclaration($"I{entity.Name}Repository", entity.Name.Pluralize(), true));
        if (_appSetting.IsThereIdentity)
            properties.Add(PropertyDeclaration("IRefreshTokenRepository", "RefreshTokens", true));

        var fileds = new List<FieldDeclarationSyntax>()
        {
            FieldDeclaration([SyntaxKind.PrivateKeyword],"IDbContextTransaction", "_transaction"),
            FieldDeclaration([SyntaxKind.PrivateKeyword, SyntaxKind.ReadOnlyKeyword], "AppDbContext", "_context")
        };

        var constructor = ConstructorDeclaration(
            modifiers: [SyntaxKind.PublicKeyword],
            name: "UnitOfWork",
            parameters: [
                ParameterDeclaration("AppDbContext", "context"),
                ..entities.Select(e => ParameterDeclaration($"I{e.Name}Repository", $"{e.Name}Repository".ToCamelCase())),
            ],
            statements: [
                StatementExpression("_context", "context"),
                ..entities.Select(e => StatementExpression(e.Name.Pluralize(), $"{e.Name}Repository".ToCamelCase())),
            ]
        );
        if (_appSetting.IsThereIdentity)
        {
            constructor = constructor.AddParameterListParameters(ParameterDeclaration("IRefreshTokenRepository", "refreshTokenRepository"));
            constructor = constructor.AddBodyStatements(StatementExpression("RefreshTokens", "refreshTokenRepository"));
        }

        var methodsConcrete = new List<MethodDeclarationSyntax>()
        {
            #region Syncronous
		    MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: "SaveChanges",
                returnType: "int",
                body: "return _context.SaveChanges();"
            ),
            MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword],
                name: "BeginTransaction",
                returnType: "void",
                body: @"
                    if (_transaction != null) throw new InvalidOperationException(""Transaction already started for begin transaction."");
                    _transaction = _context.Database.BeginTransaction();
                "
            ),
            MethodDeclaration(
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
            MethodDeclaration(
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
            MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "SaveChangesAsync",
                returnType: "Task<int>",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                body: "return await _context.SaveChangesAsync(cancellationToken);"
            ),
            MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "BeginTransactionAsync",
                returnType: "Task",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                body: @"
                    if (_transaction != null) throw new InvalidOperationException(""Transaction already started for begin transaction."");
                    _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                "
            ),
            MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "CommitTransactionAsync",
                returnType: "Task",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                body: @"
                    if (_transaction == null) throw new InvalidOperationException(""Transaction has not been started for commit."");
                    await _transaction.CommitAsync(cancellationToken);
                    await _transaction.DisposeAsync();
                    _transaction = null;
                "
            ),
            MethodDeclaration(
                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.AsyncKeyword],
                name: "RollbackTransactionAsync",
                returnType : "Task",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                body: @"
                    if (_transaction == null) throw new InvalidOperationException(""Transaction has not been started for rollback."");
                    await _transaction.RollbackAsync(cancellationToken);
                    await _transaction.DisposeAsync();
                    _transaction = null;    
                "
            ), 
	        #endregion
		
            #region Dispose
            MethodDeclaration(
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
            MethodDeclaration(
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

        string code_concrete = CompilationUnit(
            usings: [
                $"{_appSetting.DataAccessLayerProjectName}.Abstract",
                $"{_appSetting.DataAccessLayerProjectName}.Contexts",
                "Microsoft.EntityFrameworkCore.Storage"
            ],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.DataAccessLayerProjectName}.UoW",
                members: [
                    ClassDeclaration(
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

    private string IUnitOfWork(List<Entity> entities)
    {
        var properties = new List<PropertyDeclarationSyntax>();
        foreach (var entity in entities)
            properties.Add(PropertyDeclaration($"I{entity.Name}Repository", entity.Name.Pluralize(), true));
        if (_appSetting.IsThereIdentity)
            properties.Add(PropertyDeclaration("IRefreshTokenRepository", "RefreshTokens", true));

        var abstractMethods = new List<MethodDeclarationSyntax>()
        {
            MethodDeclaration(name: "SaveChanges", returnType: "int", isThereBody: false),
            MethodDeclaration(name : "BeginTransaction", returnType : "void", isThereBody: false),
            MethodDeclaration(name : "CommitTransaction", returnType : "void", isThereBody: false),
            MethodDeclaration(name : "RollbackTransaction", returnType : "void", isThereBody: false),

            MethodDeclaration(
                name: "SaveChangesAsync",
                returnType: "Task<int>",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                isThereBody: false
            ),
            MethodDeclaration(
                name: "BeginTransactionAsync",
                returnType: "Task",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                isThereBody: false
            ),
            MethodDeclaration(
                name: "CommitTransactionAsync",
                returnType: "Task",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                isThereBody: false
            ),
            MethodDeclaration(
                name: "RollbackTransactionAsync",
                returnType : "Task",
                parameters: [ParameterDeclaration("CancellationToken", "cancellationToken", false)],
                isThereBody: false
            )
        };

        return CompilationUnit(
            usings: [$"{_appSetting.DataAccessLayerProjectName}.Abstract"],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.DataAccessLayerProjectName}.UoW",
                members: [
                    InterfaceDeclaration(
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
    public string GenerateContext()
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);
        string IdentityKeyType = identityTypeConfigs.IdentityKeyType;
        string IdentityUserType = identityTypeConfigs.IdentityUserType;
        string IdentityRoleType = identityTypeConfigs.IdentityRoleType;

        #region DbSets
        var dbSets = entities.Select(e =>
            _appSetting.IsThereIdentity && ((e.Id == _appSetting.UserEntityId && e.Name == "User") || (e.Id == _appSetting.RoleEntityId && e.Name == "Role")) ?
                PropertyDeclaration($"override DbSet<{e.Name}>", e.Name.Pluralize(), true) :
                PropertyDeclaration($"DbSet<{e.Name}>", e.Name.Pluralize(), true)
        ).ToList();
        if (_appSetting.IsThereIdentity)
            dbSets.Add(PropertyDeclaration("DbSet<RefreshToken>", "RefreshTokens", true));
        dbSets.Add(PropertyDeclaration("DbSet<Log>", "Logs", true));
        dbSets.Add(PropertyDeclaration("DbSet<Archive>", "Archives", true));
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
                if (relation.RelationTypeId == (int)RelationTypeEnums.OneToOne)
                {
                    statements.Add(RelationOneToOne(eSc, f_eSc, relation));
                }
                else if (relation.RelationTypeId == (int)RelationTypeEnums.OneToMany)
                {
                    statements.Add(RelationOneToMany(eSc, f_eSc, relation));
                }
            }

            if (_appSetting.IsThereUser && entity.Id == _appSetting.UserEntityId)
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

        if (_appSetting.IsThereIdentity)
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

        if (_appSetting.IsThereIdentity)
        {
            if (!_appSetting.IsThereUser)
            {
                modelBuilders.Add(SyntaxFactory.ParseStatement($"modelBuilder.Entity<IdentityUser<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"Users\"); }});"));
            }

            if (!_appSetting.IsThereRole)
            {
                Dictionary<string, string> defaultRoles = new Dictionary<string, string>
                {
                    {
                        "User",
                        IdentityKeyType == "int" ? "1" :
                            IdentityKeyType == "string" ? "b370875e-34cd-4b79-891c-93ae38f99d11" :
                            "new Guid(\"b370875e-34cd-4b79-891c-93ae38f99d11\")"
                    },
                    {
                        "Manager",
                        IdentityKeyType == "int" ? "2" :
                            IdentityKeyType == "string" ? "cd6040ef-dacc-4678-9a85-154f12581cff" :
                                "new Guid(\"cd6040ef-dacc-4678-9a85-154f12581cff\")"
                    },
                    {
                        "Admin",
                        IdentityKeyType == "int" ? "3" :
                            IdentityKeyType == "string" ? "7138ec51-4f9e-4afd-b61b-5a9a4584f5da" :
                                "new Guid(\"7138ec51-4f9e-4afd-b61b-5a9a4584f5da\")"
                    },
                    {
                        "Owner",
                        IdentityKeyType == "int" ? "4" :
                            IdentityKeyType == "string" ? "1f20c152-530e-4064-a39c-bbbed341fe84" :
                                "new Guid(\"1f20c152-530e-4064-a39c-bbbed341fe84\")"
                    }
                };

                string defaultRolesString = string.Join(",\n", defaultRoles.Select(r =>
                    $@"new IdentityRole<{IdentityKeyType}>
                    {{
                        Id = {r.Value},
                        Name = ""{r.Key}"",
                        NormalizedName = ""{r.Key.ToUpperInvariant()}"",
                        ConcurrencyStamp = ""{r.Value}.ToString()""
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

        var context = CompilationUnit(
            usings: [
                "Microsoft.AspNetCore.Identity",
                "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
                "Microsoft.EntityFrameworkCore",
                $"{_appSetting.ModelLayerProjectName}.Entities",
                $"{_appSetting.ModelLayerProjectName}.ProjectEntities"
            ],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.DataAccessLayerProjectName}.Contexts",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword],
                        name: "AppDbContext",
                        baseTypes: _appSetting.IsThereIdentity
                            ? [SyntaxFactory.ParseTypeName($"IdentityDbContext<{IdentityUserType}, {IdentityRoleType}, {IdentityKeyType}>")]
                            : [SyntaxFactory.ParseTypeName("DbContext")],
                        members: [
                            ConstructorDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword],
                                name: "AppDbContext",
                                parameters: [ParameterDeclaration("DbContextOptions<AppDbContext>", "options")],
                                baseArgs: ["options"]
                            ),
                            ..dbSets,
                            MethodDeclaration(
                                modifiers: [SyntaxKind.ProtectedKeyword, SyntaxKind.OverrideKeyword],
                                name: "OnModelCreating",
                                returnType: "void",
                                parameters: [ParameterDeclaration("ModelBuilder", "modelBuilder")],
                                block: SyntaxFactory.Block(modelBuilders)
                            )
                        ]
                    )
                ]
            )
        ).ToFullString();

        string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.DataAccessLayerProjectName, "Contexts");
        results.Add(AddFile(folderPath, "AppDbContext.cs", context));

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
    public string GenerateServiceRegistration()
    {
        var sb = new StringBuilder();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        foreach (var entity in entities)
            sb.AppendLine($"services.AddScoped<I{entity.Name}Repository, {entity.Name}Repository>();");
        if (_appSetting.IsThereIdentity)
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

        var code = CompilationUnit(
            usings: [
                $"{_appSetting.DataAccessLayerProjectName}.Abstract",
                $"{_appSetting.DataAccessLayerProjectName}.Concrete",
                $"{_appSetting.DataAccessLayerProjectName}.Contexts",
                $"{_appSetting.DataAccessLayerProjectName}.Interceptors",
                $"{_appSetting.DataAccessLayerProjectName}.UoW",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.DependencyInjection",
            ],
            nspace: NamespaceDeclaration(
                value: $"{_appSetting.DataAccessLayerProjectName}",
                members: [
                    ClassDeclaration(
                        modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
                        name: "ServiceRegistration",
                        members: [
                            MethodDeclaration(
                                modifiers: [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
                                name: "AddDataAccessServices",
                                returnType: "IServiceCollection",
                                parameters: [
                                    ParameterDeclaration(modifiers: [SyntaxKind.ThisKeyword], type: "IServiceCollection", name: "services"),
                                    ParameterDeclaration(type: "IConfiguration", name: "configuration")
                                ],
                                body: sb.ToString()
                            )
                        ]
                    )
                ]
             )
         );

        string folderPath = Path.Combine(_appSetting.SolutionPath, _appSetting.DataAccessLayerProjectName);
        return AddFile(folderPath, "ServiceRegistration.cs", code.ToFullString());
    }
    #endregion
}
