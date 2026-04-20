using GeneratorWPF.CodeGenerators.NLayer.Core;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Models.Statics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GeneratorWPF.CodeGenerators.NLayer.Base;

public class NLayerGeneratorBase
{
    protected readonly AppSetting _appSetting;
    public NLayerGeneratorBase(AppSetting appSetting) => _appSetting = appSetting;


    #region Project Create Methods
    public string CreateSolution()
    {
        try
        {
            string slnPath = Path.Combine(_appSetting.SolutionPath, $"{_appSetting.SolutionName}.sln");
            string slnxPath = Path.Combine(_appSetting.SolutionPath, $"{_appSetting.SolutionName}.slnx");

            if (!Directory.Exists(_appSetting.SolutionPath))
                Directory.CreateDirectory(_appSetting.SolutionPath);

            if (File.Exists(slnPath) || File.Exists(slnxPath))
                return "INFO: Solution already exists.";

            RunCommand(_appSetting.SolutionPath, "dotnet", $"new sln  -n {_appSetting.SolutionName}");

            return "OK: Solution Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the Solution. \n\t Details:{ex.Message}");
        }
    }

    public  virtual string CreateClassLibrearyProject(string layerProjectName, string[]? referances = null)
    {
        try
        {
            string layerPath = Path.Combine(_appSetting.SolutionPath, layerProjectName);
            string csprojPath = Path.Combine(layerPath, $"{layerProjectName}.csproj");

            if (Directory.Exists(layerPath) && File.Exists(csprojPath))
                return $"INFO: {layerProjectName} already exists.";

            bool isSlnx = File.Exists(Path.Combine(_appSetting.SolutionPath, $"{_appSetting.SolutionName}.slnx"));

            RunCommand(_appSetting.SolutionPath, "dotnet", $"new classlib -n {layerProjectName}");
            RunCommand(_appSetting.SolutionPath, "dotnet", $"sln {_appSetting.SolutionName}.{(isSlnx ? "slnx" : "sln")} add {layerProjectName}/{layerProjectName}.csproj");
            
            RemoveFile(layerPath, "Class1.cs");

            if (referances != null && referances.Length > 0)
            {
                foreach (var referance in referances)
                {
                    // "../Core/Core.csproj"
                    RunCommand(layerPath, "dotnet", $"add reference {referance}");
                }
            }

            return $"OK: {layerProjectName} Layer Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the {layerProjectName}. \n\t Details:{ex.Message}");
        }
    }
    #endregion

    public string GenerateStaticFiles(string layerName, string layerProjectName, object? extendedOptions = null)
    {
        List<string> resultLogs = new List<string>();

        string layerPath = Path.Combine(_appSetting.SolutionPath, layerProjectName);
        string rootPath = GetTemplatesRootPath(layerName);

        var resources = GetAllTemplateResources(rootPath);
        foreach (var resourcePath in resources)
        {
            string relativePath = resourcePath.Replace(rootPath, "").Replace(".tpl", "");

            string fileDirectory = Path.GetDirectoryName(relativePath)!;
            fileDirectory = fileDirectory.TrimStart('\\', '/');
            string fileName = Path.GetFileName(relativePath);
            string folderPath = Path.Combine(layerPath, fileDirectory);

            if (File.Exists(Path.Combine(folderPath, fileName)))
            {
                resultLogs.Add($"INFO: File already exists: {fileName}");
                continue;
            }

            string template = File.ReadAllText(resourcePath);

            IDictionary<string, object> dict = new Dictionary<string, object>
            {
                ["core_project_name"] = _appSetting.CoreLayerProjectName,
                ["model_project_name"] = _appSetting.ModelLayerProjectName,
                ["dataAccess_project_name"] = _appSetting.DataAccessLayerProjectName,
                ["business_project_name"] = _appSetting.BusinessLayerProjectName,
                ["api_project_name"] = _appSetting.WebAPILayerProjectName,
                ["webui_project_name"] = _appSetting.WebUILayerProjectName,
                ["project_name"] = _appSetting.ProjectName!
            };

            if (extendedOptions != null)
            {
                foreach (var prop in extendedOptions.GetType().GetProperties())
                {
                    var value = prop.GetValue(extendedOptions);
                    if (value != null)
                        dict[prop.Name] = value;
                }
            }

            string content = ScribanRender(template, dict);

            resultLogs.Add(AddFile(folderPath, fileName, content));
        }

        return string.Join("\n", resultLogs);
    }


    #region Package-Methods
    public string AddPackage(string packageName, string layerProjectName)
    {
        try
        {
            string layerPath = Path.Combine(_appSetting.SolutionPath, layerProjectName);
            string csprojPath = Path.Combine(layerPath, $"{layerProjectName}.csproj");

            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"{layerProjectName}.csproj not found for adding package({layerProjectName}).");

            var doc = XDocument.Load(csprojPath);
            string searchParam = Regex.Replace(packageName, @"\s*--version.*", "").Trim();
            var packageAlreadyAdded = doc.Descendants("PackageReference").Any(p => p.Attribute("Include")?.Value == searchParam);

            if (packageAlreadyAdded)
                return $"INFO: Package {packageName} already exists in {layerProjectName}.";

            RunCommand(layerPath, "dotnet", $"add package {packageName}");

            return $"OK: Package {packageName} added to {layerProjectName}.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding package to {layerProjectName}. \n\t Details:{ex.Message}");
        }
    }

    public string Restore(string layerProjectName)
    {
        try
        {
            string layerPath = Path.Combine(_appSetting.SolutionPath, layerProjectName);

            RunCommand(layerPath, "dotnet", "restore");
            return $"OK: Restored {layerProjectName}.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while restoring {layerProjectName}. \n Details:{ex.Message}");
        }
    }
    #endregion


    protected string RunCommand(string workingDirectory, string fileName, string arguments)
    {
        var processInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            string output = process!.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process!.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command failed: {error}");
            }
            else
            {
                return output;
            }
        }
    }


    #region Template Provide
    protected string TemplateProvider(string rootPath, string filePath)
    {
        return File.ReadAllText(Path.Combine(rootPath, filePath));
    }

    protected IEnumerable<string> GetAllTemplateResources(string rootPath)
    {
        return Directory.GetFiles(rootPath, "*.tpl", SearchOption.AllDirectories);
    }

    protected string GetTemplatesRootPath(string layer)
    {
        return Path.Combine(AppContext.BaseDirectory, "CodeGenerators", "NLayer", layer, "Templates");
    }

    protected string ScribanRender(string templateText, object model)
    {
        var template = Template.Parse(templateText);

        if (template.HasErrors)
            throw new InvalidOperationException(string.Join("\n", template.Messages));

        return template.Render(model, member => member.Name);
    }
    #endregion


    #region File Control
    protected string AddFile(string folderPath, string fileName, string code)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, code);
                return $"OK: File {fileName} added.";
            }
            else
            {
                return $"INFO: File {fileName} already exists.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file({fileName}). \n Details:{ex.Message}");
        }
    }

    protected string RemoveFile(string folderPath, string fileName)
    {
        try
        {
            string filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return $"OK: File {fileName} removed.";
            }
            else
            {
                return $"INFO: File {fileName} does not exist.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing file ({fileName}) \n Details: {ex.Message}");
        }
    }

    protected string RemoveFolder(string folderPath)
    {
        try
        {
            string filePath = Path.Combine(folderPath);

            if (Directory.Exists(filePath))
            {
                Directory.Delete(filePath, true);
                return $"OK: Folder {folderPath} removed.";
            }
            else
            {
                return $"INFO: Folder {folderPath} does not exist.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing folder ({folderPath}) \n Details: {ex.Message}");
        }
    }

    protected string CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir);

        //return $"INFO: Directory {sourceDir} already exists in WebUI project.";

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string targetFilePath = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, targetFilePath, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string targetSubDir = Path.Combine(destinationDir, Path.GetFileName(directory));
            CopyDirectory(directory, targetSubDir);
        }

        return $"OK: Directory {sourceDir} added to WebUI project.";
    }
    #endregion


    #region Roslyn Methods
    protected ParameterSyntax ParameterDeclaration(string type, string name, bool required = true, SyntaxKind[]? modifiers = null)
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
                    SyntaxFactory.LiteralExpression(
                         SyntaxKind.DefaultLiteralExpression,
                         SyntaxFactory.Token(SyntaxKind.DefaultKeyword)
                    )
                )
            );

        return parameter.NormalizeWhitespace();
    }

    protected PropertyDeclarationSyntax PropertyDeclaration(string type, string name, bool required = false, SyntaxKind[]? modifiers = null, AttributeSyntax[]? attributes = null)
    {
        if (required == false && !type.EndsWith("?") && !Statics.nonReferanceTypes.Contains(type))
            type += "?";
        else if (required == true && type.EndsWith("?"))
            type = type.TrimEnd('?');

        var property = SyntaxFactory
            .PropertyDeclaration(SyntaxFactory.ParseTypeName(type), SyntaxFactory.Identifier(name))
            .AddModifiers(modifiers != null && modifiers.Length > 0 ? modifiers.Select(SyntaxFactory.Token).ToArray() : [SyntaxFactory.Token(SyntaxKind.PublicKeyword)])
            .AddAccessorListAccessors(
                SyntaxFactory
                    .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory
                    .AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            );

        if (attributes?.Length > 0)
            property = property.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes)));

        if (required == true && !Statics.nonReferanceTypes.Contains(type))
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

    protected FieldDeclarationSyntax FieldDeclaration(SyntaxKind[] modifiers, string type, string name, bool nullable = false)
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

    protected MethodDeclarationSyntax MethodDeclaration(string name, string returnType, bool isThereBody = true, SyntaxKind[]? modifiers = null, ParameterSyntax[]? parameters = null, AttributeSyntax[]? attributes = null, BlockSyntax? block = null, string? body = null)
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

    protected ClassDeclarationSyntax ClassDeclaration(SyntaxKind[] modifiers, string name, AttributeSyntax[]? attributes = null, TypeSyntax[]? baseTypes = null, MemberDeclarationSyntax[]? members = null, string? body = null)
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
            classSyntax = classSyntax.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>([SyntaxFactory.ParseMemberDeclaration(body)]!));
        return classSyntax.NormalizeWhitespace();
    }

    protected InterfaceDeclarationSyntax InterfaceDeclaration(SyntaxKind[] modifiers, string name, AttributeSyntax[]? attributes = null, TypeSyntax[]? baseTypes = null, MemberDeclarationSyntax[]? members = null)
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

    protected NamespaceDeclarationSyntax NamespaceDeclaration(string value, MemberDeclarationSyntax[] members)
    {
        return SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName(value))
            .AddMembers(members) // memebers can be class, interface, enum, struct etc.
            .NormalizeWhitespace();
    }

    protected CompilationUnitSyntax CompilationUnit(string[] usings, NamespaceDeclarationSyntax nspace)
    {
        return SyntaxFactory
           .CompilationUnit()
           .AddUsings(usings.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))).ToArray())
           .AddMembers(nspace)
           .NormalizeWhitespace();
    }

    protected ClassDeclarationSyntax ValidatorClassDeclaration(string modelName, string[] ruleList)
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
                            SyntaxFactory.IdentifierName(modelName)
                        )
                    )
                )
            ],
            members: [constructor]
        ).NormalizeWhitespace();
    }

    protected static ConstructorDeclarationSyntax ConstructorDeclaration(SyntaxKind[] modifiers, string name, ParameterSyntax[]? parameters = null, string[]? baseArgs = null, StatementSyntax[]? statements = null, BlockSyntax? block = null)
    {
        var constructorSyntax = SyntaxFactory
            .ConstructorDeclaration(name)
            .AddModifiers([.. modifiers.Select(SyntaxFactory.Token)]);

        if (parameters?.Length > 0)
            constructorSyntax = constructorSyntax.AddParameterListParameters(parameters);

        if (baseArgs != null)
        {
            var arrOfBaseArgs = baseArgs.Select(arg => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(arg))).ToArray();
            constructorSyntax = constructorSyntax
                .WithInitializer(
                    SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
                    .AddArgumentListArguments(arrOfBaseArgs));
        }

        constructorSyntax = constructorSyntax.WithBody(statements != null ? SyntaxFactory.Block(statements) : block != null ? block : SyntaxFactory.Block());

        return constructorSyntax.NormalizeWhitespace();
    }

    /// <summary>Generates: <c>left = right;</c></summary>
    protected StatementSyntax StatementExpression(string left, string right)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(left),
                SyntaxFactory.IdentifierName(right)
            )
        ).NormalizeWhitespace();
    }
    #endregion

    #region Validation
    protected string ValidationRule(Validation validation, string fieldName, string? message)
    {
        string rule = string.Empty;

        switch (validation.ValidatorTypeId)
        {
            case (int)ValidatorTypes.NotEmpty:
                if (string.IsNullOrEmpty(message)) message = "This field is required.";
                rule = $".NotEmpty().WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.NotNull:
                if (string.IsNullOrEmpty(message)) message = "This field cannot be null.";
                rule = $".NotNull().WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.NotEqual:
                var neqValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.NotEqual_Value)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"The value cannot be {neqValue ?? "?"}.";
                rule = $".NotEqual({neqValue}).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.MaxLength:
                var mxLValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.MaxLength_Max)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"This field must be at most {mxLValue ?? "?"} characters long.";
                rule = $".MaximumLength({mxLValue}).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.MinLength:
                var minLValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.MinLength_Min)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"This field must be at least {minLValue ?? "?"} characters long.";
                rule = $".MinimumLength({minLValue}).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.Range:
                var rngMinValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.Range_Min)?.Value : "?";
                var rngMaxValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.Range_Max)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"Value must be between {rngMinValue ?? "?"} and {rngMaxValue ?? "?"}";
                rule = $".InclusiveBetween({rngMinValue}, {rngMaxValue}).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.Regex:
                if (string.IsNullOrEmpty(message)) message = "The format of this field is invalid.";
                var rgPattern = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.Regex_Pattern)?.Value : "?";
                rule = $".Matches({rgPattern}).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.GreaterThan:
                var gtValue = validation.ValidationParams != null ? validation.ValidationParams.First(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.GreaterThan_Value).Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"Value must be greater than {gtValue ?? "?"}";
                rule = $".GreaterThan({gtValue}).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.LessThan:
                var ltValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.LessThan_Value)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"Value must be less than {ltValue ?? "?"}";
                rule = $".LessThan({ltValue}).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.EmailAddress:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid email address.";
                rule = $".EmailAddress().WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.CreditCard:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid credit card number.";
                rule = $".CreditCard().WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.Phone:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid phone number.";
                rule = $".Matches(@\"^\\+?\\d{{10,15}}$\").WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.Url:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid URL.";
                rule = $".Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.Date:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid date.";
                rule = $".Must(date => date != default).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.Number:
                if (string.IsNullOrEmpty(message)) message = "Please enter a valid number.";
                rule = $".Must(amount => decimal.TryParse(amount.ToString(), out _)).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.GuidNotEmpty:
                if (string.IsNullOrEmpty(message)) message = "Field must be a valid guid value";
                rule = $".NotEqual(Guid.Empty).WithMessage(\"{message}\")";
                break;
            case (int)ValidatorTypes.Length:
                var lengthValue = validation.ValidationParams != null ? validation.ValidationParams.FirstOrDefault(f => f.ValidatorTypeParamId == (int)ValidatorTypeParams.Length_Value)?.Value : "?";
                if (string.IsNullOrEmpty(message)) message = $"This field must be {lengthValue ?? "?"} characters long.";
                rule = $".Length({lengthValue}).WithMessage(\"{message}\")";
                break;
            default:
                break;
        }
        if (string.IsNullOrWhiteSpace(rule))
            return string.Empty;
        return $"RuleFor(v => v.{fieldName}){rule};";
    }
    #endregion
}
