namespace XFEExtension.NetCore.AutoImplement.Test;

[CreateImpl]
public abstract class MyTestClass<T, F>(T obj, F obj2) where T : class where F : new()
{
    public T TProperty { get; set; } = obj;
    public F FProperty { get; set; } = obj2;
}