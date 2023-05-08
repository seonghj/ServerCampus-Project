using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;

namespace DungeonFarming.Services;

public interface IGameDb
{

    public Task<(ErrorCode, string)> InsertPlayer(string AccountId);

    public Task<ErrorCode> InsertPlayerItem(string UID, PlayerItem item);

    public Task<ErrorCode> InsertItemListToPlayer(List<PlayerItem> itemList);

    public Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfo(string AccountId);

    public Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfoIntoUID(string uid);

    public Task<Tuple<ErrorCode, List<PlayerItem>>> GetPlayerItem(string AccountId);

    public Task<Tuple<ErrorCode, List<Mail>>> GetMailAsync(string uid, Int32 page);

    public Task<List<PlayerItem>> MakeItemListFromMail(PlayerInfo playerInfo, List<ItemCodeAndCount> ItemInMail);

    public Task<Tuple<ErrorCode, List<PlayerItem>>> GetMailItemAsync(string uid, string mailcode);

    public Task<Tuple<ErrorCode, PlayerInfo>> LoginAndUpdateAttendenceDay(string accountid);

    public Task<ErrorCode> SendAttendenceRewordsMail(string uid);

    public Task<ErrorCode> InAppProductSentToMail(string uid, Int32 productCode, string receiptCode);

    public Task<Tuple<ErrorCode, PlayerItem>> EnhanceItem(string uid, string ItemUniqueID);
}