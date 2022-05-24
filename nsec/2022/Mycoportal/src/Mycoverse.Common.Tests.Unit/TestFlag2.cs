namespace Mycoverse.Common.Tests.Unit;

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.Diagnostics.Runtime;
using Xunit;

using Mycoverse.Common.Cryptography;
using Mycoverse.Common.Options;

public class TestFlag2
{
    [Fact]
    public void BackupFlagShouldProduceEncryptedFlag()
    {
        throw new Exception("todo");
    }

    [Fact]
    public void BackupFlagShouldBeDecryptableWithConfiguredKey()
    {
        throw new Exception("todo");
    }

    [Fact]
    public void SymlinkLoopShouldCrashTheProcessAndGenerateCoreDump()
    {
        throw new Exception("todo");
    }

    [Fact]
    public void ClrMdShouldBeAbleToRecoverConfiguredKey()
    {
        var basepath = Path.Combine(Constants.BasePath, "files");
        var src = new FileInfo(Path.Combine(basepath, "tests/test.zst"));
        var dst = new FileInfo(Path.Combine(basepath, "tests/test.dmp"));

        Assert.True(src.Exists);

        Console.WriteLine(src.FullName);
        Console.WriteLine(dst.FullName);
        var p = Process.Start("bash", new string[] {"-c", $"zstdcat '{src.FullName}' > '{dst.FullName}'"});
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);
        Assert.True(dst.Exists);

        try
        {
            // https://github.com/microsoft/clrmd/blob/master/doc/GettingStarted.md#loading-a-crash-dump
            // need kernel param `coredump_filter=0x3f` ...
            // var cordac = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name!.Contains("libhost")).Location;
            // var cordac = "/usr/share/dotnet/shared/Microsoft.NETCore.App/6.0.2/libmscordaccore.so";

            using DataTarget dataTarget = DataTarget.LoadDump(dst.FullName);
            ClrInfo runtimeInfo = dataTarget.ClrVersions[0];  // just using the first runtime
            ClrRuntime runtime = runtimeInfo.CreateRuntime();

            ClrObject? options = null;
            foreach (var obj in runtime.Heap.EnumerateObjects())
            {
                if (obj.Type?.Name?.EndsWith("CryptographyOptions") == true)
                {
                    options = obj;
                    break;
                }
            }
            Assert.NotNull(options);
            var opts = options!.Value;
            var fields = opts.Type!.Fields;
            var keyField = fields.Single(f => f.Name == "_key"); // Key is a property and thus has a backing field.
            var field = opts.ReadObjectField(keyField.Name!);
            Assert.True(field.Type!.IsArray);
            var key = field.AsArray().ReadValues<byte>(0, 32);
            Assert.NotNull(key);
            var expected = File.ReadAllBytes(Path.Combine(basepath, "chal2/cfg/backup.key"));
            Assert.Equal(key, expected);
        }
        finally
        {
            dst.Delete();
        }
    }

    [Fact]
    public void FlagShouldBeObtainable()
    {
        var key = File.ReadAllBytes(Path.Combine(Constants.BasePath, "files/chal2/cfg/backup.key"));
        var aes = new AesCipher(new MockedOptions<CryptographyOptions>(new CryptographyOptions {
            Key = key
        }));

        var flag = "FLAG-SampleFlagData";

        var ms = new MemoryStream(UTF8Encoding.Default.GetBytes(flag));
        var ds = new MemoryStream(48);
        aes.Encrypt(ms, ds);
        Assert.Equal(48, ds.Length);

        ds.Seek(0, SeekOrigin.Begin);
        var roundtrip = new MemoryStream();
        aes.Decrypt(ds, roundtrip);
        roundtrip.Seek(0, SeekOrigin.Begin);
        var tr = new StreamReader(roundtrip);
        var actual = tr.ReadToEnd();
        Assert.Equal(flag, actual);
    }
}