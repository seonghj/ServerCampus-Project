using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using DungeonFarming.ResponseFormat;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace DungeonFarming.Services;

public interface IRedisDb
{
    public void Init(string address);

    public Task<ErrorCode> InsertPlayerAuthAsync(string accountid);

    public Task<(ErrorCode, AuthPlayer)> GetPlayerAuthAsync(string accountid);

    public Task<(ErrorCode, bool)> CheckPlayerAuthAsync(string accountid, string playerAuthToken);

    public Task<(ErrorCode, List<NoticeContent>)> GetNotificationAsync(string NotificationKey);

    public Task<ErrorCode> SetNotificationAsync(string NotificationKey, string title, string Content);

    public Task<bool> SetRequestLockAsync(string key);

    public Task<bool> DeleteRequestLockAsync(string key);

    public Task<ErrorCode> PlayerFarmingItem(Int32 uid, Int32 ItemCode, Int32 ItemCount, Int32 stageCode, Int32 maxCount);

    public Task<ErrorCode> PlayerKillNPC(Int32 uid, Int32 NpcCode, Int32 stageCode, Int32 maxCount);

    public Task<InStageItem> GetFarmingItem(Int32 uid, Int32 itemCode, Int32 stageCode);

    public Task<List<InStageItem>> GetFarmingItemListAll(Int32 uid, Int32 stageCode);

    public Task<InStageNpc> GetKilledNPC(Int32 uid, Int32 NpcCode, Int32 stageCode);

    public Task<List<InStageNpc>> GetKilledNPCList(Int32 uid, Int32 stageCode);

    public Task<ErrorCode> DeleteFarmingItemList(Int32 uid, Int32 stageCode);

    public Task<ErrorCode> DeleteKilledNPCList(Int32 uid, Int32 stageCode);
}