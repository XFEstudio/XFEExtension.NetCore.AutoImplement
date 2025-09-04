using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace XFEExtension.NetCore.AutoImplement.Analyzer.Generator
{
    [Generator]
    public class ImplementAutoGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        public void Generate(SourceProductionContext context, Compilation compilation)
        {
            var syntaxTrees = compilation.SyntaxTrees;
            foreach (var syntaxTree in syntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Where(classDeclaration => classDeclaration.AttributeLists.Any(IsCreateImplAttribute));
                var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
                FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax = null;
                var namespaceResults = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>();
                if (namespaceResults != null && namespaceResults.Count() > 0)
                    fileScopedNamespaceDeclarationSyntax = namespaceResults.First();
                foreach (var classDeclaration in classDeclarations)
                {
                    var className = classDeclaration.Identifier.ValueText;
                    var implementationSyntaxTree = GenerateImplementationSyntaxTree(classDeclaration, usingDirectives, fileScopedNamespaceDeclarationSyntax, className);
                    context.AddSource($"{className}Impl.g.cs", implementationSyntaxTree.ToString());
                }
            }
        }

        private static bool IsCreateImplAttribute(AttributeListSyntax attributeList) => attributeList.Attributes.Any(attribute => attribute.Name.ToString() == "CreateImpl");

        private static SyntaxTree GenerateImplementationSyntaxTree(ClassDeclarationSyntax classDeclaration, UsingDirectiveSyntax[] usingDirectiveSyntaxes, FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax, string className, string[] modifierParameters, string nameSpace)
        {
            var fatherClassName = classDeclaration.Identifier.ValueText;
            var modifiers = modifierParameters.Select(modifier => SyntaxFactory.ParseToken(modifier)).ToArray();
            ClassDeclarationSyntax implementationClass;
            if (classDeclaration.ParameterList is null)
            {
                implementationClass = SyntaxFactory.ClassDeclaration(className)
                    .AddModifiers(modifiers)
                    .AddMembers(classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().Select(constructor =>
                    {
                        return SyntaxFactory.ConstructorDeclaration(className)
                                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                                            .WithBody(SyntaxFactory.Block())
                                            .WithParameterList(constructor.ParameterList)
                                            .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(constructor.ParameterList.Parameters.Select(parameter => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Identifier)))))));
                    }).ToArray())
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
                implementationClass = implementationClass.AddTypeParameterListParameters(typeParameterListSyntax.Parameters.ToArray())
                                                         .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"{className}{typeParameterListSyntax}")))
                                                         .AddConstraintClauses(classDeclaration.ConstraintClauses.ToArray());
            }
            else
            {
                implementationClass = implementationClass.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(className)));
            }
            MemberDeclarationSyntax memberDeclaration;
            if (fileScopedNamespaceDeclarationSyntax is null)
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
}
