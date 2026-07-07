using Generator.Domain.Core;
using Generator.Domain.Core.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Domain.CodeGenerators.Services;

/// <summary>
/// Provides helper methods for constructing C# syntax trees using Roslyn.
/// Extracted from NLayerGeneratorBase.
/// </summary>
public class RoslynSyntaxHelper
{
    public ParameterSyntax ParameterDeclaration(string type, string name, bool required = true, SyntaxKind[]? modifiers = null, string? defaultValue = null)
    {
        if (required == false && !Statics.nonReferanceTypes.Contains(type) && !type.EndsWith("?"))
            type += "?";

        var parameter = SyntaxFactory
            .Parameter(SyntaxFactory.Identifier(name))
            .WithType(SyntaxFactory.ParseTypeName(type));

        if (modifiers?.Length > 0)
            parameter = parameter.AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);

        if (required == false)
            parameter = parameter.WithDefault(
                SyntaxFactory.EqualsValueClause(
                    defaultValue != null ?
                    SyntaxFactory.ParseExpression(defaultValue) :
                    SyntaxFactory.LiteralExpression(
                         SyntaxKind.DefaultLiteralExpression,
                         SyntaxFactory.Token(SyntaxKind.DefaultKeyword)
                    )
                )
            );
        else if (required == true && defaultValue != null)
            parameter = parameter.WithDefault(
                SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.ParseExpression(defaultValue)
                )
            );

        return parameter.NormalizeWhitespace();
    }

    public AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, SyntaxKind[]? modifiers = null)
    {
        var accessor = SyntaxFactory
            .AccessorDeclaration(kind)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        if (modifiers?.Length > 0)
            accessor = accessor.AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);
        return accessor.NormalizeWhitespace();
    }

    public PropertyDeclarationSyntax PropertyDeclaration(string type, string name, bool required = false, SyntaxKind[]? modifiers = null, AttributeSyntax[]? attributes = null, bool earlyInstance = false, bool nullableDecleration = true, AccessorDeclarationSyntax[]? accessors = null)
    {
        if (required == false && !type.EndsWith("?") && !Statics.nonReferanceTypes.Contains(type))
            type += "?";
        else if (required == true && type.EndsWith("?"))
            type = type.TrimEnd('?');

        var property = SyntaxFactory
            .PropertyDeclaration(SyntaxFactory.ParseTypeName(type), SyntaxFactory.Identifier(name))
            .AddModifiers(modifiers != null ? modifiers.Length == 0 ? [] : modifiers.Select(SyntaxFactory.Token).ToArray() : [SyntaxFactory.Token(SyntaxKind.PublicKeyword)]);

        if (accessors != null && accessors.Length > 0)
        {
            property = property.AddAccessorListAccessors(accessors);
        }
        else
        {
            property = property.AddAccessorListAccessors(
                SyntaxFactory
                    .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory
                    .AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            );
        }

        if (attributes?.Length > 0)
            property = property.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes)));

        if (earlyInstance == true)
        {
            property = property
                .WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(type))
                        .WithArgumentList(SyntaxFactory.ArgumentList())
                    )
                )
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }
        else if (required == true && nullableDecleration && !Statics.nonReferanceTypes.Contains(type) && Statics.IsReferanceTypeNullable(type))
        {
            property = property
                .WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.Token(SyntaxKind.EqualsToken),
                        SyntaxFactory.ParseExpression("null!")
                    )
                )
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        return property.NormalizeWhitespace();
    }

    public FieldDeclarationSyntax FieldDeclaration(SyntaxKind[] modifiers, string type, string name, bool nullable = false)
    {
        if (nullable && !Statics.nonReferanceTypes.Contains(type) && !type.EndsWith("?"))
            type += "?";

        var field = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(type))
                    .AddVariables(SyntaxFactory.VariableDeclarator(name))
        );

        if (modifiers?.Length > 0)
            field = field.AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);

        return field.NormalizeWhitespace();
    }

    public MethodDeclarationSyntax MethodDeclaration(string name, string returnType, bool isThereBody = true, SyntaxKind[]? modifiers = null, ParameterSyntax[]? parameters = null, AttributeSyntax[]? attributes = null, BlockSyntax? block = null, string? body = null)
    {
        var methodSytax = SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName(returnType), SyntaxFactory.Identifier(name));

        if (modifiers?.Length > 0)
            methodSytax = methodSytax.AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);

        if (parameters?.Length > 0)
            methodSytax = methodSytax.AddParameterListParameters(parameters);

        if (attributes?.Length > 0)
            methodSytax = methodSytax.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes)));

        if (isThereBody)
        {
            // If body is provided as a string, parse it into a statement and create a block. Otherwise, create an empty block.
            if (!string.IsNullOrWhiteSpace(body))
            {
                block = (BlockSyntax)SyntaxFactory.ParseStatement($$"""
                {
                {{body}}
                }
                """);
            }
            methodSytax = block != null ? methodSytax.WithBody(block) : methodSytax.WithBody(SyntaxFactory.Block());
        }
        else
            methodSytax = methodSytax.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        return methodSytax.NormalizeWhitespace();
    }

    public ClassDeclarationSyntax ClassDeclaration(SyntaxKind[] modifiers, string name, AttributeSyntax[]? attributes = null, TypeSyntax[]? baseTypes = null, MemberDeclarationSyntax[]? members = null, string? body = null)
    {
        var classSyntax = SyntaxFactory
            .ClassDeclaration(name)
            .AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);

        if (attributes?.Length > 0)
            classSyntax = classSyntax.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes)));
        if (baseTypes?.Length > 0)
            classSyntax = classSyntax.AddBaseListTypes(baseTypes.Select(SyntaxFactory.SimpleBaseType).ToArray());
        if (members?.Length > 0)
            classSyntax = classSyntax.AddMembers(members);
        if (body != null)
            classSyntax = classSyntax.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>([SyntaxFactory.ParseMemberDeclaration(body)!]));
        return classSyntax.NormalizeWhitespace();
    }

    public InterfaceDeclarationSyntax InterfaceDeclaration(SyntaxKind[] modifiers, string name, AttributeSyntax[]? attributes = null, TypeSyntax[]? baseTypes = null, MemberDeclarationSyntax[]? members = null)
    {
        var interfaceSyntax = SyntaxFactory
            .InterfaceDeclaration(name)
            .AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);

        if (attributes?.Length > 0)
            interfaceSyntax = interfaceSyntax.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes)));
        if (baseTypes?.Length > 0)
            interfaceSyntax = interfaceSyntax.AddBaseListTypes(baseTypes.Select(SyntaxFactory.SimpleBaseType).ToArray());
        if (members?.Length > 0)
            interfaceSyntax = interfaceSyntax.AddMembers(members);

        return interfaceSyntax.NormalizeWhitespace();
    }

    public NamespaceDeclarationSyntax NamespaceDeclaration(string value, MemberDeclarationSyntax[] members)
    {
        return SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName(value))
            .AddMembers(members) // memebers can be class, interface, enum, struct etc.
            .NormalizeWhitespace();
    }

    public CompilationUnitSyntax CompilationUnit(string[] usings, NamespaceDeclarationSyntax nspace)
    {
        return SyntaxFactory
           .CompilationUnit()
           .AddUsings(usings.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))).ToArray())
           .AddMembers(nspace)
           .NormalizeWhitespace();
    }

    public ClassDeclarationSyntax ValidatorClassDeclaration(string modelName, string[] ruleList)
    {
        var constructor = ConstructorDeclaration(
            modifiers: [SyntaxKind.PublicKeyword],
            name: $"{modelName}Validator",
            statements: ruleList.Select(rule => SyntaxFactory.ParseStatement(rule)).ToArray()
        );

        return ClassDeclaration(
            modifiers: [SyntaxKind.PublicKeyword],
            name: $"{modelName}Validator",
            baseTypes: [
                SyntaxFactory.GenericName("AbstractValidator").WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.ParseTypeName(modelName)
                        )
                    )
                )
            ],
            members: [constructor]
        ).NormalizeWhitespace();
    }

    public ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxKind[] modifiers, string name, ParameterSyntax[]? parameters = null, string[]? baseArgs = null, StatementSyntax[]? statements = null, BlockSyntax? block = null)
    {
        var constructorSyntax = SyntaxFactory
            .ConstructorDeclaration(name)
            .AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);

        if (parameters?.Length > 0)
            constructorSyntax = constructorSyntax.AddParameterListParameters(parameters);

        if (baseArgs != null)
        {
            var arrOfBaseArgs = baseArgs.Select(arg => SyntaxFactory.Argument(SyntaxFactory.ParseTypeName(arg))).ToArray();
            constructorSyntax = constructorSyntax
                .WithInitializer(
                    SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
                    .AddArgumentListArguments(arrOfBaseArgs));
        }

        constructorSyntax = constructorSyntax.WithBody(statements != null ? SyntaxFactory.Block(statements) : block != null ? block : SyntaxFactory.Block());

        return constructorSyntax.NormalizeWhitespace();
    }

    /// <summary>Generates: <c>left = right;</c></summary>
    public StatementSyntax StatementExpression(string left, string right)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.ParseTypeName(left),
                SyntaxFactory.ParseTypeName(right)
            )
        ).NormalizeWhitespace();
    }

    public LocalDeclarationStatementSyntax LocalDeclaration(string type, string name, ExpressionSyntax expression)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(type))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(expression)
                        )
                    )
                )
            ).NormalizeWhitespace();
    }

    // ex: expression: "GetDataAsync()", isAsync: true generates: await GetDataAsync();
    // ex: expression: "Calculate()", isAsync: false generates: Calculate();
    public ExpressionSyntax ExpressionStatement(string expression, bool isAsync = false)
    {
        return
            isAsync
                ? SyntaxFactory.AwaitExpression(SyntaxFactory.ParseExpression(expression)).NormalizeWhitespace()
                : SyntaxFactory.ParseExpression(expression).NormalizeWhitespace();
    }

    // ex: typeName: "Book", arguments: [MemberAccessExpression("Title", "The Great Gatsby"), MemberAccessExpression("Author", "F. Scott Fitzgerald")]
    // generates: new Book { Title = "The Great Gatsby", Author = "F. Scott Fitzgerald" }
    public ObjectCreationExpressionSyntax ObjectCreation(string typeName, params ExpressionSyntax[] members)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeName))
            .WithInitializer(
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ObjectInitializerExpression,
                    SyntaxFactory.SeparatedList(members)
                )
            );
    }

    // ex: typeName: "List<string>", arguments: [LiteralExpression("Item1"), LiteralExpression("Item2")]
    // generates: new List<string> { "Item1", "Item2" }
    public ObjectCreationExpressionSyntax ObjectCreationCollection(string typeName, params ExpressionSyntax[] items)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeName))
            .WithInitializer(
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.CollectionInitializerExpression,
                    SyntaxFactory.SeparatedList(items)
                )
            );
    }

    // ex: memberName: "Title", instanceName: "book"
    // generates: Title = "The Great Gatsby"
    public AssignmentExpressionSyntax PropertyAssignment(string name, string? value = null, ExpressionSyntax? expression = null)
    {
        return SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.ParseTypeName(name),
            value != null
                ? SyntaxFactory.ParseExpression(value!)
                : expression ?? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
        ).NormalizeWhitespace();
    }

    public ReturnStatementSyntax ReturnStatement(string expression)
    {
        return SyntaxFactory.ReturnStatement(
            SyntaxFactory.ParseExpression(expression)
        ).NormalizeWhitespace();
    }
}
