# XFEExtension.NetCore.AutoImplement

## 描述

自动生成实现类

## 自动生成实现类

```csharp
[CreateImpl]
abstract class TestAbstractClass(int num)
{
    public int Num { get; set; } = num;
}

class Program
{
    static void Main(string[] args)
    {
        var testAbstractClass = new TestAbstractClassImpl(123);
        Console.WriteLine(testAbstractClass.Num);
    }
}
```