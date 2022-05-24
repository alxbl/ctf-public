namespace Mycoverse.Services.Avatar;

using System.Reflection;
using System.Security.Cryptography;

using Mycoverse.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mycoverse.Common.Options;

public class AvatarValidator
{
    private const int HmacLen = 1000;
    private readonly byte[] _key;

    private readonly ILogger<AvatarValidator> _log;

    public AvatarValidator(IOptions<CryptographyOptions> opts, ILogger<AvatarValidator> log)
    {
        _key = opts.Value.Key;
        _log = log;
    }
    public bool Validate(byte[] data)
    {
        if (data.Length <= HmacLen) return false;

        Decode(ref data);

        try
        {
            var a = Assembly.Load(data);
            var t = a.GetTypes().SingleOrDefault(t => typeof(IAvatar).IsAssignableFrom(t));
            if (t is null) return false;

            var ctor = t.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>());
            if (ctor is null) return false;
            object instance = ctor.Invoke(Array.Empty<object>());

            var avatar = instance as IAvatar;
            if (avatar is null) return false;

            if (string.IsNullOrWhiteSpace(avatar.Author)) return false;

            if (!VerifyHmac(data[..HmacLen], avatar.Signature)) return false;
        }
        catch(Exception e)
        {
            _log.LogError(e, "Validation failed");
            return false;
        }

        return true;
    }

    public static void Decode(ref byte[] avatar)
    {
        var k = Key.ToArray(); // Copy
        for (var i = 0; i < avatar.Length; ++i)
        {
            avatar[i] ^= k[i % k.Length]; // retrieve plaintext
            var s = (avatar[i] & 0xc0) >> 6; // "SBOX": adjust key based on the plaintext

            switch (s)
            {
                case 0x01: S(k, 1, 0); break; // swap k[0] and k[1]
                case 0x02: S(k, 2, 3); break; // swap k[2] and k[3]
                case 0x03: S(k, 3, 0); break; // swap k[3] and k[0]
            }

            if (i % 25 == 0 && i > 0) // unscramble: swap bytes data[i] and data[i-1]
                S(avatar, i, i - 1); // Swap after decoding.

        }
    }
    private static void S(byte[] b, int x, int y)
    {
        var t = b[x];
        b[x] = b[y];
        b[y] = t;
    }

#if DEBUG
    public static void Encode(ref byte[] avatar)
    {
        var k = Key.ToArray(); // Copy
        for (var i = 0; i < avatar.Length; ++i)
        {
            if (i + 1 < avatar.Length && (i + 1) % 25 == 0) // scramble: swap bytes data[i] and data[i+1]
                S(avatar, i, i + 1); // Swap before encoding.

            var s = (avatar[i] & 0xc0) >> 6; // "SBOX": plaintext & 1110 0000

            avatar[i] ^= k[i % k.Length]; // encode byte.

            switch (s) // apply "SBOX" to shuffle key.
            {
                case 0x01: S(k, 1, 0); break; // swap k[0] and k[1]
                case 0x02: S(k, 2, 3); break; // swap k[2] and k[3]
                case 0x03: S(k, 3, 0); break; // swap k[3] and k[0]
            }
        }
    }
#else
    public static void Encode(ref byte[] avatar) => throw new NotImplementedException();
#endif

    private bool VerifyHmac(byte[] data, byte[] sig)
    {
        // This code doesn't matter. The point is that you're never supposed to be able to get a valid HMAC.
        // It is also somewhat of a red herring, to test the challenger's understanding of the codebase/.NET.

        // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.hmacsha256?view=net-6.0
        using HMACSHA256 hmac = new HMACSHA256(_key);
        using var r = new MemoryStream(data);
        byte[] h = hmac.ComputeHash(r);
        if (h.Length != sig.Length) return false;
        int eq = 0;
        for (var i = 0; i < h.Length; ++i) eq += h[i] ^ sig[i];
        return eq == 0;
    }

    private static byte[] Key = new byte[] { 0xba, 0xad, 0xf0, 0x0d };
}