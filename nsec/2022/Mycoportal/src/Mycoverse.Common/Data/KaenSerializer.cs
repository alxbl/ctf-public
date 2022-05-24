namespace Mycoverse.Common.Data;

using System.Buffers.Text;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

using System.Reflection;
using System.Diagnostics;
using System.Net;

// https://en.wikipedia.org/wiki/Podostroma_cornu-damae
// This is a very naive serializer which handles plain old datatypes
// and an arbitrary constructor, invoked by reflection.
//
public class KaenSerializer
{
    public static readonly string ObjectStart = "🍄";
    public static readonly string FieldEnd = "🛑";


    // The idea is that the Session is going to deserialize and create a Process object with a StartInfo, and then the gadget is the
    // session object itself. The way to find out what the gadget is, is by turning on debug.
    // The session itself cannot be changed, but it has an "object" field that can be abused.
    // Errors are displayed with `debug`, so messing with the serialized state leads to figuring out what the exploitable member is.

    // The mushroom is actually used to invoke a method on the type, maybe for list insertion or something.

    // Ran out of time to code a proper serializer and it would've made the challenge way too difficult anyway.
    public void Serialize(Stream dst, object graph)
    {
        using var w = new StreamWriter(dst, Encoding.UTF8, 4096, true);

        var t = graph.GetType();

        var fullName = t.FullName;
        var path = t.Assembly.Location;
        var serType = "file://" + path + "!" + fullName;
        w.Write(serType);
        w.Write(ObjectStart);
        foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var name = prop.Name;
            var val = prop.GetValue(graph);
            WriteValue(name, val, w);
        }
    }

    public T? Deserialize<T>(Stream src)
    {
        using var r = new StreamReader(src, Encoding.UTF8, false, 4096, true);

        var dstType = typeof(T);
        var buf = r.ReadToEnd();

        var parts = buf.Split("!");
        var asmLoc = parts[0];
        var data = parts[1];


        Uri.TryCreate(asmLoc, new UriCreationOptions {
            DangerousDisablePathAndQueryCanonicalization = false,
        }, out var uri);


        var asm = uri!.Scheme switch {
            "file" => Assembly.LoadFrom(uri.AbsolutePath),
            "http" => LoadFromRemote(uri),
            "https" => LoadFromRemote(uri),
            _ => throw new InvalidOperationException("Unsupported protocol.")
        };

        // Assembly should be loaded, get the type.
        parts = data.Split(ObjectStart);
        var typeName = parts[0];
        data = parts[1];

        var obj = asm.CreateInstance(typeName);
        var type = obj!.GetType();

        // Read fields and set them.
        var fields = data.Split(FieldEnd);
        foreach (var f in fields) {
            if (string.IsNullOrWhiteSpace(f)) continue; // Last field.

            string? fieldName = null;
            object? fvalue = f switch {
                _ when f.Contains("💬") => ReadValue(f, "💬", out fieldName),
                _ when f.Contains("❓") => ReadValue(f, "❓", out fieldName),
                _ when f.Contains("🐜") => ReadValue(f, "🐜", out fieldName),
                _ => null
            };

            if (fvalue is null || string.IsNullOrEmpty(fieldName)) continue; // ignore.
            var prop = type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            prop!.SetValue(obj, fvalue);
        }
        return (T)obj;
    }

    private Assembly LoadFromRemote(Uri uri)
    {
        var req = WebRequest.Create(uri);
        var rsp = req.GetResponse();

        var r = rsp.GetResponseStream();
        var ms = new MemoryStream((int)rsp.ContentLength);
        r.CopyTo(ms);
        return Assembly.Load(ms.ToArray());
    }

    private object? ReadValue(string data, string splitter, out string fieldName)
    {
        var parts = data.Split(splitter);
        fieldName = parts[0];
        return splitter switch {
            "💬" => parts[1],
            "❓" => bool.Parse(parts[1]),
            "🐜" => int.Parse(parts[1]),
            _ => null,
        };
    }
    private void WriteValue(string field, object? val, StreamWriter dst)
    {
        if (val is null) return; // Skip null fields.

        var type = val.GetType().FullName switch
        {
            "System.String" when string.IsNullOrEmpty(val as string) => null,
            "System.String" => "💬",
            "System.Boolean" => "❓",
            "System.Int32" => "🐜",
            _ => null
        };

        if (type is null) return; // Not supported.
        dst.Write(field);
        dst.Write(type);
        dst.Write(val.ToString()!);
        dst.Write(FieldEnd);
    }
}