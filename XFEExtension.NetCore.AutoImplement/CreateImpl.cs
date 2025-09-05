namespace XFEExtension.NetCore.AutoImplement;

/// <summary>
/// 创建一个类的实现类
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CreateImpl : Attribute
{
    public string? ClassName { get; set; }
    public string? NameSpace { get; set; }
    public string[]? Modifiers { get; set; }
    public CreateImpl() { }
    public CreateImpl(string className)
    {
        ClassName = className;
    }
}
