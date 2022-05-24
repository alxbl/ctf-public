namespace Mycoverse.Services.Avatar;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;

using Mycoverse.Common.Model;
using Mycoverse.Services.Avatar.Options;

public class DatabaseService : IDisposable
{
    private readonly ILogger<DatabaseService> _log;
    private readonly SqliteConnection _db;
    private readonly DatabaseOptions _opts;
    private readonly SemaphoreSlim _quota;

    public DatabaseService(ILogger<DatabaseService> log, IOptions<DatabaseOptions> opts)
    {
        _log = log;
        _opts = opts.Value;
        _db = new SqliteConnection($"Data Source={_opts.Path}");
        _db.Open();
        _quota = new SemaphoreSlim(_opts.MaxQuery, _opts.MaxQuery);
        _log.LogInformation("Using database '{0}' with maximum {1} concurrent queries", _opts.Path, _opts.MaxQuery);
    }
    public async Task<IList<string>> GetUserKeysAsync(Session s, CancellationToken k)
    {
        await _quota.WaitAsync(k);
        try
        {
            var result = new List<string>();
            var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT k.token FROM Users u LEFT JOIN ApiKeys k ON u.id = k.uid WHERE u.name = $name";
            cmd.Parameters.AddWithValue("$name", s.Username);

            using var reader = cmd.ExecuteReader();

            if (_opts.InjectDelay > 0) Thread.Sleep(_opts.InjectDelay);
            
            while (reader.Read()) result.Add(reader.GetString(0));
            return result;
        }
        finally
        {
            _quota.Release();
        }
    }

    public async Task<bool> RemoveApiToken(Session s, string token, CancellationToken k)
    {
        await _quota.WaitAsync(k);
        try
        {
            var id = GetUserId(s.Username);
            if (id is null) return false;

            var cmd = _db.CreateCommand();
            cmd.CommandText = "DELETE FROM ApiKeys WHERE uid = $id AND token = $token LIMIT 1;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$token", token);

            var affected = await cmd.ExecuteNonQueryAsync(k);
            _log.LogDebug("Inserted API Key for User: {0}, {1}: {2}", id, token, affected);
            return affected > 0;
        }
        finally
        {
            _quota.Release();
        }
    }

    public async Task<bool> AddApiToken(Session s, string token, CancellationToken k)
    {
        await _quota.WaitAsync(k);
        try
        {
            var id = GetUserId(s.Username);
            if (id is null) return false;

            var cmd = _db.CreateCommand();
            cmd.CommandText = "INSERT INTO ApiKeys (uid, token) VALUES ($id, $token);";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$token", token);

            var affected = await cmd.ExecuteNonQueryAsync(k);
            return affected > 0;
        }
        catch (Exception e)
        {
            _log.LogError(e, "Boom");
            return false;
        }
        finally
        {
            _quota.Release();
        }

    }

    public void Dispose() => _db.Dispose();

    private long? GetUserId(string username)
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT id FROM Users u WHERE u.name = $name";
        cmd.Parameters.AddWithValue("$name", username);

        var id = cmd.ExecuteScalar();
        return id as long?;
    }
}