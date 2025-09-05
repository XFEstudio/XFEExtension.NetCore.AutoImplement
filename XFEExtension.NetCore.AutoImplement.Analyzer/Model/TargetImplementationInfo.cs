namespace XFEExtension.NetCore.AutoImplement.Analyzer.Model;

public class TargetImplementationInfo
{
    public string TargetClassName { get; set; } = string.Empty;
    public string[] Modifiers { get; set; } = [];
    public string NameSpace { get; set; } = string.Empty;
}
