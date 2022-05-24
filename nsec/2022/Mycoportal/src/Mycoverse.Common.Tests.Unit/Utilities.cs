namespace Mycoverse.Common.Tests.Unit;

using System.IO;
using Microsoft.Extensions.Options;

public static class Constants
{
    // Path to the root of the repository.
    public static readonly string BasePath = new FileInfo(typeof(Constants).Assembly.Location).Directory!.Parent!.Parent!.Parent!.Parent!.Parent!.FullName;
}

public class MockedOptions<T> : IOptions<T> where T : class
{
    public MockedOptions(T opts)
    {
        Value = opts;
    }
    public T Value { get; }
}