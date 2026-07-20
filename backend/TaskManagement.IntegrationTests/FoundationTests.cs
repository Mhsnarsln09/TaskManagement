using System.Reflection;

namespace TaskManagement.IntegrationTests;

public sealed class FoundationTests
{
    [Fact]
    public void ApiAssembly_CanBeLoaded()
    {
        Assembly assembly = Assembly.Load("TaskManagement.Api");

        Assert.Equal("TaskManagement.Api", assembly.GetName().Name);
    }
}
