using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Mycoverse.Services.Backup.Options;
using Mycoverse.Common.Options;

using Mycoverse.Net.Options;

using Mycoverse.Services.Backup;
using Mycoverse.Common.Extensions;
using Mycoverse.Common.Cryptography;

using TextServer = Mycoverse.Net.Server<Mycoverse.Net.TextConnection, Mycoverse.Net.TextRequest, Mycoverse.Net.TextResponse>;
using ITextServer = Mycoverse.Net.IServer<Mycoverse.Net.TextConnection, Mycoverse.Net.TextRequest, Mycoverse.Net.TextResponse>;

// https://docs.microsoft.com/en-us/dotnet/core/extensions/workers
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, configuration) =>
    {
        var env = ctx.HostingEnvironment;

        configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;
        
        services
            .WithOptions<BackupOptions>(config)
            .WithOptions<CryptographyOptions>(config)
            .WithOptions<NetworkOptions>(config);
            
        services
            .AddSingleton<ICipher,AesCipher>()
            .AddSingleton<ITextServer, TextServer>()
            .AddHostedService<BackupService>()
        ;
    })
    .Build();

await host.RunAsync();