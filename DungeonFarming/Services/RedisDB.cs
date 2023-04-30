using DungeonFarming.DBTableFormat;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using CloudStructures;
using CloudStructures.Structures;
using ZLogger;
using static LogManager;
using Microsoft.Extensions.Options;
using DungeonFarming.ResponseFormat;
using static Humanizer.In;

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
            return ErrorCode.RedisDbConnectionFail;
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
            return new Tuple<ErrorCode, AuthPlayer>(ErrorCode.None, result.Value);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: ID Not Exist");
            return new Tuple<ErrorCode, AuthPlayer>(ErrorCode.LoginFailAddRedis, null);
        }
    }

    public async Task<Tuple<ErrorCode, bool>> CheckPlayerAuthAsync(string accountid, string playerAuthToken)
    {
        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, accountid, null);
            var result = await redis.GetAsync();
            if (!result.HasValue)
            {
                s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Not Assigned Player, RedisString Get Error");
                return new Tuple<ErrorCode, bool>(ErrorCode.AuthTokenNotFound, false);
            }
            if (result.Value.AuthToken != playerAuthToken)
            {
                s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Not Assigned Player, RedisString Get Error");
                return new Tuple<ErrorCode, bool>(ErrorCode.AuthTokenMismatch, false);
            }
            return new Tuple<ErrorCode, bool>(ErrorCode.None, true);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Redis Connection Error");
            return new Tuple<ErrorCode, bool>(ErrorCode.RedisDbConnectionFail, false);
        }
    }

    public async Task<Tuple<ErrorCode, List<NoticeContent>>> GetNotificationAsync(string NotificationKey)
    {

        try
        {
            var redis = new RedisDictionary<string, string>(_redisConn, NotificationKey, null);
            var result = await redis.GetAllAsync();
            if (result.Count == 0)
            {
                s_logger.ZLogError(
                   $"ErrorMessage: Can Not Get Notification, RedisString Get Error");
                return new Tuple<ErrorCode, List<NoticeContent>>(ErrorCode.AuthTokenNotFound, null);
            }
            List<NoticeContent> NoticeList = new List<NoticeContent>();
            foreach (var item in result)
            {
                NoticeList.Add(new NoticeContent
                {
                    title = item.Key,
                    Content = item.Value
                    
                });
            }

            return new Tuple<ErrorCode, List<NoticeContent>>(ErrorCode.None, NoticeList);
            //return new Tuple<ErrorCode, List<NoticeContent>>(ErrorCode.None, null);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ErrorMessage: Redis Connection Error");
            return new Tuple<ErrorCode, List<NoticeContent>>(ErrorCode.RedisDbConnectionFail, null);
        }
    }
}