using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Mycoverse.Common.Options;

using Mycoverse.Net.Options;

using Mycoverse.Services.Avatar;
using Mycoverse.Services.Avatar.Options;
using Mycoverse.Common.Extensions;
using Mycoverse.Common.Cryptography;


using IJsonServer = Mycoverse.Net.IServer<Mycoverse.Net.Json.JsonConnection, Mycoverse.Net.Json.JsonRequest, Mycoverse.Net.Json.JsonResponse>;
using JsonServer = Mycoverse.Net.Server<Mycoverse.Net.Json.JsonConnection, Mycoverse.Net.Json.JsonRequest, Mycoverse.Net.Json.JsonResponse>;

// https://docs.microsoft.com/en-us/dotnet/core/extensions/workers
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, configuration) =>
    {
        var env = ctx.HostingEnvironment;

        configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        services
            .WithOptions<DatabaseOptions>(config)
            .WithOptions<CryptographyOptions>(config)
            .WithOptions<NetworkOptions>(config);

        services
            .AddSingleton<ICipher, AesCipher>()
            .AddSingleton<IJsonServer, JsonServer>()
            .AddSingleton<DatabaseService>()
            .AddSingleton<ApiKeyService>()
            .AddSingleton<AvatarValidator>()
            .AddHostedService<AvatarService>()

        ;
    })
    .Build();

await host.RunAsync();