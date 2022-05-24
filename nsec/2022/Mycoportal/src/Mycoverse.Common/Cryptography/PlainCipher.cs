namespace Mycoverse.Common.Cryptography;

public class PlainCipher : ICipher
{
    public void Encrypt(Stream src, Stream dst) => src.CopyTo(dst);
    public void Decrypt(Stream src, Stream dst) => src.CopyTo(dst);
}