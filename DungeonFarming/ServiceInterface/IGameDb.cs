using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;

namespace DungeonFarming.Services;

public interface IGameDb
{

    public Task<(ErrorCode, string)> InsertPlayer(string AccountId);

    public Task<ErrorCode> InsertPlayerItem(string UID, PlayerItem item);

    public Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfo(string AccountId);
    public Task<Tuple<ErrorCode, List<PlayerItem>>> GetPlayerItem(string AccountId);

    public Task<Tuple<ErrorCode, List<Mail>>> GetMailAsync(string uid, Int32 page);

    public Task<Tuple<ErrorCode, List<PlayerItem>>> GetMailItemAsync(string uid, string mailcode);

}