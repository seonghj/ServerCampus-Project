using DungeonFarming.DBTableFormat;
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

    public Task<ErrorCode> PlayerFarmingItem(Int32 uid, Int32 ItemCode);

    public Task<ErrorCode> PlayerKillNPC(Int32 uid, Int32 NPCCode);

    public Task<List<int>> GetFarmingItemList(Int32 uid);

    public Task<List<int>> GetKilledNPCList(Int32 uid);
}