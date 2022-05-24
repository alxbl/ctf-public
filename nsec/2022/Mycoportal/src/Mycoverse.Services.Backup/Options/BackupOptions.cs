namespace Mycoverse.Services.Backup.Options;

public class BackupOptions
{
    public List<string> Denied { get; set; } = new();
    public List<string> Encrypted { get; set; } = new();
    public string BackupDir {get; set;} = "/var/backups";
    public bool IsDenied(FileInfo file) => Denied?.Contains(file.FullName) ?? false;
    public bool ShouldEncrypt(FileInfo file) => Encrypted?.Contains(file.FullName) ?? false;
}