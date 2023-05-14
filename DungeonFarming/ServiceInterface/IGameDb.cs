using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;

namespace DungeonFarming.Services;

public interface IGameDb : IDisposable
{
    public Task<(ErrorCode, Int32)> InsertNewPlayer(string AccountId);

    public Task<ErrorCode> InsertPlayerItem(PlayerItem item);

    public Task<ErrorCode> DeletePlayer(Int32 uid);

    public Task<ErrorCode> DeletePlayerItem(Int32 itemUniqueID);

    public Task<ErrorCode> DeleteMail(Int32 mailCode);

    public Task<(ErrorCode, PlayerInfo)> GetPlayerInfo(string AccountId);

    public Task<(ErrorCode, PlayerInfo)> GetPlayerInfo(Int32 uid);

    public Task<(ErrorCode, List<PlayerItem>)> GetPlayerItem(Int32 uid);

    public Task<(ErrorCode, List<Mail>)> GetMailAsync(Int32 uid, Int32 page);

    public Task<(ErrorCode, PlayerItemForClient)> InsertPlayerItemFromMail(Int32 uid, Int32 itemCode, Int32 itemCount);

    public Task<(ErrorCode, PlayerItemForClient)> ReceiveItemFromMail(Int32 uid, Int32 mailcode);

    public Task<ErrorCode> SendAttendenceRewordsMail(Int32 uid);

    public Task<ErrorCode> InAppProductSentToMail(Int32 uid, Int32 productCode, string receiptCode);

    public Task<(ErrorCode, PlayerItem, bool)> EnhanceItem(Int32 uid, Int32 itemUID);

    public Task<(ErrorCode, bool)> CheckAbleStartStage(Int32 uid, Int32 stageCode);

    public List<Int32> GetStageItemInfo(Int32 uid, Int32 stageCode);

    public List<NPCInfo> GetStageNPCInfo(Int32 uid, Int32 stageCode);

    public bool CheckItemExistInStage(Int32 itemCode, Int32 stageCode);

    public bool CheckNPCExistInStage(Int32 NPCCode, Int32 stageCode);

    public ErrorCode CheckCanFarmingItem(Int32 itemCode, Int32 stageCode, List<Int32> farmingItemList);

    public ErrorCode CheckCanKillNPC(Int32 npcCode, Int32 stageCode, List<Int32> currKilledNpc);
}