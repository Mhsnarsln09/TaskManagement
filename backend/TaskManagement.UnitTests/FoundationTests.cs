using System.Reflection;

namespace TaskManagement.UnitTests;

public sealed class FoundationTests
{
    [Fact]
    public void DomainAssembly_CanBeLoaded()
    {
        Assembly assembly = Assembly.Load("TaskManagement.Domain");

        Assert.Equal("TaskManagement.Domain", assembly.GetName().Name);
    }
}
