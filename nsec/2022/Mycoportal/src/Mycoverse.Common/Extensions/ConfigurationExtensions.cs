namespace Mycoverse.Common.Extensions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;



public static class ConfigurationExtensions 
{
    public static IServiceCollection WithOptions<T>(this IServiceCollection services, IConfiguration config, string? name = null) where T : class, new()
        {
            name ??= typeof(T).Name;
            return services.Configure<T>(config.GetSection(key: name));
        }

    // public static T BindOptions<T>(this IConfigurationRoot config, string? name = null) where T : class, new()
    // {
    //     T opts = new();
    //     config.GetSection(name ?? nameof(T)).Bind(opts);
    //     return opts;
    // }
}