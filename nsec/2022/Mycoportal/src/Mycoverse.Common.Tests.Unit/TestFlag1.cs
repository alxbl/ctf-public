namespace Mycoverse.Common.Tests.Unit;

using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mycoverse.Common.Data;
using Mycoverse.Common.Model;

using System.Reflection;
using System.Diagnostics;

public class TestFlag1
{
    private static readonly Random Rng = new Random();

    [Fact]
    public void CompressDecompressShouldRecoverOriginalData()
    {
        var data = new byte[5000];
        Rng.NextBytes(data);

        var compressed = Compression.Compress(data);
        var decompressed = Compression.Decompress(compressed);

        Assert.Equal(data.Length, decompressed.Length);
        var res = data.Zip(decompressed);
        Assert.All(res, r => Assert.Equal(r.First, r.Second));
    }

    [Fact]
    public void SerializerRoundtripShouldSupportSession()
    {
        var session = new Session {
            ApiKey = "1234",
            Debug = true,
            Hmac = "badHmac",
            Username = "A user name",
        };

        var ser = new KaenSerializer();
        using var ms = new MemoryStream(4096);
        ser.Serialize(ms, session);
        ms.Seek(0, SeekOrigin.Begin);

        var roundtrip = ser.Deserialize<Session>(ms);
        Assert.NotNull(roundtrip);
        Assert.Equal(session.ApiKey, roundtrip!.ApiKey);
        Assert.Equal(session.Debug, roundtrip!.Debug);
        Assert.Equal(session.Hmac, roundtrip!.Hmac);
        Assert.Equal(session.Username, roundtrip!.Username);
    }

    [Fact]
    public void Flag1ShouldBeObtainable()
    {
        // TODO.
    }
}