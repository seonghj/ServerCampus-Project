using DungeonFarming.ModelDB;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using CloudStructures;
using CloudStructures.Structures;
using ZLogger;
using static LogManager;
using Microsoft.Extensions.Options;

namespace DungeonFarming.Services;

public class RedisDb : IRedisDb
{
    public RedisConnection _redisConn;
    private static readonly ILogger<RedisDb> s_logger = GetLogger<RedisDb>();
    readonly IOptions<DbConfig> _dbConfig;

    public RedisDb(IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        var config = new RedisConfig("default", _dbConfig.Value.Redis);
        _redisConn = new RedisConnection(config);

        s_logger.ZLogDebug($"userDbAddress:{dbConfig.Value.Redis}");
    }

    public async Task<ErrorCode> CreatePlayerAuthAsync(string accountid)
    {
        var AuthKey = DateTime.Now.ToString("MMddyyyy") + accountid;
        try
        {
            var defaultExpiry = TimeSpan.FromDays(1);
            var player = new AuthPlayer
            {
                AuthToken = AuthKey,
                State = PlayerState.Default.ToString()
            };

            var redis = new RedisString<AuthPlayer>(_redisConn, accountid, defaultExpiry);
            var result = await redis.SetAsync(player,null);
            if (result == false)
            {
                s_logger.ZLogError(EventIdDic[EventType.LoginAddRedis],
                   $"ID:{accountid}, AuthToken:{AuthKey},ErrorMessage:PlayerBasicAuth, RedisString set Error");
                return ErrorCode.LoginFailAddRedis;
            }
        }
        catch
        {
            s_logger.ZLogError(EventIdDic[EventType.LoginAddRedis],
                   $"ID:{accountid}, AuthToken:{AuthKey},ErrorMessage:Redis Connection Error");
            return ErrorCode.LoginFailAddRedis;
        }
        return ErrorCode.None;
    }

    public async Task<Tuple<ErrorCode, AuthPlayer>> GetPlayerAuthAsync(string accountid)
    {
        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, accountid, null);
            var result = await redis.GetAsync();
            if (!result.HasValue)
            {
                s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Not Assigned Player, RedisString Get Error");
                return new Tuple<ErrorCode, AuthPlayer>(ErrorCode.LoginFailAddRedis, null);
            }
            return new Tuple<ErrorCode, AuthPlayer>(ErrorCode.LoginFailAddRedis, result.Value);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: ID Not Exist");
            return new Tuple<ErrorCode, AuthPlayer>(ErrorCode.LoginFailAddRedis, null);
        }
    }
}