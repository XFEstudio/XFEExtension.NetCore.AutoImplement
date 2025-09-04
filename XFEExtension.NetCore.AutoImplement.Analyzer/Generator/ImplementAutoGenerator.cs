using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using XFEExtension.NetCore.AutoImplement.Analyzer.Model;

namespace XFEExtension.NetCore.AutoImplement.Analyzer.Generator;

[Generator]
public class ImplementAutoGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var implementationProvider = context.SyntaxProvider.ForAttributeWithMetadataName("CreateImpl",
                                                                                         static (x, _) => x is ClassDeclarationSyntax classDeclaration && classDeclaration.AttributeLists.Any(IsCreateImplAttribute),
                                                                                         GetCreateImpl);
        context.RegisterSourceOutput(implementationProvider.Combine(context.CompilationProvider), Generate);
    }

    public void Generate(SourceProductionContext context, (ImplementationInfo? ImplementationInfo, Compilation Compilation) args)
    {
        Debugger.Launch();
        if (args.ImplementationInfo is null)
            return;
        var root = args.ImplementationInfo.ClassDeclaration.SyntaxTree.GetRoot();
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
        FileScopedNamespaceDeclarationSyntax? fileScopedNamespaceDeclarationSyntax = null;
        var namespaceResults = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>();
        if (namespaceResults != null && namespaceResults.Count() > 0)
            fileScopedNamespaceDeclarationSyntax = namespaceResults.First();
        var implementationSyntaxTree = GenerateImplementationSyntaxTree(args.ImplementationInfo.ClassDeclaration, usingDirectives, fileScopedNamespaceDeclarationSyntax, args.ImplementationInfo.TargetClassName, args.ImplementationInfo.Modifiers, args.ImplementationInfo.NameSpace);
        context.AddSource($"{args.ImplementationInfo.TargetClassName}.g.cs", implementationSyntaxTree.ToString());
    }

    private static bool IsCreateImplAttribute(AttributeListSyntax attributeList) => attributeList.Attributes.Any(attribute => attribute.Name.ToString() == "CreateImpl");

    //public static List<AttributeSyntax> GetCreateImplAttributeList(ClassDeclarationSyntax classDeclaration) => [.. classDeclaration.AttributeLists.Where(IsCreateImplAttribute).SelectMany(attributeList => attributeList.Attributes)];

    public static ImplementationInfo? GetCreateImpl(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        Debugger.Launch();
        token.ThrowIfCancellationRequested();
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
        var attributeData = context.Attributes.FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == "CreateImpl");
        if (attributeData is null)
            return null;
        var className = classDeclaration.Identifier.ValueText;
        var targetClassName = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "ClassName").Value.Value as string ?? $"{className}Impl";
        var nameSpace = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "NameSpace").Value.Value as string ?? string.Empty;
        var modifiers = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "Modifiers").Value.Values.Select(v => v.Value?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToArray() ?? [];//此处出现问题
        if (modifiers.Length == 0)
            modifiers = ["internal", "partial"];
        return new ImplementationInfo
        {
            ClassDeclaration = classDeclaration,
            TargetClassName = targetClassName,
            Modifiers = modifiers,
            NameSpace = nameSpace
        };
    }

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
            implementationClass = SyntaxFactory.ClassDeclaration($"{className}Impl")
                .AddModifiers(modifiers)
                .AddMembers(SyntaxFactory.ConstructorDeclaration($"{className}Impl")
                                         .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                                         .WithBody(SyntaxFactory.Block())
                                         .WithParameterList(classDeclaration.ParameterList)
                                         .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(classDeclaration.ParameterList.Parameters.Select(parameter => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Identifier))))))))
                .WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia($@"/// <summary>
/// <seealso cref=""{className}Impl""/> 是根据 <seealso cref=""{className}""/> 自动生成的实现类
/// </summary>
"))
                .NormalizeWhitespace();
        }
        if (classDeclaration.TypeParameterList is TypeParameterListSyntax typeParameterListSyntax && typeParameterListSyntax.Parameters.Count > 0)
        {
            implementationClass = implementationClass.AddTypeParameterListParameters([.. typeParameterListSyntax.Parameters])
                                                     .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"{className}{typeParameterListSyntax}")))
                                                     .AddConstraintClauses([.. classDeclaration.ConstraintClauses]);
        }
        else
        {
            implementationClass = implementationClass.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(className)));
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
