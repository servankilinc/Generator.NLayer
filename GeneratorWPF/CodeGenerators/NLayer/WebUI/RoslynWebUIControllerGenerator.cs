using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace GeneratorWPF.CodeGenerators.NLayer.WebUI;

public class RoslynWebUIControllerGenerator
{
    private readonly AppSetting _appSetting;
    private readonly DtoRepository _dtoRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly RelationRepository _relationRepository;
    public RoslynWebUIControllerGenerator(AppSetting appSetting)
    {
        _appSetting = appSetting;
        _dtoRepository = new();
        _fieldRepository = new();
        _relationRepository = new();
    }

    public string GeneraterController(Entity entity, List<Dto> dtos)
    {
        string controllerName = $"{entity.Name}Controller";
        string serviceTypeName = $"I{entity.Name}Service";
        string serviceArgName = $"{entity.Name.ToCamelCase()}Service";
        string serviceName = $"_{entity.Name.ToCamelCase()}Service";


        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).ToList();


        Dictionary<string, string> selectableRelations = new Dictionary<string, string>();  // fieldName, primaryRelationEntityName
        List<Entity> entitiesToInjection = new List<Entity>();
        List<Field> filterableFields = _fieldRepository.GetAll(filter: f => f.EntityId == entity.Id && f.Filterable, include: i => i.Include(x => x.FieldType));
        foreach (var field in filterableFields)
        {
            Relation? relation = _relationRepository.Get(
              filter: f => f.ForeignFieldId == field.Id && f.RelationTypeId == (byte)RelationTypeEnums.OneToMany,
              include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity));

            if (relation == null) continue;

