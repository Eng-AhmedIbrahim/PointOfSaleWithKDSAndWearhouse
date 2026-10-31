using System;
using System.Collections.Generic;
using System.Linq;
namespace POS.Services.CashService;

public class RedisService
{
    private readonly ConnectionMultiplexer _redisConnection;

    public RedisService(string redisConnectionString)
    {
        _redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
    }

    public IDatabase GetDatabase() => _redisConnection.GetDatabase();

    public async Task SetConnectionIdAsync(string machineName, string connectionId)
    {
        var db = _redisConnection.GetDatabase();
        await db.StringSetAsync($"connection:{machineName}", connectionId);
    }

    // استرجاع ConnectionId باستخدام اسم الجهاز
    public async Task<string> GetConnectionIdAsync(string machineName)
    {
        var db = _redisConnection.GetDatabase();
        var connectionId = await db.StringGetAsync($"connection:{machineName}");
        return connectionId;
    }

    // إزالة ConnectionId من Redis
    public async Task RemoveConnectionIdAsync(string connectionId)
    {
        var db = _redisConnection.GetDatabase();
        var server = _redisConnection.GetServer(_redisConnection.GetEndPoints()[0]);
        foreach (var key in server.Keys(pattern: $"connection:*"))
        {
            var storedConnectionId = await db.StringGetAsync(key);
            if (storedConnectionId == connectionId)
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }

}
