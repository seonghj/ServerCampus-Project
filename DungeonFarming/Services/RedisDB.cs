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
using System.Text.Json;
using StackExchange.Redis;
using DungeonFarming.Controllers;
using System.Collections.Generic;
using System.Linq;

namespace DungeonFarming.Services;

public class RedisDb : IRedisDb
{
    private const string NotificationKey = "Notification";
    private const string FarmingItemKey = "FarmingItem_";
    private const string KilledNPCKey = "KilledNPC_";
    private const string ChattingKey = "ChatChannel_";

    private const Int32 maxChatToRecv = 10;

    public RedisConnection _redisConn;
    private static readonly ILogger<RedisDb> s_logger = GetLogger<RedisDb>();

    public void Init(string address)
    {
        var config = new RedisConfig("default", address);
        _redisConn = new RedisConnection(config);
        s_logger.ZLogDebug($"userDbAddress:{address}");

    }

    private string MakeFarmingItemKey(Int32 uid, Int32 stageCode)
    {
        return $"{FarmingItemKey}{stageCode}_{uid}";
    }

    private string MakeKilledNpcKey(Int32 uid, Int32 stageCode)
    {
        return $"{KilledNPCKey}{stageCode}_{uid}";
    }

    private TimeSpan StageDataExpireTime() 
    {
        return TimeSpan.FromHours(RediskeyExpireTime.InStageDataExpireHour);
    }

    private InStageNpc MakeKilledNpcData(Int32 NpcCode, Int32 maxCount)
    {
        InStageNpc data = new InStageNpc()
        {
            NpcCode = NpcCode,
            NpcCount = 1,
            MaxCount = maxCount,
            KillTime = DateTime.Now
        };

        return data;
    }

    private InStageItem MakeFarmingItemData(Int32 ItemCode, Int32 ItemCount, Int32 maxCount)
    {
        InStageItem data = new InStageItem()
        {
            ItemCode = ItemCode,
            ItemCount = ItemCount,
            MaxCount = maxCount,
            FarmingTime = DateTime.Now
        };

        return data;
    }

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

