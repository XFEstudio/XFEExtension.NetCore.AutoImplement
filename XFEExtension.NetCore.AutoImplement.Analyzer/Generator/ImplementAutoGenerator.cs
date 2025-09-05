using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace XFEExtension.NetCore.AutoImplement.Analyzer.Generator;

[Generator]
public class ImplementAutoGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //var implementationProvider = context.SyntaxProvider.ForAttributeWithMetadataName("CreateImpl",
        //static (x, _) => x is ClassDeclarationSyntax classDeclaration && classDeclaration.AttributeLists.Any(IsCreateImplAttribute),
        //GetCreateImpl);
        //context.RegisterSourceOutput(implementationProvider.Combine(context.CompilationProvider), Generate);
        context.RegisterSourceOutput(context.CompilationProvider, Generate);
    }

    //public void Generate(SourceProductionContext context, (ImplementationInfo? ImplementationInfo, Compilation Compilation) args)
    //{
    //    if (args.ImplementationInfo is null)
    //        return;
    //    var root = args.ImplementationInfo.ClassDeclaration.SyntaxTree.GetRoot();
    //    var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
    //    FileScopedNamespaceDeclarationSyntax? fileScopedNamespaceDeclarationSyntax = null;
    //    var namespaceResults = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>();
    //    if (namespaceResults != null && namespaceResults.Count() > 0)
    //        fileScopedNamespaceDeclarationSyntax = namespaceResults.First();
    //    var implementationSyntaxTree = GenerateImplementationSyntaxTree(args.ImplementationInfo.ClassDeclaration, usingDirectives, fileScopedNamespaceDeclarationSyntax, args.ImplementationInfo.TargetClassName, args.ImplementationInfo.Modifiers, args.ImplementationInfo.NameSpace);
    //    context.AddSource($"{args.ImplementationInfo.TargetClassName}.g.cs", implementationSyntaxTree.ToString());
    //}

    public void Generate(SourceProductionContext context, Compilation compilation)
    {
        var syntaxTrees = compilation.SyntaxTrees;
        foreach (var syntaxTree in syntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(classDeclaration => classDeclaration.AttributeLists.Any(IsCreateImplAttribute));
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            FileScopedNamespaceDeclarationSyntax? fileScopedNamespaceDeclarationSyntax = null;
            var namespaceResults = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>();
            if (namespaceResults != null && namespaceResults.Count() > 0)
                fileScopedNamespaceDeclarationSyntax = namespaceResults.First();
            foreach (var classDeclaration in classDeclarations)
            {
                foreach (var attribute in GetCreateImplAttributeList(classDeclaration))
                {
                    var className = $"{classDeclaration.Identifier.ValueText}Impl";
                    var modifiers = new string[] { "internal", "partial" };
                    var nameSpace = string.Empty;
                    if (attribute.ArgumentList is not null && attribute.ArgumentList.Arguments.Count > 0)
                    {
                        foreach (var argument in attribute.ArgumentList.Arguments)
                        {
                            if (argument.Expression is LiteralExpressionSyntax literalExpressionSyntax && literalExpressionSyntax.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                className = literalExpressionSyntax.Token.ValueText;
                            }
                            else if (argument.NameEquals is NameEqualsSyntax nameEqualsSyntax)
                            {
                                var argumentName = nameEqualsSyntax.Name.Identifier.ValueText;
                                if (argumentName == "ClassName" && argument.Expression is LiteralExpressionSyntax classNameLiteral && classNameLiteral.IsKind(SyntaxKind.StringLiteralExpression))
                                {
                                    className = classNameLiteral.Token.ValueText;
                                }
                                else if (argumentName == "Modifiers")
                                {
                                    if (argument.Expression is ArrayCreationExpressionSyntax arrayCreationExpressionSyntax)
                                    {
                                        if (arrayCreationExpressionSyntax.Initializer is InitializerExpressionSyntax initializerExpressionSyntax)
                                        {
                                            modifiers = [.. initializerExpressionSyntax.Expressions.OfType<LiteralExpressionSyntax>()
                                                                                             .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
                                                                                             .Select(literal => literal.Token.ValueText)
                                                                                             .Where(s => !string.IsNullOrEmpty(s))];
                                        }
                                    }
                                    else if (argument.Expression is CollectionExpressionSyntax collectionExpressionSyntax)
                                    {
                                        modifiers = [.. collectionExpressionSyntax.ChildNodes().OfType<ExpressionElementSyntax>()
                                                                                           .Select(element => element.Expression.ToString().Replace("\"",""))
                                                                                           .Where(s => !string.IsNullOrEmpty(s))];
                                    }
                                }
                                else if (argumentName == "NameSpace" && argument.Expression is LiteralExpressionSyntax nameSpaceLiteral && nameSpaceLiteral.IsKind(SyntaxKind.StringLiteralExpression))
                                {
                                    nameSpace = nameSpaceLiteral.Token.ValueText;
                                }
                            }
                        }
                    }
                    var implementationSyntaxTree = GenerateImplementationSyntaxTree(classDeclaration, usingDirectives, fileScopedNamespaceDeclarationSyntax, className, modifiers, nameSpace);
                    context.AddSource($"{className}.g.cs", implementationSyntaxTree.ToString());
                }
            }
        }
    }

    private static bool IsCreateImplAttribute(AttributeListSyntax attributeList) => attributeList.Attributes.Any(attribute => attribute.Name.ToString() == "CreateImpl");

    public static List<AttributeSyntax> GetCreateImplAttributeList(ClassDeclarationSyntax classDeclaration) => [.. classDeclaration.AttributeLists.Where(IsCreateImplAttribute).SelectMany(attributeList => attributeList.Attributes)];

    //public static ImplementationInfo? GetCreateImpl(GeneratorAttributeSyntaxContext context, CancellationToken token)
    //{
    //    if (context is { TargetSymbol: INamedTypeSymbol })
    //        Debugger.Launch();
    //    token.ThrowIfCancellationRequested();
    //    var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
    //    var attributeData = context.Attributes.FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == "CreateImpl");
    //    if (attributeData is null)
    //        return null;
    //    var className = classDeclaration.Identifier.ValueText;
    //    var targetClassName = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "ClassName").Value.Value as string ?? $"{className}Impl";
    //    //var targetClassName = $"{className}Impl";
    //    var nameSpace = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "NameSpace").Value.Value as string ?? string.Empty;
    //    //var nameSpace = string.Empty;
    //    //var value = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "Modifiers").Value;
    //    //var modifiers = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "Modifiers").Value.Values.Select(v => v.Value?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToArray() ?? [];
    //    var modifiers = Array.Empty<string>();
    //    //foreach (var attributeList in classDeclaration.AttributeLists)
    //    //{
    //    //    foreach (var attribute in attributeList.Attributes)
    //    //    {
    //    //        if (attribute is null || attribute.Name.ToString() != "CreateImpl")
    //    //            continue;
    //    //        foreach (var argument in attribute.ArgumentList.Arguments)
    //    //        {

    //    //        }
    //    //    }
    //    //}
    //    if (modifiers.Length == 0)
    //        modifiers = ["internal", "partial"];
    //    return new ImplementationInfo
    //    {
    //        ClassDeclaration = classDeclaration,
    //        TargetClassName = targetClassName,
    //        Modifiers = modifiers,
    //        NameSpace = nameSpace
    //    };
    //}

    private static SyntaxTree GenerateImplementationSyntaxTree(ClassDeclarationSyntax classDeclaration, UsingDirectiveSyntax[] usingDirectiveSyntaxes, FileScopedNamespaceDeclarationSyntax? fileScopedNamespaceDeclarationSyntax, string className, string[] modifierParameters, string nameSpace)
    {
        var fatherClassName = classDeclaration.Identifier.ValueText;
        var modifiers = modifierParameters.Select(modifier => SyntaxFactory.ParseToken(modifier)).ToArray();
        ClassDeclarationSyntax implementationClass;
        if (classDeclaration.ParameterList is null)
        {
            implementationClass = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(modifiers)
                .AddMembers([.. classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().Select(constructor =>
                {
                    return SyntaxFactory.ConstructorDeclaration(className)
                                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                                        .WithBody(SyntaxFactory.Block())
                                        .WithParameterList(constructor.ParameterList)
                                        .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(constructor.ParameterList.Parameters.Select(parameter => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Identifier)))))));
                })])
                .WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia($@"/// <summary>
/// <seealso cref=""{className}""/> 是根据 <seealso cref=""{fatherClassName}""/> 自动生成的实现类
/// </summary>
"))
                .NormalizeWhitespace();
        }
        else
        {
            implementationClass = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(modifiers)
                .AddMembers(SyntaxFactory.ConstructorDeclaration(className)
                                         .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                                         .WithBody(SyntaxFactory.Block())
                                         .WithParameterList(classDeclaration.ParameterList)
                                         .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(classDeclaration.ParameterList.Parameters.Select(parameter => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Identifier))))))))
                .WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia($@"/// <summary>
/// <seealso cref=""{className}""/> 是根据 <seealso cref=""{fatherClassName}""/> 自动生成的实现类
/// </summary>
"))
                .NormalizeWhitespace();
        }
        if (classDeclaration.TypeParameterList is TypeParameterListSyntax typeParameterListSyntax && typeParameterListSyntax.Parameters.Count > 0)
        {
            implementationClass = implementationClass.AddTypeParameterListParameters([.. typeParameterListSyntax.Parameters])
                                                     .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"{fatherClassName}{typeParameterListSyntax}")))
                                                     .AddConstraintClauses([.. classDeclaration.ConstraintClauses]);
        }
        else
        {
            implementationClass = implementationClass.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(fatherClassName)));
        }
        MemberDeclarationSyntax memberDeclaration;
        if (nameSpace != string.Empty)
        {
            memberDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nameSpace))
                .AddMembers(implementationClass);
        }
        else if (fileScopedNamespaceDeclarationSyntax is null)
        {
            var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            if (namespaceDeclaration is null)
                memberDeclaration = implementationClass;
            else
                memberDeclaration = SyntaxFactory.NamespaceDeclaration(namespaceDeclaration.Name)
                    .AddMembers(implementationClass);
        }
        else
        {
            memberDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(fileScopedNamespaceDeclarationSyntax.Name)
                .AddMembers(implementationClass);
        }
        var implementationCompilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(usingDirectiveSyntaxes)
            .AddMembers(memberDeclaration)
            .NormalizeWhitespace();
        return SyntaxFactory.SyntaxTree(implementationCompilationUnit);
    }
}
