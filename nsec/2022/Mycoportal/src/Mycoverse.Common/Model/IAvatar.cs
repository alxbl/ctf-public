namespace Mycoverse.Common.Model;

public interface IAvatar
{
    string Author { get; }
    Version Version { get; }

    void Render(float time);

    decimal Cost {get;}

    DateTime Released {get; }

    byte[] Signature { get; }
}