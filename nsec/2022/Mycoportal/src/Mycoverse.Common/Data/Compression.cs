namespace Mycoverse.Common.Data;

using System.Buffers.Text;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
public static class Compression
{
    public static string Compress(byte[] indata)
    {
        using var input = new MemoryStream(indata);

        input.Seek(0, SeekOrigin.Begin);
        using var output = new MemoryStream(4096);
        GZip.Compress(input, output, isStreamOwner: false);

        // 3. Base64
        output.Seek(0, SeekOrigin.Begin);
        // Base64.EncodeToUtf8(output.GetBuffer()[..(int)output.Length], scratch, out var consumed, out var written, isFinalBlock: true);
        return Convert.ToBase64String(output.GetBuffer()[..(int)output.Length]);
    }

    public static byte[] Decompress(string input) {
        var buf = UTF8Encoding.Default.GetBytes(input);
        Base64.DecodeFromUtf8InPlace(buf, out var written );
        using var compressed = new MemoryStream(buf, 0, written);

        using var decompressed = new MemoryStream(4096);
        GZip.Decompress(compressed, decompressed, isStreamOwner: false);
        var reader = new StreamReader(decompressed, Encoding.UTF8);
        var res = new byte[decompressed.Length];
        decompressed.Seek(0, SeekOrigin.Begin);
        decompressed.Read(res, 0, res.Length);
        return res;
    }
}