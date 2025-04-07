using Microsoft.Extensions.DependencyInjection;

namespace Notifliwy.Units;

public class GenericUnit
{
    protected ServiceCollection ServiceCollection { get; } = []; 
    
    [Fact]
    public void Test()
    {
        using var serviceProvider = ServiceCollection
            .AddScoped<ITestInterface<TestFirst, TestSecond>, TestClass>()
            .BuildServiceProvider();
    }
}

internal interface ITestInterface<TFirst, TSecond>
    where TSecond : class
    where TFirst : class;

internal record TestFirst;

internal record TestSecond;

internal class TestClass : ITestInterface<TestFirst, TestSecond>;