            selectableRelations.Add(field.Name, relation.PrimaryField.Entity.Name);
            entitiesToInjection.Add(relation.PrimaryField.Entity);
        }


        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>();
        if (_appSetting.IsThereIdentiy) attributeList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Authorize")));


        // 2) Field List
        var fieldList = new List<FieldDeclarationSyntax>
        {
            CreateReadOnlyField(serviceTypeName, serviceName)
        };

        foreach (var entityToInj in entitiesToInjection)
        {
            fieldList.Add(CreateReadOnlyField($"I{entityToInj.Name}Service", $"_{entityToInj.Name.ToCamelCase()}Service"));
        }


        // 3) Constructor args
        List<ParameterSyntax> parameterSyntaxes = new List<ParameterSyntax>
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(serviceArgName)).WithType(SyntaxFactory.ParseTypeName(serviceTypeName))
        };

        foreach (var entityToInj in entitiesToInjection)
        {
            parameterSyntaxes.Add(SyntaxFactory.Parameter(
                SyntaxFactory.Identifier($"{entityToInj.Name.ToCamelCase()}Service"))
                    .WithType(SyntaxFactory.ParseTypeName($"I{entityToInj.Name}Service")));
        }

        // 4) Constructor Statements
        List<ExpressionStatementSyntax> statements = new List<ExpressionStatementSyntax>()
        {
            SyntaxFactory.ExpressionStatement(
               SyntaxFactory.AssignmentExpression(
                   SyntaxKind.SimpleAssignmentExpression,
                   SyntaxFactory.IdentifierName(serviceName),
                   SyntaxFactory.IdentifierName(serviceArgName)
               )
           )
        };

        foreach (var entityToInj in entitiesToInjection)
        {
            statements.Add(
                SyntaxFactory.ExpressionStatement(
                   SyntaxFactory.AssignmentExpression(
                       SyntaxKind.SimpleAssignmentExpression,
                       SyntaxFactory.IdentifierName($"_{entityToInj.Name.ToCamelCase()}Service"),
                       SyntaxFactory.IdentifierName($"{entityToInj.Name.ToCamelCase()}Service")
                   )
               )
            );
        }

        // 5) Constructor
        var constructor = SyntaxFactory.ConstructorDeclaration(controllerName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(parameterSyntaxes.ToArray())
            .WithBody(SyntaxFactory.Block(statements.ToArray())
        );


        // 6) Method List
        var methodList = new List<MethodDeclarationSyntax>
        {
            // Index
            GenerateActionIndex(entity, selectableRelations),

            // Create
            GenerateActionCreateGet(entity, selectableRelations),
            GenerateActionCreateForm(entity, selectableRelations),
            GenerateActionCreatePost(entity),
            
            // Update
            GenerateActionUpdateForm(entity, selectableRelations, uniqueFields),
            GenerateActionUpdatePost(entity),

            // Delete
            GenerateActionDeletePost(entity, uniqueFields),
            GenerateActionUndoDelete(entity, uniqueFields),

            // Datatable
            GenerateActionDatatableServerSide(entity)
        };


        // 7) Class
        var classDeclaration = SyntaxFactory
            .ClassDeclaration(controllerName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Controller")))
            .AddMembers(fieldList.ToArray())
            .AddMembers(constructor)
            .AddMembers(methodList.ToArray());

        if (attributeList.Count > 0)
            classDeclaration = classDeclaration.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)));

        // 8) NameSpace
        var namespaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName("WebUI.Controllers"))
            .AddMembers(classDeclaration);

        // 9) Compilation Unit
        var usings = new List<UsingDirectiveSyntax>()
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Business.Abstract")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.BaseRequestModels")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.Utils.Datatable")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("WebUI.Utils.ActionFilters")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Entities")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Authorization")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"WebUI.Models.ViewModels.{entity.Name}_"))
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


    #region Index Method
    private MethodDeclarationSyntax GenerateActionIndex(Entity entity, Dictionary<string, string> selectableRelations)
    {
        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))
        };


        // 2) Selecting Call List
        var selectListCallings = new List<ExpressionSyntax>();

        foreach (var relation in selectableRelations)
        {
            string entityName = relation.Value;
            string fieldName = relation.Key.ToForeignFieldSlectListName(entityName);

            selectListCallings.Add(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.AwaitExpression(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName($"_{entityName.Trim().ToCamelCase()}Service"),
                                SyntaxFactory.IdentifierName("GetSelectListAsync")
                            )
                        )
                    )
                )
            );
        }

        // 3) Instance Decleration
        var objectCreationExpression =
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName($"{entity.Name}ViewModel"))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(JoinWithCommas(selectListCallings))
                    )
                );

        // 4) ViewModel Instance Decleration by Result
        var viewModelInstanceDecleration =
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("viewModel"))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(objectCreationExpression))
                        )
                    )
            );

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("View"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("viewModel"))
                    )
            );


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("Index")
            )
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .WithBody(
                SyntaxFactory.Block(
                    viewModelInstanceDecleration,
                    returnViewStatement
                )
            );
    }
    #endregion

    #region Create Methods
    private MethodDeclarationSyntax GenerateActionCreateGet(Entity entity, Dictionary<string, string> selectableRelations)
    {
        Dto? createDto = entity.CreateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.CreateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereCreateDto = createDto != default;

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))
        };


        // 2) Selecting Call List
        var selectListCallings = new List<ExpressionSyntax>();

        foreach (var relation in selectableRelations)
        {
            string entityName = relation.Value;
            string fieldName = relation.Key.ToForeignFieldSlectListName(entityName);

            // dto içerisnde yoksa boşuna eklenmemeli
            if (isThereCreateDto && createDto!.DtoFields.Any(f => f.SourceField.Name.Trim().ToLowerInvariant() == relation.Key.Trim().ToLowerInvariant()) == false)
                continue;


            selectListCallings.Add(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.AwaitExpression(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName($"_{entityName.Trim().ToCamelCase()}Service"),
                                SyntaxFactory.IdentifierName("GetSelectListAsync")
                            )
                        )
                    )
                )
            );
        }

        // 3) Instance Decleration
        var objectCreationExpression =
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName($"{entity.Name}CreateViewModel"))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(JoinWithCommas(selectListCallings))
                    )
                );

        // 4) ViewModel Instance Decleration by Result
        var viewModelInstanceDecleration =
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("viewModel"))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(objectCreationExpression))
                        )
                    )
            );

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("View"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("viewModel"))
                    )
            );


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("Create")
            )
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .WithBody(
                SyntaxFactory.Block(
                    viewModelInstanceDecleration,
                    returnViewStatement
                )
            );
    }

    private MethodDeclarationSyntax GenerateActionCreateForm(Entity entity, Dictionary<string, string> selectableRelations)
    {
        Dto? createDto = entity.CreateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.CreateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereCreateDto = createDto != default;

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))
        };

        // 2) Selecting Call List
        var selectListCallings = new List<ExpressionSyntax>();

        foreach (var relation in selectableRelations)
        {
            string entityName = relation.Value;
            string fieldName = relation.Key.ToForeignFieldSlectListName(entityName);

            // dto içerisnde yoksa boşuna eklenmemeli
            if (isThereCreateDto && createDto!.DtoFields.Any(f => f.SourceField.Name.Trim().ToLowerInvariant() == relation.Key.Trim().ToLowerInvariant()) == false)
                continue;

            selectListCallings.Add(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.AwaitExpression(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName($"_{entityName.Trim().ToCamelCase()}Service"),
                                SyntaxFactory.IdentifierName("GetSelectListAsync")
                            )
                        )
                    )
                )
            );
        }

        // 3) Instance Decleration
        var objectCreationExpression =
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName($"{entity.Name}CreateViewModel"))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(JoinWithCommas(selectListCallings))
                    )
                );

        // 4) ViewModel Instance Decleration by Result
        var viewModelInstanceDecleration =
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("viewModel"))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(objectCreationExpression))
                        )
                    )
            );

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("PartialView"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("\"./Partials/CreateForm\"")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("viewModel"))
                    )
            );


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("CreateForm")
            )
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .WithBody(
                SyntaxFactory.Block(
                    viewModelInstanceDecleration,
                    returnViewStatement
                )
            );
    }

    private MethodDeclarationSyntax GenerateActionCreatePost(Entity entity)
    {
        Dto? createDto = entity.CreateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.CreateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereCreateDto = createDto != default;

        string requstModelType = isThereCreateDto ? createDto!.Name : entity.Name;
        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpPost"))
        };

        if (isThereCreateDto) attributeList.Add(
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("ServiceFilter"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(
                                SyntaxFactory.IdentifierName($"ValidationFilter<{requstModelType}>")
                            )
                        )
                    )
                )
            )
        );

        // 2) Parameters of Action
        var paramList = new List<ParameterSyntax>
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("createModel"))
                .WithType(SyntaxFactory.IdentifierName(requstModelType))
        };

        // 3) Arguments of method call
        var argumentsOfMethod = new List<ArgumentSyntax>()
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("createModel"))
        };


        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.CreateAsync")
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

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))
                    )
            );


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("Create")
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
                    returnViewStatement
                )
            );
    }
    #endregion

    #region Update Methods
    private MethodDeclarationSyntax GenerateActionUpdateForm(Entity entity, Dictionary<string, string> selectableRelations, List<Field> uniqueFields)
    {
        Dto? updateDto = entity.UpdateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.UpdateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereUpdateDto = updateDto != default;

        string serviceName = $"_{entity.Name.Trim().ToCamelCase()}Service"; 

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpGet"))
        };

        // 2) Parameters of Action
        var paramList = new List<ParameterSyntax>();
        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }

        // 3) Arguments of get original method call
        var arguments = new List<ArgumentSyntax>
        {
            CreateWhereRule(uniqueFields)
        };

        // 4) Method get original Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    isThereUpdateDto ?
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier($"{serviceName}.GetAsync"))
                            .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName(updateDto!.Name))
                        :
                        SyntaxFactory.IdentifierName($"{serviceName}.GetAsync")
                )
                .AddArgumentListArguments(arguments.ToArray())
            );

        // 5) Method Call get original Decleration by Result
        var methodCallDecleration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("data"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(awaitInvocation))
                    )
                )
            );

        // 6) NotFound original data
        IfStatementSyntax ifDataNotFoundStatement = CreateNotFoundReturn("data");


        // 7) ViewModel Prop List
        var viewModelProps = new List<ExpressionSyntax>()
        {
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("UpdateModel"), 
                SyntaxFactory.IdentifierName("data")
            )
        };
        

        foreach (var relation in selectableRelations)
        {
            string entityName = relation.Value;
            string fieldName = relation.Key.ToForeignFieldSlectListName(entityName);

            // dto içerisnde yoksa boşuna eklenmemeli
            if (isThereUpdateDto && updateDto!.DtoFields.Any(f => f.SourceField.Name.Trim().ToLowerInvariant() == relation.Key.Trim().ToLowerInvariant()) == false)
                continue;


            viewModelProps.Add(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.AwaitExpression(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName($"_{entityName.Trim().ToCamelCase()}Service"),
                                SyntaxFactory.IdentifierName("GetSelectListAsync")
                            )
                        )
                    )
                )
            );
        }

        // 8) Instance Decleration
        var objectCreationExpression =
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName($"{entity.Name}UpdateViewModel"))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(JoinWithCommas(viewModelProps))
                    )
                );

        // 9) ViewModel Instance Decleration by Result
        var viewModelInstanceDecleration =
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("viewModel"))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(objectCreationExpression))
                        )
                    )
            );

        // 10) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("PartialView"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("\"./Partials/UpdateForm\"")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("viewModel"))
                    )
            );


        // 11) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("UpdateForm")
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
                    ifDataNotFoundStatement,
                    viewModelInstanceDecleration,
                    returnViewStatement
                )
            );
    }

    private MethodDeclarationSyntax GenerateActionUpdatePost(Entity entity)
    {
        Dto? updateDto = entity.UpdateDtoId != default ? _dtoRepository.Get(f => f.Id == entity.UpdateDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereUpdateDto = updateDto != default;

        string requstModelType = isThereUpdateDto? updateDto!.Name : entity.Name;
        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpPost"))
        };

        if (isThereUpdateDto) attributeList.Add(
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("ServiceFilter"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(
                                SyntaxFactory.IdentifierName($"ValidationFilter<{requstModelType}>")
                            )
                        )
                    )
                )
            )
        );

        // 2) Parameters of Action
        var paramList = new List<ParameterSyntax>
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("updateModel"))
                .WithType(SyntaxFactory.IdentifierName(requstModelType))
        };

        // 3) Arguments of method call
        var argumentsOfMethod = new List<ArgumentSyntax>()
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("updateModel"))
        };


        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.UpdateAsync")
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

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))
                    )
            );


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("Update")
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
                    returnViewStatement
                )
            );
    }
    #endregion

    #region Delete Methods
    private MethodDeclarationSyntax GenerateActionDeletePost(Entity entity, List<Field> uniqueFields)
    {
        Dto? deleteDto = entity.DeleteDtoId != default ? _dtoRepository.Get(f => f.Id == entity.DeleteDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereDeleteDto = deleteDto != default;

        string requstModelType = isThereDeleteDto ? deleteDto!.Name : entity.Name;
        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpDelete"))
        };

        if (isThereDeleteDto) attributeList.Add(
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("ServiceFilter"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.TypeOfExpression(
                                SyntaxFactory.IdentifierName($"ValidationFilter<{requstModelType}>")
                            )
                        )
                    )
                )
            )
        );

        // 2) Parameters of Action
        var paramList = new List<ParameterSyntax>();

        if (isThereDeleteDto)
        {
            paramList.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("deleteModel")).WithType(SyntaxFactory.IdentifierName(requstModelType)));
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

        // 3) If Null Statements
        List<IfStatementSyntax> ifStatements = new();
        if (!isThereDeleteDto)
        {
            foreach (var field in uniqueFields)
            {
                ifStatements.Add(CreateIfDefaultCheckCondition(field.Name.ToCamelCase()));
            }
        }

        // 4) Arguments of method call
        var argumentsOfMethod = new List<ArgumentSyntax>();
        if (isThereDeleteDto)
        {
            argumentsOfMethod.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("deleteModel")));
        }
        else
        {
            foreach (var field in uniqueFields)
            {
                argumentsOfMethod.Add(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(field.Name.ToCamelCase()))
                );
            }
        }
        
        // 4) Method Call
        var awaitInvocation =
                SyntaxFactory.AwaitExpression(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName($"{serviceName}.DeleteAsync")
                    )
                    .AddArgumentListArguments(argumentsOfMethod.ToArray())
                );

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.ExpressionStatement(awaitInvocation);

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
            );


        // 6) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.AddRange(ifStatements);
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnViewStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("Delete")
            )
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }
    #endregion

    #region UndoDelete Methods
    private MethodDeclarationSyntax GenerateActionUndoDelete(Entity entity, List<Field> uniqueFields)
    {
        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        // 1) Parameters of Action
        var paramList = new List<ParameterSyntax>(); 
        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }

        // 2) If Null Statements
        List<IfStatementSyntax> ifStatements = new();
        foreach (var field in uniqueFields)
        {
            ifStatements.Add(CreateIfDefaultCheckCondition(field.Name.ToCamelCase()));
        }

        // 3) Arguments of method call
        var argumentsOfMethod = new List<ArgumentSyntax>();
        foreach (var field in uniqueFields)
        {
            argumentsOfMethod.Add(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(field.Name.ToCamelCase()))
            );
        }

        // 4) Method Call
        var awaitInvocation =
                SyntaxFactory.AwaitExpression(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName($"{serviceName}.UndoDeleteAsync")
                    )
                    .AddArgumentListArguments(argumentsOfMethod.ToArray())
                );

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.ExpressionStatement(awaitInvocation);

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
            );


        // 6) Method Decleration
        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.AddRange(ifStatements);
        bodyStatements.Add(methodCallDecleration);
        bodyStatements.Add(returnViewStatement);

        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("UndoDelete")
            )
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(bodyStatements));
    }
    #endregion

    #region Datatable Methods
    private MethodDeclarationSyntax GenerateActionDatatableServerSide(Entity entity)
    {
        Dto? reportDto = entity.ReportDtoId != default ? _dtoRepository.Get(f => f.Id == entity.ReportDtoId, include: i => i.Include(x => x.DtoFields).ThenInclude(x => x.SourceField)) : default;
        bool isThereReportDto = reportDto != default;
         
        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("HttpPost"))
        };

        // 2) Parameters of Action
        var paramList = new List<ParameterSyntax>
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicDatatableServerSideRequest"))
        }; 

        // 3) Arguments of method call
        var argumentsOfMethod = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
        };        

        // 4) Method Call
        var awaitInvocation =
                SyntaxFactory.AwaitExpression(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName(
                            isThereReportDto ? 
                                $"{serviceName}.DatatableServerSideByReportAsync" : 
                                $"{serviceName}.DatatableServerSideAsync"
                        )
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

        // 5) Return Statement
        var returnViewStatement =
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))
                    )
            );


        // 6) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier("DatatableServerSide")
            )
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            )
            .AddParameterListParameters(paramList.ToArray())
            .WithBody(SyntaxFactory.Block(
                methodCallDecleration,
                returnViewStatement
                )
            );
    }
    #endregion


    #region Helpers
    private FieldDeclarationSyntax CreateReadOnlyField(string type, string name)
    {
        return SyntaxFactory
            .FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(type)
            )
            .AddVariables(SyntaxFactory.VariableDeclarator(name)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
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

    private IfStatementSyntax CreateNotFoundReturn(string dataName = "data")
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName(dataName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            ),
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("NotFound"))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("data"))
                        )
                    )
                )
            )
        );
    }

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
    #endregion

    public static SyntaxNodeOrToken[] JoinWithCommas(IEnumerable<ExpressionSyntax> expressions)
    {
        var list = new List<SyntaxNodeOrToken>();
        var exprArray = expressions.ToArray();

        for (int i = 0; i < exprArray.Length; i++)
        {
            list.Add(exprArray[i]);

            // Sona virgül koyma
            if (i < exprArray.Length - 1)
            {
                list.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }
        }

        return list.ToArray();
    }
}
