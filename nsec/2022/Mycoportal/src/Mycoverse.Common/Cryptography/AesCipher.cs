namespace Mycoverse.Common.Cryptography;

using Mycoverse.Common;
using Mycoverse.Common.Extensions;
using Mycoverse.Common.Options;

using System.Security.Cryptography;
using Microsoft.Extensions.Options;

// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aescryptoserviceprovider?view=net-5.0
public class AesCipher : ICipher
{
    private byte[] _key;

    private CryptographyOptions _settings;

    public AesCipher(IOptions<CryptographyOptions> settings)
    {
        _settings = settings.Value;
        _key = _settings.Key;
    }

    public void Encrypt(Stream src, Stream dst)
    {
        var iv = _settings.GenerateIV();
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        dst.Write(iv); // Prefix with IV

        // Create an encryptor to perform the stream transform.
        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var cs = new CryptoStream(dst, encryptor, CryptoStreamMode.Write, leaveOpen: true);
        src.CopyTo(cs);
    }


    #if DEBUG
    public void Decrypt(Stream src, Stream dst)
    {
        var iv = new byte[16];
        src.Read(iv);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var cs = new CryptoStream(src, decryptor, CryptoStreamMode.Read, leaveOpen: true);
        cs.CopyTo(dst);
    }
    #else
    public void Decrypt(Stream src, Stream dst) => throw new NotImplementedException();
    #endif
}