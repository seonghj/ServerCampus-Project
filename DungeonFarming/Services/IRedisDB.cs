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

    public Task<Tuple<ErrorCode, AuthPlayer>> GetPlayerAuthAsync(string accountid);

    public Task<Tuple<ErrorCode, bool>> CheckPlayerAuthAsync(string accountid, string playerAuthToken);

    public Task<Tuple<ErrorCode, List<NoticeContent>>> GetNotificationAsync(string NotificationKey);

    public Task<ErrorCode> SetNotificationAsync(string NotificationKey, string title, string Content);

    public Task<bool> SetRequestLockAsync(string key);

    public Task<bool> DeleteRequestLockAsync(string key);
}