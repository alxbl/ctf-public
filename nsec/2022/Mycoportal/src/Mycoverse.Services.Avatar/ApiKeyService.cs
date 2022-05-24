namespace Mycoverse.Services.Avatar;

using Microsoft.Extensions.Logging;
using Mycoverse.Common.Model;

public class ApiKeyService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ApiKeyService> _log;

    public ApiKeyService(DatabaseService db, ILogger<ApiKeyService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<bool> ValidateAsync(Session s, CancellationToken k)
    {
        if (s.Username == "guest") return true;
        if (string.IsNullOrWhiteSpace(s.ApiKey)) return false;

        var keys = await _db.GetUserKeysAsync(s, k);
        return keys?.Contains(s.ApiKey) == true;
    }

    public async Task<bool?> AddAsync(Session s, string key, CancellationToken k)
    {
        if (!await ValidateAsync(s, k)) return null;
        return await _db.AddApiToken(s, key, k);
    }

    public async Task<bool?> RemoveAsync(Session s, string key, CancellationToken k)
    {
        if (!await ValidateAsync(s, k)) return null;
        return await _db.RemoveApiToken(s, key, k);
    }

    public async Task<IList<string>?> ListAsync(Session s, CancellationToken k)
    {
        if (!await ValidateAsync(s, k)) return null;
        return await _db.GetUserKeysAsync(s, k);
    }
}