using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.ModelReqRes;

namespace DungeonFarming.Services;

public interface IGameDb
{

    public Task<ErrorCode> InsertPlayer(string AccountId);

    public Task<ErrorCode> InsertPlayerItem(string UID);

    public Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfo(string AccountId); 
}