namespace Mycoverse.Services.Avatar.Options;

public class DatabaseOptions
{
    public string Path { get; set; } = "/app/db.sqlite";
    public int MaxQuery {get; set; } = 10;

    public int InjectDelay { get; set; } = 10;
}