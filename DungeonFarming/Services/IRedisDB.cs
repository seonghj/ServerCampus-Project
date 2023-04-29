using DungeonFarming.ModelDB;
using System;
using System.Threading.Tasks;

namespace DungeonFarming.Services;

public interface IRedisDb
{
    public void Init(string address);

    public Task<ErrorCode> CreatePlayerAuthAsync(string accountid);

    public Task<Tuple<ErrorCode, AuthPlayer>> GetPlayerAuthAsync(string accountid);

}