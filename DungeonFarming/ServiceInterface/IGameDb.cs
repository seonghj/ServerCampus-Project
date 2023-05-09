using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;

namespace DungeonFarming.Services;

public interface IGameDb : IDisposable
{

    public Task<(ErrorCode, string)> InsertNewPlayer(string AccountId);

    public Task<ErrorCode> InsertPlayerItem(string UID, PlayerItem item);

    public Task<ErrorCode> InsertItemListToPlayer(List<PlayerItem> itemList);

    public Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfo(string AccountId);

    public Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfoIntoUID(string uid);

    public Task<Tuple<ErrorCode, List<PlayerItem>>> GetPlayerItem(string AccountId);

    public Task<Tuple<ErrorCode, List<Mail>>> GetMailAsync(string uid, Int32 page);

    public Task<List<PlayerItem>> MakeItemListFromMail(PlayerInfo playerInfo, List<MailItem> ItemInMail);

    public Task<Tuple<ErrorCode, List<PlayerItem>>> GetMailItemAsync(string uid, string mailcode);

    public Task<Tuple<ErrorCode, PlayerInfo>> UpdateAttendenceDay(string accountid);

    public Task<ErrorCode> SendAttendenceRewordsMail(string uid);

    public Task<ErrorCode> InAppProductSentToMail(string uid, Int32 productCode, string receiptCode);

    public Task<Tuple<ErrorCode, PlayerItem, bool>> EnhanceItem(string uid, string itemUID);

    public Task<Tuple<ErrorCode, bool>> CheckAbleStartStage(string uid, Int32 stageCode);
}