namespace XFEExtension.NetCore.AutoImplement;

/// <summary>
/// 创建一个类的实现类
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CreateImpl : Attribute
{
    public string? ClassName { get; set; }
    public string? NameSpace { get; set; }
    public string? Modifier { get; set; }
    public CreateImpl() { }
    public CreateImpl(string className)
    {
        ClassName = className;
    }
    public CreateImpl(string className, string modifier)
    {
        ClassName = className;
        Modifier = modifier;
    }
}