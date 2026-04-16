using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GeneratorWPF.CodeGenerators.NLayer.API;

public partial class RoslynApiControllerGenerator
{
    private readonly AppSetting _appSetting;
    public RoslynApiControllerGenerator(AppSetting appSetting)
    {
        _appSetting = appSetting;
    }

    public string GeneraterController(Entity entity, List<Dto> dtos)
    {
        string controllerName = $"{entity.Name}Controller";
        string serviceTypeName = $"I{entity.Name}Service";
        string serviceArgName = $"{entity.Name.ToCamelCase()}Service";
        string serviceName = $"_{entity.Name.ToCamelCase()}Service";

        List<Field> uniqueFields = entity.Fields.Where(f => f.IsUnique).ToList();

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>();

        if (_appSetting.IsThereIdentity) attributeList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Authorize")));
        attributeList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ApiController")));
        attributeList.Add(
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("Route"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal("api/[controller]")
                            )
                        )
                    )
                )
            )
        );


        // 2) Field List
        var fieldList = new List<FieldDeclarationSyntax>
        {
            CreateReadOnlyField(serviceTypeName, serviceName)
        };


        // 3) Constructor
        var constructor = SyntaxFactory.ConstructorDeclaration(controllerName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(serviceArgName)).WithType(SyntaxFactory.ParseTypeName(serviceTypeName))
            )
            .WithExpressionBody(
                SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(serviceName),
                        SyntaxFactory.IdentifierName(serviceArgName)
                    )
                )
            )
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        // 4) Method List
        var methodList = new List<MethodDeclarationSyntax>();

        #region Get
        methodList.Add(GeneratorGetMethod("Get", serviceName, "GetAsync", uniqueFields));
        methodList.Add(GeneratorGetAllMethod("GetAll", serviceName, "GetAllAsync"));
        methodList.Add(GeneratorGetListMethod("GetList", serviceName, "GetListAsync"));
        #endregion

        #region GetBasic
        var basicResponseDto = dtos.FirstOrDefault(f => f.Id == entity.BasicResponseDtoId);
        bool isThereBasicResponseDto = basicResponseDto != null;
        if (isThereBasicResponseDto)
        {
            methodList.Add(GeneratorGetMethod("GetByBasic", serviceName, "GetByBasicAsync", uniqueFields));
            methodList.Add(GeneratorGetAllMethod("GetAllByBasic", serviceName, "GetAllByBasicAsync"));
            methodList.Add(GeneratorGetListMethod("GetListByBasic", serviceName, "GetListByBasicAsync"));
        }
        #endregion

        #region GetDetail
        var detailResponseDto = dtos.FirstOrDefault(f => f.Id == entity.DetailResponseDtoId);
        if (detailResponseDto != null)
        {
            methodList.Add(GeneratorGetMethod("GetByDetail", serviceName, "GetByDetailAsync", uniqueFields));
            methodList.Add(GeneratorGetAllMethod("GetAllByDetail", serviceName, "GetAllByDetailAsync"));
            methodList.Add(GeneratorGetListMethod("GetListByDetail", serviceName, "GetListByDetailAsync"));
        }
        #endregion

        #region Other Read Dtos
        var readResponseDtos = dtos.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read && f.Id != entity.BasicResponseDtoId && f.Id != entity.DetailResponseDtoId);
        if (readResponseDtos != null && readResponseDtos.Any())
        {
            foreach (var readDto in readResponseDtos)
            {
                string subModelName = readDto.Name.EndsWith("Dto") ? readDto.Name.Substring(0, readDto.Name.Length - 3) : readDto.Name;

                methodList.Add(GeneratorGetMethod($"Get{subModelName}", serviceName, $"Get{readDto.Name}Async", uniqueFields));
                methodList.Add(GeneratorGetAllMethod($"GetAll{subModelName}", serviceName, $"GetAll{readDto.Name}Async"));
                methodList.Add(GeneratorGetListMethod($"GetList{subModelName}", serviceName, $"GetList{readDto.Name}Async"));
            }
        }
        #endregion

        #region Create
        methodList.Add(GeneratorCreateMethod(serviceName, entity, dtos));
        #endregion

        #region Update
        methodList.Add(GeneratorUpdateMethod(serviceName, entity, dtos));
        #endregion

        #region Delete
        methodList.Add(GeneratorDeleteMethod(serviceName, entity, dtos, uniqueFields));
        #endregion

        #region Datatable Methods
        methodList.Add(GenerateDatatableClientSideMethod("DatatableClientSide", serviceName, "DatatableClientSideAsync"));
        methodList.Add(GenerateDatatableServerSideMethod("DatatableServerSide", serviceName, "DatatableServerSideAsync"));

        Dto? reportDto = entity.ReportDtoId != default ? dtos.FirstOrDefault(f => f.Id == entity.ReportDtoId) : default;
        if (reportDto != default)
        {
            methodList.Add(GenerateDatatableClientSideMethod("DatatableClientSideReport", serviceName, "DatatableClientSideByReportAsync"));
            methodList.Add(GenerateDatatableServerSideMethod("DatatableServerSideReport", serviceName, "DatatableServerSideByReportAsync"));
        }
        #endregion



        // 5) Class
        var classDeclaration = SyntaxFactory
            .ClassDeclaration(controllerName)
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList)))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ControllerBase")))
            .AddMembers(fieldList.ToArray())
            .AddMembers(constructor)
            .AddMembers(methodList.ToArray());

        // 6) NameSpace
        var namespaceDeclaration = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName("WebAPI.Controllers"))
            .AddMembers(classDeclaration);

        // 7) Compilation Unit
        var usings = new List<UsingDirectiveSyntax>()
        {
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Business.Abstract")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Core.BaseRequestModels")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Authorization")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc")),
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Model.Entities"))
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


    #region Action Generators
    private MethodDeclarationSyntax GeneratorGetMethod(string actionName, string serviceName, string methodName, List<Field> uniqueFields)
    {
        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpGet"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(actionName)
                            )
                        )
                    )
                )
            )
        };

        // 2) Parameters
        var paramList = new List<ParameterSyntax>();
        foreach (var field in uniqueFields)
        {
            paramList.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                    .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
            );
        }

        // 3) Arguments of method call
        var argumentsOfMethod = new List<ArgumentSyntax>();
        foreach (var field in uniqueFields)
        {
            argumentsOfMethod.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(field.Name.ToCamelCase())));
        }

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.{methodName}")
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
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))));


        // 7) NotFound Contition
        IfStatementSyntax ifStatementNotFound = CreateIfNotFoundCondition("result");


        // 8) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier(actionName)
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
                    ifStatementNotFound,
                    returnOkStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorGetAllMethod(string actionName, string serviceName, string methodName)
    {
        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpPost"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(actionName)
                            )
                        )
                    )
                )
            )
        };

        // 2) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.NullableType(SyntaxFactory.IdentifierName("DynamicRequest")))
        };

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
        };

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.{methodName}")
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
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))));

        // 7) NotFound Contition
        IfStatementSyntax ifStatementNotFound = CreateIfNotFoundCondition("result");


        // 8) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier(actionName)
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
                    ifStatementNotFound,
                    returnOkStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorGetListMethod(string actionName, string serviceName, string methodName)
    {
        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpPost"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(actionName)
                            )
                        )
                    )
                )
            )
        };

        // 2) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicPaginationRequest"))
        };

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
        };

        // 3) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.{methodName}")
                )
                .AddArgumentListArguments(arguments.ToArray()));

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
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))));

        // 6) NotFound Contition
        IfStatementSyntax ifStatementNotFound = CreateIfNotFoundCondition("result");

        // 7) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier(actionName)
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
                    ifStatementNotFound,
                    returnOkStatement
                )
            );
    }


    private MethodDeclarationSyntax GeneratorCreateMethod(string serviceName, Entity entity, List<Dto> dtos)
    {
        var createDto = dtos.FirstOrDefault(f => f.Id == entity.CreateDtoId);
        bool isThereCreateDto = createDto != null;

        string argType = isThereCreateDto ? createDto!.Name : entity.Name;

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpPost"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal("Create")
                            )
                        )
                    )
                )
            )
        };

        // 2) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName(argType))
        };

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
        };

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.CreateAsync")
                )
                .AddArgumentListArguments(arguments.ToArray()));

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
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))));

        // 7) Method Decleration
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
                    returnOkStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorUpdateMethod(string serviceName, Entity entity, List<Dto> dtos)
    {
        var updateDto = dtos.FirstOrDefault(f => f.Id == entity.UpdateDtoId);
        bool isThereUpdateDto = updateDto != null;

        string argType = isThereUpdateDto ? updateDto!.Name : entity.Name;


        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpPatch"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal("Update")
                            )
                        )
                    )
                )
            )
        };

        // 2) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName(argType))
        };

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
        };

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.UpdateAsync")
                )
                .AddArgumentListArguments(arguments.ToArray()));

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
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))));

        // 7) Method Decleration
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
                    returnOkStatement
                )
            );
    }

    private MethodDeclarationSyntax GeneratorDeleteMethod(string serviceName, Entity entity, List<Dto> dtos, List<Field> uniqueFields)
    {
        var deleteDto = dtos.FirstOrDefault(f => f.Id == entity.DeleteDtoId);
        bool isThereDeleteDto = deleteDto != null;

        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpDelete"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal("Delete")
                            )
                        )
                    )
                )
            )
        };

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
                paramList.Add(SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier(field.Name.ToCamelCase()))
                        .WithType(SyntaxFactory.IdentifierName(field.GetMapedTypeName()))
                );
            }
        }

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>();
        if (isThereDeleteDto)
        {
            arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request")));
        }
        else
        {
            foreach (var field in uniqueFields)
            {
                arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(field.Name.ToCamelCase())));
            }
        }

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.DeleteAsync")
                )
                .AddArgumentListArguments(arguments.ToArray()));

        // 5) Method Call Decleration by Result
        var methodCallDecleration = SyntaxFactory.ExpressionStatement(awaitInvocation);

        // 6) Return Statement
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok")));

        // 7) Method Decleration
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
            .WithBody(
                SyntaxFactory.Block(
                    methodCallDecleration,
                    returnOkStatement
                )
            );
    }


    private MethodDeclarationSyntax GenerateDatatableClientSideMethod(string actionName, string serviceName, string methodName)
    {
        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpPost"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(actionName)
                            )
                        )
                    )
                )
            )
        };

        // 2) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicRequest"))
        };

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
        };

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.{methodName}")
                )
                .AddArgumentListArguments(arguments.ToArray()));

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
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))));

        // 7) NotFound Contition
        IfStatementSyntax ifStatementNotFound = CreateIfNotFoundCondition("result");

        // 8) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier(actionName)
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
                    ifStatementNotFound,
                    returnOkStatement
                )
            );
    }

    private MethodDeclarationSyntax GenerateDatatableServerSideMethod(string actionName, string serviceName, string methodName)
    {
        // 1) Attribute List
        List<AttributeSyntax> attributeList = new List<AttributeSyntax>()
        {
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("HttpPost"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(actionName)
                            )
                        )
                    )
                )
            )
        };

        // 2) Parameters
        var paramList = new List<ParameterSyntax>()
        {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.IdentifierName("DynamicDatatableServerSideRequest"))
        };

        // 3) Arguments of method call
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("request"))
        };

        // 4) Method Call
        var awaitInvocation =
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"{serviceName}.{methodName}")
                )
                .AddArgumentListArguments(arguments.ToArray()));

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
        var returnOkStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Ok"))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("result"))));

        // 7) NotFound Contition
        IfStatementSyntax ifStatementNotFound = CreateIfNotFoundCondition("result");

        // 8) Method Decleration
        return SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName("Task<IActionResult>"),
                SyntaxFactory.Identifier(actionName)
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
                    ifStatementNotFound,
                    returnOkStatement
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
    private IfStatementSyntax CreateIfNotFoundCondition(string argName)
    {
        var condition = SyntaxFactory.BinaryExpression(
            SyntaxKind.EqualsExpression,
            SyntaxFactory.IdentifierName(argName),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
        );

        var notFound =
            SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName("NotFound")
                    )
                )
            );
        return SyntaxFactory.IfStatement(
            condition,
            notFound
        );
    }
    #endregion
}