namespace Mycoverse.Common.Tests.Unit;

using Xunit;
using System;
using System.IO;
using System.Linq;
using Mycoverse.Services.Avatar;
using Mycoverse.Common.Options;

using Microsoft.Extensions.Logging.Abstractions;

public class TestFlag4
{
    private static Random Rng = new Random();

    [Fact]
    public void EncodeDecodeShouldRecoverPlaintext()
    {
        var data = new byte[50];
        Rng.NextBytes(data);

        var roundtrip = data.ToArray();

        AvatarValidator.Encode(ref roundtrip);
        Assert.NotEqual(data, roundtrip);
        AvatarValidator.Decode(ref roundtrip);
        var res = data.Zip(roundtrip);
        Assert.All(res, r => Assert.Equal(r.First, r.Second));
    }

    [Fact]
    public void FlagShouldBeObtainable()
    {
        var flag = new FileInfo("flag4.txt");

        var payloadPath = Path.Join(Constants.BasePath, "src/Payloads/bin/Release/net6.0/Payloads.dll");
        var fi = new FileInfo(payloadPath);
        var bytes = File.ReadAllBytes(fi.FullName);

        try
        {
            AvatarValidator.Encode(ref bytes);
            File.WriteAllBytes("encoded.bin", bytes);
            var validator = new AvatarValidator(new MockedOptions<CryptographyOptions>(
                new CryptographyOptions
                {
                    Key = new byte[0]
                }),
                new NullLogger<AvatarValidator>());

            validator.Validate(bytes);
            Assert.True(flag.Exists);
        }
        finally
        {
            flag.Delete();
        }
    }
}