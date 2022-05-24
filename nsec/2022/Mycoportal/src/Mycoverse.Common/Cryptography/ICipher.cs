namespace Mycoverse.Common.Cryptography;

public interface ICipher 
{
    void Encrypt(Stream src, Stream dst);
    void Decrypt(Stream src, Stream dst);
}