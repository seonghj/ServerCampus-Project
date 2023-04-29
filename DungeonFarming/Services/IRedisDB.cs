using DungeonFarming.ModelDB;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace DungeonFarming.Services;

public interface IRedisDb
{
    public Task<ErrorCode> CreatePlayerAuthAsync(string accountid);

    public Task<Tuple<ErrorCode, AuthPlayer>> GetPlayerAuthAsync(string accountid);

    public Task<Tuple<ErrorCode, bool>> CheckPlayerAuthAsync(string accountid, string playerAuthToken);
}