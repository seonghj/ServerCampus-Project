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
using System.Reflection.Emit;
using DungeonFarming.MasterData;

namespace DungeonFarming.Services;

public class RedisDb : IRedisDb
{
    private const string FarmingItemKey = "FarmingItem_";
    private const string KilledNPCKey = "KilledNPC_";

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

    public async Task<(ErrorCode, AuthPlayer)> GetPlayerAuthAsync(string accountid)
    {
        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, accountid, null);
            var result = await redis.GetAsync();
            if (!result.HasValue)
            {
                s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Not Assigned Player, RedisString Get Error");
                return (ErrorCode.LoginFailAddRedis, null);
            }
            return (ErrorCode.None, result.Value);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: ID Not Exist");
            return (ErrorCode.LoginFailAddRedis, null);
        }
    }

    public async Task<(ErrorCode, bool)> CheckPlayerAuthAsync(string accountid, string playerAuthToken)
    {
        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, accountid, null);
            var result = await redis.GetAsync();
            if (!result.HasValue)
            {
                s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Not Assigned Player, RedisString Get Error");
                return (ErrorCode.AuthTokenNotFound, false);
            }
            if (result.Value.AuthToken != playerAuthToken)
            {
                s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Not Assigned Player, RedisString Get Error");
                return (ErrorCode.AuthTokenMismatch, false);
            }
            return (ErrorCode.None, true);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Redis Connection Error");
            return (ErrorCode.RedisDbConnectionFail, false);
        }
    }

    // 공지
    public async Task<(ErrorCode, List<NoticeContent>)> GetNotificationAsync(string NotificationKey)
    {

        try
        {
            var redis = new RedisDictionary<string, string>(_redisConn, NotificationKey, null);
            var result = await redis.GetAllAsync();
            if (result.Count == 0)
            {
                s_logger.ZLogError(
                   $"ErrorMessage: Can Not Get Notification, RedisDictionary Get Error");
                return (ErrorCode.AuthTokenNotFound, null);
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

            return (ErrorCode.None, NoticeList);
        }
        catch
        {
            s_logger.ZLogError(
                   $"ErrorMessage: Redis Connection Error");
            return (ErrorCode.RedisDbConnectionFail, null);
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

    public async Task<ErrorCode> PlayerFarmingItem(Int32 uid, Int32 ItemCode, Int32 stageCode)
    {
        try
        { 
            var redis = new RedisList<int>(_redisConn, $"{FarmingItemKey}_{stageCode}_{uid}", null);
            var redisResult = await redis.LeftPushAsync(ItemCode, null);
            return ErrorCode.None;

        }
        catch
        {
            s_logger.ZLogError(
                  $"ErrorMessage: Farming Item Error");
            return ErrorCode.FarmingItemFail;
        }
    }

    public async Task<List<int>> GetFarmingItemList(Int32 uid, Int32 stageCode)
    {
        try
        {
            var redis = new RedisList<int>(_redisConn, $"{FarmingItemKey}_{stageCode}_{uid}", null);
            var redisResult = await redis.RangeAsync();
            return redisResult.ToList();

        }
        catch(Exception ex) 
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Farming Item Error");
            return null;
        }
    }

    public async Task<ErrorCode> DeleteFarmingItemList(Int32 uid, Int32 stageCode)
    {
        try
        {
            var redis = new RedisList<int>(_redisConn, $"{FarmingItemKey}_{stageCode}_{uid}", null);
            var redisResult = await redis.DeleteAsync();

            if (redisResult == false)
            {
                s_logger.ZLogError(
                 $"ErrorMessage: Delete Farming Item List Error");
                return ErrorCode.DeleteFarmingItemListFail;
            }
            return ErrorCode.None;

        }
        catch (Exception ex)
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Farming Item Error");
            return ErrorCode.DeleteFarmingItemListFail;
        }
    }

    public async Task<ErrorCode> PlayerKillNPC(Int32 uid, Int32 NPCCode, Int32 stageCode)
    {
        try
        {
            var redis = new RedisList<int>(_redisConn, $"{KilledNPCKey}_{stageCode}_{uid}", null);
            var redisResult = await redis.LeftPushAsync(NPCCode, null);

            return ErrorCode.None;

        }
        catch
        {
            s_logger.ZLogError(
                  $"ErrorMessage: Player Kill NPC Error");
            return ErrorCode.FarmingItemFail;
        }
    }

    public async Task<List<int>> GetKilledNPCList(Int32 uid, Int32 stageCode)
    {
        try
        {
            var redis = new RedisList<int>(_redisConn, $"{KilledNPCKey}_{stageCode}_{uid}", null);
            var redisResult = await redis.RangeAsync();
            return redisResult.ToList();

        }
        catch
        {
            s_logger.ZLogError(
                  $"ErrorMessage: Farming Item Error");
            return null;
        }
    }

    public async Task<ErrorCode> DeleteKilledNPCList(Int32 uid, Int32 stageCode)
    {
        try
        {
            var redis = new RedisList<int>(_redisConn, $"{KilledNPCKey}_{stageCode}_{uid}", null);
            var redisResult = await redis.DeleteAsync();

            if (redisResult == false)
            {
                s_logger.ZLogError(
                 $"ErrorMessage: Delete Killed NPC List Error");
                return ErrorCode.DeleteKilledNPCListFail;
            }
            return ErrorCode.None;

        }
        catch (Exception ex)
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Farming Item Error");
            return ErrorCode.DeleteKilledNPCListFail;
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
