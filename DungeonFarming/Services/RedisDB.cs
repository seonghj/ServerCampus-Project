using DungeonFarming.DBTableFormat;
using DungeonFarming.Service;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using CloudStructures;
using CloudStructures.Structures;
using ZLogger;
using static LogManager;
using Microsoft.Extensions.Options;
using DungeonFarming.ResponseFormat;

namespace DungeonFarming.Services;

public class RedisDb : IRedisDb
{
    public RedisConnection _redisConn;
    private static readonly ILogger<RedisDb> s_logger = GetLogger<RedisDb>();

    public void Init(string address)
    {
        var config = new RedisConfig("default", address);
        _redisConn = new RedisConnection(config);

        s_logger.ZLogDebug($"userDbAddress:{address}");
    }

    // 인증키
    public async Task<ErrorCode> InsertPlayerAuthAsync(string accountid)
    {
        var AuthKey = Service.Security.CreatePlayerAuth(accountid);
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

    // 공지
    public async Task<Tuple<ErrorCode, List<NoticeContent>>> GetNotificationAsync(string NotificationKey)
    {

        try
        {
            var redis = new RedisDictionary<string, string>(_redisConn, NotificationKey, null);
            var result = await redis.GetAllAsync();
            if (result.Count == 0)
            {
                s_logger.ZLogError(
                   $"ErrorMessage: Can Not Get Notification, RedisDictionary Get Error");
                return new Tuple<ErrorCode, List<NoticeContent>>(ErrorCode.AuthTokenNotFound, null);
            }
            var NoticeList = new List<NoticeContent>();
            foreach (var item in result)
            {
                NoticeList.Add(new NoticeContent
                {
                    title = item.Key,
                    Content = item.Value
                    
                });
            }

            return new Tuple<ErrorCode, List<NoticeContent>>(ErrorCode.None, NoticeList);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ErrorMessage: Redis Connection Error");
            return new Tuple<ErrorCode, List<NoticeContent>>(ErrorCode.RedisDbConnectionFail, null);
        }
    }

    public async Task<ErrorCode> SetNotificationAsync(string NotificationKey, string title, string Content)
    {
        try
        {
            var redis = new RedisDictionary<string, string>(_redisConn, NotificationKey, null);
            var result = await redis.SetAsync(title, Content);
            if (!result)
            {
                s_logger.ZLogError(
                   $"ErrorMessage: Can Not Set Notification, RedisDictionary Set Error");
                return ErrorCode.AuthTokenNotFound;
            }
            
            return ErrorCode.None;
        }
        catch
        {
            s_logger.ZLogError(
                   $"ErrorMessage: Redis Connection Error");
            return ErrorCode.RedisDbConnectionFail;
        }
    }

    public async Task<bool> SetRequestLockAsync(string key)
    {
        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, key, NxKeyTimeSpan());
            if (await redis.SetAsync(new AuthPlayer
            {
            }, NxKeyTimeSpan(), StackExchange.Redis.When.NotExists) == false)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteRequestLockAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, key, null);
            var redisResult = await redis.DeleteAsync();
            return redisResult;
        }
        catch
        {
            return false;
        }
    }

    public TimeSpan NxKeyTimeSpan()
    {
        return TimeSpan.FromSeconds(RediskeyExpireTime.NxKeyExpireSecond);
    }
}

public class RediskeyExpireTime
{
    public const ushort NxKeyExpireSecond = 3;
    public const ushort RegistKeyExpireSecond = 6000;
    public const ushort LoginKeyExpireMin = 60;
    public const ushort TicketKeyExpireSecond = 6000;
}
