namespace Mycoverse.Common.Options;

public class CryptographyOptions
{
    private readonly static Random Rand = new Random();
    public string Keyfile { get; set; } = string.Empty;

    public byte[] Key
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Keyfile) && _key == NullKey && File.Exists(Keyfile))
                _key = File.ReadAllBytes(Keyfile);
            return _key;
        }
        
        set
        {
            _key = value;
        }
    }

    public byte[] GenerateIV()
    {
        var iv = new byte[16];
        Rand.NextBytes(iv);
        return iv;
    }

    private byte[] _key = NullKey;

    private static readonly byte[] NullKey = new byte[32];
}