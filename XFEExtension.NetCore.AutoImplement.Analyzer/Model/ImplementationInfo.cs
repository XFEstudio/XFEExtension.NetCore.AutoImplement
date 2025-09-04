using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XFEExtension.NetCore.AutoImplement.Analyzer.Model;

public class ImplementationInfo
{
    public ClassDeclarationSyntax ClassDeclaration { get; set; }
    public string TargetClassName { get; set; } = string.Empty;
    public string[] Modifiers { get; set; } = [];
    public string NameSpace { get; set; } = string.Empty;
}