    public async Task<(ErrorCode, bool)> CheckPlayerState(string accountid, PlayerState state)
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
            if (result.Value.State != state.ToString())
            {
                s_logger.ZLogError(
                   $"ID:{accountid}, ErrorMessage: Player State Is Not {state.ToString()}");
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

    public async Task<ErrorCode> ChangePlayerState(string accountId, PlayerState state)
    {
        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, accountId, null);
            var getResult = await redis.GetAsync();
            if (!getResult.HasValue)
            {
                s_logger.ZLogError(
                   $"ID:{accountId}, ErrorMessage: Not Assigned Player, RedisString Get Error");
                return ErrorCode.AuthTokenNotFound;
            }
            var updateValue = getResult.Value;

            updateValue.State = state.ToString(); 
            var setResult = await redis.SetAsync( updateValue );
            if (setResult == false)
            {
                s_logger.ZLogError(
                   $"ID:{accountId}, ErrorMessage: Player State Update Error");
                return ErrorCode.RedisDbConnectionFail;
            }

            return ErrorCode.None;
        }
        catch(Exception ex)
        {
            s_logger.ZLogError(ex,
                   $"ID:{accountId}, ErrorMessage: Redis Connection Error");
            return ErrorCode.RedisDbConnectionFail;
        }
    }

    public async Task<(ErrorCode, List<NoticeContent>)> GetNotificationAsync()
    {

        try
        {
            var redis = new RedisSet<NoticeContent>(_redisConn, NotificationKey, null);
            var result = await redis.MembersAsync();
            if (result.Length == 0)
            {
                s_logger.ZLogError(
                   $"ErrorMessage: Can Not Get Notification, RedisDictionary Get Error");
                return (ErrorCode.NotExistNofitication, null);
            }

            return (ErrorCode.None, result.ToList<NoticeContent>());
        }
        catch
        {
            s_logger.ZLogError(
                   $"ErrorMessage: Redis Connection Error");
            return (ErrorCode.RedisDbConnectionFail, null);
        }
    }

    public async Task<bool> SetRequestLockAsync(string key)
    {
        try
        {
            var redis = new RedisString<AuthPlayer>(_redisConn, key, NxKeyTimeSpan());
            if (await redis.SetAsync(new AuthPlayer
            {}, NxKeyTimeSpan(), StackExchange.Redis.When.NotExists) == false)
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

    public async Task<ErrorCode> PlayerFarmingItem(Int32 uid, Int32 ItemCode, Int32 ItemCount, Int32 stageCode, Int32 maxCount)
    {
        try
        { 
            var redis = new RedisDictionary<Int32, InStageItem>(_redisConn, MakeFarmingItemKey(uid, stageCode)
                , StageDataExpireTime());
            InStageItem insertData = MakeFarmingItemData(ItemCode, ItemCount, maxCount);

            var gerOrSetResult = await redis.GetOrSetAsync(ItemCode, async (key) =>
            {
                return await Task.FromResult(insertData);
            });

            if (gerOrSetResult != null)
            {
                if (gerOrSetResult.FarmingTime != insertData.FarmingTime)
                {
                    Int32 currCount = gerOrSetResult.ItemCount;
                    insertData.ItemCount = currCount + ItemCount;
                    var setResult = await redis.SetAsync(ItemCode, insertData);
                }
            }
            else
            {
                s_logger.ZLogError(
                  $"ErrorMessage: Farming Item Error");
                return ErrorCode.FarmingItemFail;
            }


            return ErrorCode.None;

        }
        catch
        {
            s_logger.ZLogError(
                  $"ErrorMessage: Farming Item Error");
            return ErrorCode.FarmingItemFail;
        }
    }

    public async Task<InStageItem> GetFarmingItem(Int32 uid, Int32 itemCode, Int32 stageCode)
    {
        try
        {
            var redis = new RedisDictionary<Int32, InStageItem>(_redisConn, MakeFarmingItemKey(uid, stageCode), null);
            var redisResult = await redis.GetAsync(itemCode);

            if (redisResult.HasValue == false)
            {
                s_logger.ZLogError(
                  $"ErrorMessage: Farming Item Error");
                return null;
            }
            return redisResult.Value;

        }
        catch (Exception ex)
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Farming Item Error");
            return null;
        }
    }

    public async Task<List<InStageItem>> GetFarmingItemList(Int32 uid, Int32 stageCode)
    {
        try
        {
            var redis = new RedisDictionary<Int32, InStageItem>(_redisConn, MakeFarmingItemKey(uid, stageCode), null);
            var redisResult = await redis.GetAllAsync();

            if (redisResult.Count == 0 || redisResult == null)
            {
                return null;
            }

            List<InStageItem> farmingItemList = new List<InStageItem>();

            foreach (var item in redisResult.ToArray())
            {
                farmingItemList.Add(item.Value);
            }

            return farmingItemList.ToList();

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
            var redis = new RedisDictionary<Int32, InStageItem>(_redisConn, MakeFarmingItemKey(uid, stageCode), null);
            var redisResult = await redis.DeleteAsync();

            if (redisResult == false)
            {
                s_logger.ZLogInformationWithPayload(new {UID = uid},
                 $"ErrorMessage: Farming Item List Already Empty");
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



    public async Task<ErrorCode> PlayerKillNPC(Int32 uid, Int32 NpcCode, Int32 stageCode, Int32 maxCount)
    {
        try
        {
            InStageNpc insertData = MakeKilledNpcData(NpcCode, maxCount);
           
            var redis = new RedisDictionary<Int32, InStageNpc>(_redisConn, MakeKilledNpcKey(uid, stageCode)
                , StageDataExpireTime());
            var gerOrSetResult = await redis.GetOrSetAsync(NpcCode, async (key) =>
            {
                return await Task.FromResult(insertData);
            });
            if (gerOrSetResult != null)
            {
                if (gerOrSetResult.KillTime != insertData.KillTime)
                {
                    Int32 currCount = gerOrSetResult.NpcCount;
                    insertData.NpcCount = currCount + 1;
                    var setResult = await redis.SetAsync(NpcCode, insertData);
                }
            }
            else
            {
                s_logger.ZLogError(
                  $"ErrorMessage: Farming Item Error");
                return ErrorCode.FarmingItemFail;
            }


            return ErrorCode.None;

        }
        catch
        {
            s_logger.ZLogError(
                  $"ErrorMessage: Player Kill NPC Error");
            return ErrorCode.FarmingItemFail;
        }
    }

    public async Task<InStageNpc> GetKilledNPC(Int32 uid, Int32 npcCode, Int32 stageCode)
    {
        try
        {
            var redis = new RedisDictionary<Int32, InStageNpc>(_redisConn, MakeKilledNpcKey(uid, stageCode), null);
            var redisResult = await redis.GetAsync(npcCode);

            if (redisResult.HasValue == false)
            {
                s_logger.ZLogError(
                  $"ErrorMessage: Get Killed NPC Info Error");
                return null;
            }
            return redisResult.Value;

        }
        catch (Exception ex)
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Farming Item Error");
            return null;
        }
    }

    public async Task<List<InStageNpc>> GetKilledNPCList(Int32 uid, Int32 stageCode)
    {
        try
        {
            var redis = new RedisDictionary<Int32, InStageNpc>(_redisConn, MakeKilledNpcKey(uid, stageCode), null);
            var redisResult = await redis.GetAllAsync();

            List<InStageNpc> npcList = new List<InStageNpc>();

            foreach(var dic in redisResult)
            {
                npcList.Add(dic.Value);
            }

            return npcList;

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
            var redis = new RedisList<int>(_redisConn, MakeKilledNpcKey(uid, stageCode), null);
            var redisResult = await redis.DeleteAsync();

            if (redisResult == false)
            {
                s_logger.ZLogInformationWithPayload(new { UID = uid },
                 $"ErrorMessage: Killed Npc List Already Empty");
            }
            return ErrorCode.None;

        }
        catch (Exception ex)
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Delete Killed NPC List Error");
            return ErrorCode.DeleteKilledNPCListFail;
        }
    }



    public async Task<ErrorCode> InitInStageData(Int32 uid, Int32 stageCode)
    {
        var errorCode = await DeleteFarmingItemList(uid, stageCode);
        if (errorCode != ErrorCode.None)
        {
            return errorCode;
        }

        errorCode = await DeleteKilledNPCList(uid, stageCode);
        if (errorCode != ErrorCode.None)
        {
            return errorCode;
        }
        return ErrorCode.None;
    }

    public async Task<ErrorCode> SendChat(Int32 uid, string message)
    {
        try
        {
            var redisConnection = _redisConn.GetConnection();
            var db = redisConnection.GetDatabase();
            var streamName = ChattingKey + "1";

            var chatdata = new ChatInfo
            {
                PlayerUID= uid,
                Message = message,
                Time = DateTime.Now,
            };

            var entryId = await db.StreamAddAsync(streamName
                , "", JsonSerializer.Serialize(chatdata));

            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Send Chat Error");
            return ErrorCode.SendChatFail;
        }
    }

    public async Task<(ErrorCode, List<ChatInfo>)> ReceiveLatestChat(Int32 uid, string messageID)
    {
        try
        {
            var redisConnection = _redisConn.GetConnection();
            var db = redisConnection.GetDatabase();
            var streamName = ChattingKey + "1";

            var startID = messageID;
            var endID = "+";

            var entries = await db.StreamRangeAsync(streamName, startID, endID, maxChatToRecv);

            List<ChatInfo> list = new List<ChatInfo>();
            foreach(var entry in entries)
            {
                ChatInfo ci = JsonSerializer.Deserialize<ChatInfo>(entry.Values.FirstOrDefault().Value);
                list.Add(ci);
            }

            return (ErrorCode.None, list);
        }
        catch (Exception ex)
        {
            s_logger.ZLogError(ex,
                  $"ErrorMessage: Receive Chat Error");
            return (ErrorCode.ReceiveChatFail, null);
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

    public const ushort InStageDataExpireHour = 1;
}
