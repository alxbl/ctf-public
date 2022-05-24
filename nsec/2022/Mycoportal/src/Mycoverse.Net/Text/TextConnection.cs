namespace Mycoverse.Net;

using System.Text;

public class TextConnection : Connection<TextRequest, TextResponse>
{
    private string _data = "";

    protected override TextRequest? OnReceive(byte[] buf, int size)
    {
        var data = UTF8Encoding.Default.GetString(buf, 0, size);
        _data += data;
        var newline = _data.IndexOf("\n");
        if (newline < 0) return null;
        var msg = _data.Substring(0, newline);
        _data = _data.Substring(newline + 1).Trim();
        return TextRequest.Parse(msg);
    }

    protected override byte[] OnSend(TextResponse msg) => UTF8Encoding.Default.GetBytes(msg.Message);
}

public class TextResponse
{

    public TextResponse(string msg)
    {
        Message = msg;
        if (!Message.EndsWith("\n")) Message += "\n";
    }

    public string Message { get; }
}

public class TextRequest
{
    public static TextRequest Parse(string line)
    {
        var space = line.IndexOf(" ");
        var verb = space < 0 ? line : line.Substring(0, space);
        var body = space < 0 ? string.Empty : line.Substring(space + 1);
        return new TextRequest(verb, body);
    }

    public TextRequest(string verb, string body)
    {
        Verb = verb;
        Body = body;
    }
    public bool IsValid => !string.IsNullOrEmpty(Verb);
    public string Verb { get; } = string.Empty;
    public string Body { get; } = string.Empty;
}