using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CloudStructures.Structures;
using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;
using SqlKata;

namespace DungeonFarming.Services;

public class GameDb : IGameDb
{
    readonly ILogger<GameDb> _logger;
    readonly IOptions<DbConfig> _dbConfig;
    readonly IMasterData _MasterData;

    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    string[] MailCols = new[] { "UID", "Title", "ExpirationDate","IsReceive", "CreatedAt", "ItemCode", "ItemCount"};

    Int32 INF = -987654321;
    
    Int32 basicWeaponCode = 2;

    Int32 mailPageSize = 20;

    DateTime mailExpireDate = DateTime.Now.AddDays(30).Date;
    DateTime productsMailExpireDate = DateTime.Now.AddYears(1).Date;

    public GameDb(ILogger<GameDb> logger, IOptions<DbConfig> dbConfig
        , IMasterData masterData)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        GameDBOpen();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new SqlKata.Execution.QueryFactory(_dbConn, _compiler);
        _MasterData = masterData;
    }

    private void GameDBOpen()
    {
        _dbConn = new MySqlConnection(_dbConfig.Value.GameDb);

        _dbConn.Open();
    }

    private void GameDBClose()
    {
        _dbConn = new MySqlConnection(_dbConfig.Value.GameDb);

        _dbConn.Close();
    }

    public void Dispose()
    {
        GameDBClose();
    }

    private PlayerInfo MakeBasicPlayer(string accountID)
    {

        PlayerInfo basicPlayer = new PlayerInfo
        {
            AccountID = accountID,
            Level = BasicPlayerStatus.Level,
            Exp = BasicPlayerStatus.Exp,
            Hp = BasicPlayerStatus.Hp,
            Mp = BasicPlayerStatus.Mp,
            Gold = BasicPlayerStatus.Gold,
            LastLoginTime = DateTime.Now.Date,
            ConsecutiveLoginDays = 0,
            LastClearStage = 0
        };

        return basicPlayer;
    }

    private PlayerItem MakeBasicItem(Int32 uid)
    {
        PlayerItem basicItem = new PlayerItem
        {
            UID = uid,
            ItemCode = _MasterData.ItemDict[basicWeaponCode].Code,
            Attack = _MasterData.ItemDict[basicWeaponCode].Attack,
            Defence = _MasterData.ItemDict[basicWeaponCode].Defence,
            Magic = _MasterData.ItemDict[basicWeaponCode].Magic,
            EnhanceCount = 0,
            ItemCount = 1,
            CreatedAt = DateTime.Now
        };

        return basicItem;
    }

    private PlayerItem MakeItem(Int32 uid, Int32 itemCode, Int32 itemCount)
    {
        Item masterItemData = _MasterData.ItemDict[itemCode];
        PlayerItem Item = new PlayerItem
        {
            UID = uid,
            ItemCode = itemCode,
            Attack = masterItemData.Attack,
            Defence = masterItemData.Defence,
            Magic = masterItemData.Magic,
            EnhanceCount = 0,
            ItemCount = itemCount,
            CreatedAt = DateTime.Now
        };

        return Item;
    }

    private PlayerItemForClient MakePlayerItemForClient(PlayerItem item)
    {
        PlayerItemForClient iteminfo = new PlayerItemForClient
        {
            ItemCode = item.ItemCode,
            Attack = item.Attack,
            Defence = item.Defence,
            EnhanceCount = item.EnhanceCount,
            Magic = item.Magic,
            ItemCount = item.ItemCount,
            CreatedAt = item.CreatedAt
        };

        return iteminfo;
    }

    private PlayerItemForClient MakePlayerItemForClient(Int32 itemCode, Int32 itemCount)
    {
        Item masterItemData = _MasterData.ItemDict[itemCode];
        PlayerItemForClient iteminfo = new PlayerItemForClient
        {
            ItemCode = itemCode,
            Attack = masterItemData.Attack,
            Defence = masterItemData.Defence,
            EnhanceCount = 0,
            Magic = masterItemData.Magic,
            ItemCount = itemCount,
            CreatedAt = DateTime.Now
        };

        return iteminfo;
    }

    private Mail MakeMail(Int32 uid, string title, DateTime expirationDate, Int32 itemCode, Int32 itemCount)
    {
        DateTime CreateMailTime = DateTime.Now;

        Mail newMail = new Mail
        {
            UID = uid,
            Title = title,
            ExpirationDate = expirationDate,
            IsReceive = false,
            CreatedAt = CreateMailTime,
            ItemCode = itemCode,
            ItemCount = itemCount
        };

        return newMail;
    }

    private Receipt MakeReceipt(Int32 uid, Int32 productsCode, string receiptCode)
    {
        return new Receipt
        {
            UID = uid,
            ReceiptCode = receiptCode,
            ProductCode = productsCode
        };
    }

    private async void DeleteInAppProductsMail(Int32 uid, Int32 productCode, DateTime createTime, DateTime expirationTime)
    {
        var itemList = _MasterData.InAppProductDict[productCode].Item;

        foreach (var item in itemList)
        {
            var deleteResult = await _queryFactory.Query("Mail")
                .Where("UID", uid).Where("ExpirationDate", expirationTime).Where("CreatedAt", createTime)
                .Where("ItemCode", item.ItemCode).Where("ItemCount", item.ItemCount)
                .DeleteAsync();
        }
    }

    public async Task<(ErrorCode, Int32)> InsertNewPlayer(string AccountId)
    {
        try
        {
            var InsertPlayerRes = await _queryFactory.Query("Playerinfo").InsertAsync(MakeBasicPlayer(AccountId));

            var uid = await _queryFactory.Query("Playerinfo").Where("AccountID", AccountId)
                .Select("UID").FirstOrDefaultAsync<int>();

            PlayerItem basicItem = MakeBasicItem(uid);

            var InsertBasicItemRes = await _queryFactory.Query("PlayerItem").InsertAsync(basicItem);

            return (ErrorCode.None, uid);
        }
        catch (Exception ex)
        {
            var result = DeletePlayer(AccountId);
            _logger.ZLogError(ex,
                $"[GameDb.InsertPlayer] ErrorCode : {ErrorCode.CreatePlayerFailException}");
            return (ErrorCode.CreatePlayerFailException, -1);
        }

    }

    public async Task<ErrorCode> DeletePlayer(string accountID)
    {
        try
        {
            var result = await _queryFactory.Query("PlayerInfo").Where("AccountID", accountID).DeleteAsync();
            if (result == 0)
            {
                _logger.ZLogError(
                $"[GameDb.Delete Player] ErrorMessage : Player Not Exist / AccountID: {accountID}");
                return ErrorCode.DeletePlayerFail;
            }
            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDb.Delete Player] ErrorCode : {ErrorCode.DeletePlayerFail}");
            return ErrorCode.DeletePlayerFail;
        }
    }

    public async Task<ErrorCode> DeletePlayer(Int32 uid)
    {
        try
        {
            var result = await _queryFactory.Query("PlayerInfo").Where("UID", uid).DeleteAsync();
            if (result == 0)
            {
                _logger.ZLogError(
                $"[GameDb.Delete Player] ErrorMessage : Player Not Exist / UID: {uid}");
                return ErrorCode.DeletePlayerFail;
            }
            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDb.Delete Player] ErrorCode : {ErrorCode.DeletePlayerFail}");
            return ErrorCode.DeletePlayerFail;
        }
    }

    public async Task<ErrorCode> InsertPlayerItem(PlayerItem item)
    {
        try
        {
            var result = await _queryFactory.Query("PlayerItem").InsertAsync(item);

            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDb.InsertItem] ErrorCode : {ErrorCode.InsertPlayerItemFail}");
            return ErrorCode.CreatePlayerFailException;
        }
    }

    public async Task<ErrorCode> DeletePlayerItem(Int32 itemUniqueID)
    {
        try
        {
            var result = await _queryFactory.Query("PlayerItem")
                .Where("ItemUniqueID", itemUniqueID).DeleteAsync();

            if (result == 0)
            {
                _logger.ZLogError(
               $"[GameDb.Delete Item] ErrorMessage : Player Item Not Exist / ItemUID: {itemUniqueID}");
                return ErrorCode.DeleteItemFail;
            }
            return ErrorCode.None;
        }
        catch
        {
            _logger.ZLogError(
              $"[GameDb.Delete Item] ErrorCode : {ErrorCode.DeleteItemFail} / ItemUID: {itemUniqueID}");
            return ErrorCode.DeleteItemFail;
        }
    }

    public async Task<ErrorCode> DeletePlayerItem(Int32 uid, Int32 itemCode, DateTime createdAt)
    {
        try
        {
            var result = await _queryFactory.Query("PlayerItem").Where("UID", uid)
                .Where("ItemCode", itemCode).Where("CreatedAt", createdAt).Limit(1).DeleteAsync();

            if (result == 0)
            {
                _logger.ZLogError(
               $"[GameDb.Delete Item] ErrorMessage : Player Item Not Exist / ItemCode: {itemCode} / CreatedAt: {createdAt}");
                return ErrorCode.DeleteItemFail;
            }
            return ErrorCode.None;
        }
        catch
        {
            _logger.ZLogError(
              $"[GameDb.Delete Item] ErrorCode : {ErrorCode.DeleteItemFail} / ItemCode: {itemCode} / CreatedAt: {createdAt}");
            return ErrorCode.DeleteItemFail;
        }
    }

    public async Task<ErrorCode> DeleteMail(Int32 mailCode)
    {
        try
        {
            var result = await _queryFactory.Query("Mail")
                .Where("MailCode", mailCode).DeleteAsync();

            if (result == 0)
            {
                _logger.ZLogError(
               $"[GameDb.Delete Mail] ErrorMessage : Mail Not Exist / MailCode: {mailCode}");
                return ErrorCode.DeleteMailFail;
            }
            return ErrorCode.None;
        }
        catch
        {
            _logger.ZLogError(
              $"[GameDb.Delete Item] ErrorCode : {ErrorCode.DeleteMailFail} / MailCode: {mailCode}");
            return ErrorCode.DeleteMailFail;
        }
    }

    public async Task<(ErrorCode, PlayerItemForClient)> InsertPlayerItem(Int32 uid, Int32 itemCode, Int32 itemCount)
    {
        Int32 PlayerGoldBeforeUpdate = INF;
        Int32 ItemCountBeforeUpdate = INF;
        bool isExist = false;
        try
        {

            Item masterItemData = new Item();

            PlayerItem insertData = MakeItem(uid, itemCode, itemCount);

            if (_MasterData.ItemDict[itemCode].CanOverlap == true)
            {

                if (_MasterData.ItemDict[itemCode].Attribute == ItemAttribute.Gold)
                {
                    PlayerGoldBeforeUpdate = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                    .Select("Gold").FirstAsync<int>();
                    var updatePlayerGold = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                        .IncrementAsync("Gold", itemCount);
                }

                isExist = await _queryFactory.Query("PlayerItem").Where("itemCode", itemCode)
                    .ExistsAsync();

                if (isExist == true)
                {
                    ItemCountBeforeUpdate = await _queryFactory.Query("PlayerItem").Where("ItemCode", itemCode)
                    .Select("ItemCount").FirstAsync<int>();
                    var updateItemCount = await _queryFactory.Query("PlayerItem").Where("UID", uid)
                        .Where("ItemCode", itemCode)
                       .IncrementAsync("ItemCount", itemCount);

                    return (ErrorCode.None, MakePlayerItemForClient(insertData));
                }
            }

            var insertItemToPlayer = await _queryFactory.Query("PlayerItem").InsertAsync(insertData);

            return (ErrorCode.None, MakePlayerItemForClient(insertData));
        }
        catch(Exception ex) 
        {
            if(PlayerGoldBeforeUpdate != INF)
            {
               await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                        .UpdateAsync(new { Gold = PlayerGoldBeforeUpdate });
            }

            if (ItemCountBeforeUpdate != INF)
            {
                await _queryFactory.Query("PlayerItem").Where("UID", uid).Where("ItemCode", itemCode)
                       .UpdateAsync(new {ItemCount = ItemCountBeforeUpdate });
            }

            _logger.ZLogError(ex,
                   $"ErrorMessage: Insert Item List to Player Error");
            return (ErrorCode.InsertPlayerItemFail, null);
        }
    }



    public async Task<(ErrorCode, PlayerInfo)> GetPlayerInfo(string AccountId)
    {
        try
        {
            var PlayerInfomation = await _queryFactory.Query("playerinfo").Where("AccountID", AccountId).FirstOrDefaultAsync<PlayerInfo>();

            return (ErrorCode.None, PlayerInfomation);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.GetPlayer] ErrorCode : {ErrorCode.GetPlayerInfoFail}");
            return (ErrorCode.None, null);
        }
    }

    public async Task<(ErrorCode, PlayerInfo)> GetPlayerInfo(Int32 uid)
    {
        try
        {
            var PlayerInfomation = await _queryFactory.Query("playerinfo").Where("UID", uid).FirstOrDefaultAsync<PlayerInfo>();

            if (PlayerInfomation == null)
            {
                return (ErrorCode.GetPlayerInfoFail, null);
            }

            return (ErrorCode.None, PlayerInfomation);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.GetPlayer] ErrorCode : {ErrorCode.GetPlayerInfoFail}");
            return (ErrorCode.GetPlayerInfoFail, null);
        }
    }

    public async Task<(ErrorCode, List<PlayerItem>)> GetPlayerItem(Int32 uid)
    {
        try
        {
            var PlayerItems = await _queryFactory.Query("playerItem")
                .Where("UID", uid).GetAsync<PlayerItem>();
            
            return (ErrorCode.None, PlayerItems.ToList<PlayerItem>());
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.GetPlayerItems] ErrorCode : {ErrorCode.GetPlayerItemsFail}");
            return (ErrorCode.GetPlayerItemsFail, null);
        }
    }

    public async Task<(ErrorCode, List<Mail>)> GetMailAsync(Int32 uid, Int32 page)
    {
        try
        {
            var Mails = await _queryFactory.Query("Mail").Where("UID", uid)
                .Where("ExpirationDate", ">=", DateTime.Now.Date)
                .OrderBy("CreatedAt")
                .ForPage(page, mailPageSize).GetAsync<Mail>();

            return (ErrorCode.None, Mails.ToList<Mail>());
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Get Mail Error");
            return (ErrorCode.GetMailFail, null);
        }
    }

    public async Task<(ErrorCode, Int32)> GetPlayerAttendenceDays(Int32 uid)
    {
        try
        {
            var result = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                .Select("ConsecutiveLoginDays").FirstOrDefaultAsync<Int32>();

            return (ErrorCode.None, result);
        }
        catch(Exception ex)
        {
            _logger.ZLogError(ex, 
                       $"ErrorMessage: Get Player Attendence Days Error");
            return (ErrorCode.GetPlayerAttendenceDaysFail, 0);
        }
    }

    public async Task<(ErrorCode, PlayerItemForClient)> ReceiveItemFromMail(Int32 uid, Int32 mailcode)
    {
        try
        {
            var MailInfo = await _queryFactory.Query("Mail").Where("MailCode", mailcode).Where("IsReceive",false)
                .FirstOrDefaultAsync<Mail>();
            if (MailInfo == null)
            {
                return (ErrorCode.MailIsNotExist, null);
            }

            if (MailInfo.ExpirationDate < DateTime.Now.Date)
            {
                return (ErrorCode.MailExpirationDateOut, null);
            }

            (ErrorCode errorCode, PlayerItemForClient itemInfo) = await InsertPlayerItem(uid, MailInfo.ItemCode, MailInfo.ItemCount);

            if (errorCode != ErrorCode.None)
            {
                _logger.ZLogError(
                   $"ErrorMessage: Get Item In Mail Error");
                return (errorCode, null);
            }

            var updateIsReceive = _queryFactory.Query("mail").Where("MailCode", mailcode)
                .UpdateAsync(new {IsReceive = true});

            return (errorCode, itemInfo);
        }
        catch(Exception ex)
        {
            _logger.ZLogError(ex,
                   $"ErrorMessage: Get Item In Mail Error");
            return (ErrorCode.ReceiveMailItemFail, null);
        }
    }

    public async Task<ErrorCode> InsertItemToMail(Int32 uid, object[][] itemList)
    {
        try
        {
            var insertQuery = _queryFactory.Query("Mail").AsInsert(MailCols, itemList);
            var insertResult = await _queryFactory.ExecuteAsync(insertQuery);

            if(insertResult != itemList.Length)
            {
                _logger.ZLogError(
                  $"ErrorMessage: Insert Item To Mail Error");
                return ErrorCode.SetMailFail;
            }

            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                  $"ErrorMessage: Insert Item To Mail Error");
            return ErrorCode.SetMailFail;
        }
    }

    public async Task<ErrorCode> InsertItemToMail(Int32 uid, ItemCodeAndCount item, string title, DateTime expirationDate)
    {
        try
        {
            var insertResult = await _queryFactory.Query("Mail")
               .InsertAsync(MakeMail(uid, title, expirationDate, item.ItemCode, item.ItemCount));
            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                  $"ErrorMessage: Insert Item To Mail Error");
            return ErrorCode.SetMailFail;
        }
    }

    public async Task<ErrorCode> SendAttendenceRewordsMail(Int32 uid)
    {
        PlayerInfo playerInfo = null;
        try
        {
            (ErrorCode errorCode, playerInfo) = await GetPlayerInfo(uid);
            if(errorCode != ErrorCode.None)
            {
                _logger.ZLogError(
                   $"ErrorMessage: Update Attendence Day Error / ErrorCode : {ErrorCode.GetPlayerInfoFail}");
                return ErrorCode.SendAttendenceRewordsFail;
            }

            TimeSpan DateDifference = DateTime.Now.Date - playerInfo.LastLoginTime.Date;
            Int32 NextAttendenceDay = 0;

            if (DateDifference.Days != 1)
            {
                NextAttendenceDay = 1;
            }
            else NextAttendenceDay = playerInfo.ConsecutiveLoginDays + 1;


            var updateResult = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                    .UpdateAsync(new
                    {
                        ConsecutiveLoginDays = NextAttendenceDay,
                        LastLoginTime = DateTime.Now.Date
                    });

            if (updateResult == 0)
            {
                if (errorCode != ErrorCode.None)
                {
                    _logger.ZLogError(
                       $"ErrorMessage: Update Attendence Day Error / ErrorMessage : PlayerInfo Update Fail");
                    return ErrorCode.SendAttendenceRewordsFail;
                }
            }

            Attendance rewords = _MasterData.AttendanceDict[NextAttendenceDay];

            DateTime CreateMailTime = DateTime.Now;

            ItemCodeAndCount item = new ItemCodeAndCount { ItemCode = rewords.ItemCode, ItemCount = rewords.Count };

            var insertResult = await InsertItemToMail(uid, item
            , MailTitle.AttendanceRewords, mailExpireDate);
           
            if (insertResult == 0)
            {
                updateResult = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                    .UpdateAsync(new
                    {
                        ConsecutiveLoginDays = playerInfo.ConsecutiveLoginDays,
                        LastLoginTime = playerInfo.LastLoginTime
                    });
            }

            return ErrorCode.None;
        }
        catch
        {
            if (playerInfo != null)
            {
                var updateResult = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                    .UpdateAsync(new
                    {
                        ConsecutiveLoginDays = playerInfo.ConsecutiveLoginDays,
                        LastLoginTime = playerInfo.LastLoginTime
                    });
            }
            _logger.ZLogError(
                   $"ErrorMessage: Send Attendence Rewords Error");
            return ErrorCode.SendAttendenceRewordsFail;
        }
    }


    // 인앱 구매

    public async Task<ErrorCode> InAppProductSentToMail(Int32 uid, Int32 productCode, string receiptCode)
    {
        DateTime CreateMailTime = DateTime.Now;
        DateTime expirationTime = productsMailExpireDate;
        var itemList = new List<ItemCodeAndCount>(_MasterData.InAppProductDict[productCode].Item);
        var insertList = itemList.Select(item => new object[]
                {uid,  MailTitle.Products, expirationTime
                ,false, CreateMailTime, item.ItemCode, item.ItemCount}).ToArray();
        try
        {
            var isExist = await _queryFactory.Query("Receipt").Where("ReceiptCode", receiptCode)
                .ExistsAsync();
            if (isExist == true)
            {
                return ErrorCode.ProductAlreadyPaid;
            }

            var insertResult = InsertItemToMail(uid, insertList);

            var insertReceiptResult = await _queryFactory.Query("Receipt")
                .InsertAsync(MakeReceipt(uid, productCode, receiptCode));

            if (insertReceiptResult == 0)
            {
                DeleteInAppProductsMail(uid, productCode, CreateMailTime, expirationTime);
                _logger.ZLogError(
                   $"ErrorMessage: Send Product Mail Error");
                return ErrorCode.None;
            }

            return ErrorCode.None;
        }
        catch(Exception ex)
        {
            DeleteInAppProductsMail(uid, productCode, CreateMailTime, expirationTime);
            _logger.ZLogError(ex,
                   $"ErrorMessage: Send Product Mail Error");
            return ErrorCode.None;
        }
    }

    public async Task<(ErrorCode, PlayerItemForClient)> EnhanceWeapon(PlayerItem weapon)
    {
        try
        {
            Int32 newEnhanceCount = weapon.EnhanceCount + 1;
            var newAttack = (int)Math.Ceiling(weapon.Attack * EnhanceWeight);
            var updateResult = await _queryFactory.Query("PlayerItem").Where("ItemUniqueID", weapon.ItemUniqueID)
                .UpdateAsync(new
                { Attack = newAttack, EnhanceCount = weapon.EnhanceCount + 1 });

            PlayerItemForClient enhancedWeapon = new PlayerItemForClient
            {
                ItemCode = weapon.ItemCode,
                Attack = newAttack,
                Defence = weapon.Defence,
                Magic = weapon.Magic,
                EnhanceCount = newEnhanceCount,
                ItemCount = weapon.ItemCount,
                CreatedAt = weapon.CreatedAt,
            };

            return (ErrorCode.None, enhancedWeapon);
        }
        catch(Exception ex)
        {
            _logger.ZLogError(ex,
                   $"ErrorMessage: Weapon Enhance Disable");
            return (ErrorCode.ItemEnhanceDisable, null);
        }
    }

    public async Task<(ErrorCode, PlayerItemForClient)> EnhanceArmor(PlayerItem armor)
    {
        try
        {
            Int32 newEnhanceCount = armor.EnhanceCount + 1;
            var newDefence = (int)Math.Ceiling(armor.Defence * EnhanceWeight);
            var updateResult = await _queryFactory.Query("PlayerItem").Where("ItemUniqueID", armor.ItemUniqueID)
                .UpdateAsync(new
                { Defence = newDefence, EnhanceCount = armor.EnhanceCount + 1 });

            PlayerItemForClient enhancedArmor = new PlayerItemForClient
            {
                ItemCode = armor.ItemCode,
                Attack = newDefence,
                Defence = armor.Defence,
                Magic = armor.Magic,
                EnhanceCount = newEnhanceCount,
                ItemCount = armor.ItemCount,
                CreatedAt = armor.CreatedAt,
            };

            return (ErrorCode.None, enhancedArmor);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                   $"ErrorMessage: Armor Enhance Disable");
            return (ErrorCode.ItemEnhanceDisable, null);
        }
    }

    public async Task<(ErrorCode, PlayerItemForClient, bool)> EnhanceItem(Int32 uid, Int32 itemUID)
    {
        try
        {
            var itemInfo = await _queryFactory.Query("playerItem").Where("UID", uid)
                .Where("ItemUniqueID", itemUID).FirstOrDefaultAsync<PlayerItem>();

            var enhanceMaxCount = _MasterData.ItemDict[itemInfo.ItemCode].EnhanceMaxCount;
            var itemAttribute = _MasterData.ItemDict[itemInfo.ItemCode].Attribute;
            var enhanceResult = false;

            PlayerItemForClient enhancedItem = null;

            if (itemInfo.EnhanceCount < enhanceMaxCount)
            { 
                Random random = new Random();
                if (random.Next(100) < EnhanceItemPercentage)
                {
                    if (itemAttribute == ItemAttribute.Weapon)
                    {
                        (var errorCode, enhancedItem) = await EnhanceWeapon(itemInfo);

                        if (errorCode != ErrorCode.None)
                        {
                            return (errorCode, null, false);
                        }
                    }
                    else if (itemAttribute == ItemAttribute.Armor)
                    {
                        (var errorCode, enhancedItem) = await EnhanceArmor(itemInfo);

                        if (errorCode != ErrorCode.None)
                        {
                            return (errorCode, null, false);
                        }
                    }

                    _logger.ZLogInformationWithPayload(new { ItemUniqueID = itemUID },
                   $"Enhance item success: attack: {itemInfo.Attack} / Defence: {itemInfo.Defence} / EnhanceCount: {itemInfo.EnhanceCount}");

                    enhanceResult = true;
                }
                else
                {
                    var res = await _queryFactory.Query("PlayerItem").Where("ItemUniqueID", itemUID)
                            .DeleteAsync();
                    _logger.ZLogInformationWithPayload(new { ItemUniqueID = itemUID },
                   $"Enhance Fail");

                    enhanceResult = false;
                }
                return (ErrorCode.None, enhancedItem, enhanceResult);
            }
            else
            {
                _logger.ZLogError(
                   $"ErrorMessage: Item Enhance Disable");
                return (ErrorCode.ItemEnhanceDisable, null, false);
            }

        }
        catch {
            _logger.ZLogError(
                   $"ErrorMessage: Enhance Item Error");
            return (ErrorCode.ItemEnhanceError, null, false);
        }
    }


    public async Task<(ErrorCode, bool)> CheckAbleStartStage(Int32 uid, Int32 stageCode)
    {
        try
        {
            PlayerInfo playerInfo = await _queryFactory.Query("PlayerInfo")
                .Where("UID", uid).FirstOrDefaultAsync<PlayerInfo>();

            if (playerInfo == null)
            {
                return (ErrorCode.GetPlayerInfoFail, false);
            }
            if (playerInfo.LastClearStage + 1 < stageCode)
            {
                return (ErrorCode.None, false);
            }

            return (ErrorCode.None, true);
        }
        catch(Exception ex) 
        {
            _logger.ZLogError(ex,
                   $"ErrorMessage: Check Start Stage Error");
            return (ErrorCode.CheckStartStageError, false);
        }
    }

    public List<ItemCodeAndCount> GetStageItemInfo(Int32 stageCode)
    {
        return _MasterData.StageItemDict[stageCode].ItemInfoList;
    }

    public bool CheckItemExistInStage(Int32 itemCode, Int32 stageCode)
    {
        return _MasterData.StageItemDict[stageCode].ItemCode.Contains(itemCode);
    }

    public Int32 GetItemMaxCount(Int32 itemCode, Int32 stageCode)
    {
        return _MasterData.StageItemDict[stageCode].ItemCount[itemCode];
    }

    public ErrorCode CheckCanFarmingItem(Int32 itemCode, Int32 itemCount, Int32 stageCode, InStageItem currFarmingItem)
    {
        if (CheckItemExistInStage(itemCode, stageCode) == false)
        {
            return ErrorCode.NotExistItemInStage;
        }

        if (currFarmingItem == null)
        {
            return ErrorCode.None;
        }

        Int32 currCount = currFarmingItem.ItemCount;
        Int32 maxCount = currFarmingItem.MaxCount;

        if (currCount + itemCount > maxCount)
        {
            return ErrorCode.NotExistItemInStage;
        }
        return ErrorCode.None;
    }

    public List<NPCInfo> GetStageNPCInfo(Int32 stageCode)
    {
        return _MasterData.StageNPCDict[stageCode].NPCInfoList;
    }

    public (List<ItemCodeAndCount>, List<NPCInfo>) GetStageInfo(Int32 stageCode)
    {
        return (GetStageItemInfo(stageCode), GetStageNPCInfo(stageCode));
    }

    public Int32 GetNPCMaxCount(Int32 NpcCode, Int32 stageCode)
    {
        return _MasterData.StageNPCDict[stageCode].NPCCount[NpcCode];
    }

    public bool CheckNPCExistInStage(Int32 NPCCode, Int32 stageCode)
    {
        bool isExist = false;

        foreach (var npc in _MasterData.StageNPCDict[stageCode].NPCInfoList)
        {
            if (npc.NPCCode == NPCCode) { isExist = true; break; }
        }

        return isExist;
    }

    public ErrorCode CheckCanKillNPC(Int32 npcCode, Int32 stageCode, InStageNpc currKilledNpc)
    {
        if (CheckNPCExistInStage(npcCode, stageCode) == false)
        {
            return ErrorCode.NotExistItemInStage;
        }

        if (currKilledNpc == null)
        {
            return ErrorCode.None;
        }

        Int32 currCount = currKilledNpc.NpcCount;
        Int32 maxCount = currKilledNpc.MaxCount;

        if (currCount + 1 > maxCount)
        {
            return ErrorCode.NotExistItemInStage;
        }
        return ErrorCode.None;
    }

    public ErrorCode CheckClearStage(Int32 stageCode, List<InStageNpc> currKilledNpc)
    {
        if (currKilledNpc.Count == 0)
        {
            return ErrorCode.PlayerClearStageDisable;
        }

        Dictionary<int, int> leftNPCCount = new Dictionary<int, int>(_MasterData.StageNPCDict[stageCode].NPCCount);

        foreach (var npc in currKilledNpc)
        {
            leftNPCCount[npc.NpcCode] -= npc.NpcCount;

            if (leftNPCCount[npc.NpcCode] > 0)
            {
                return ErrorCode.PlayerClearStageDisable;
            }
        }
        return ErrorCode.None;
    }

    public async Task<(ErrorCode, List<PlayerItemForClient>)> EarnItemAfterStageClear(Int32 uid, List<InStageItem> earnItemList)
    {
        List<PlayerItemForClient> itemList = new List<PlayerItemForClient>();

        if (earnItemList == null || earnItemList.Count == 0)
        {
            return (ErrorCode.None, null);
        }

        try
        {
            foreach (var it in earnItemList)
            {
                bool canOverlap = _MasterData.ItemDict[it.ItemCode].CanOverlap;

                if (canOverlap == false)
                {
                    for (int i = 0; i <  it.ItemCount; i++) 
                    {
                        itemList.Add(MakePlayerItemForClient(it.ItemCode, 1));
                    }
                }
                else
                {
                    itemList.Add(MakePlayerItemForClient(it.ItemCode, it.ItemCount));
                }
            }


            var insertList = itemList.Select(item => new object[]
                {uid,  MailTitle.StageClearRewords ,mailExpireDate
                ,false, DateTime.Now.Date, item.ItemCode, item.ItemCount}).ToArray();

            var insertErrorCode = await InsertItemToMail(uid, insertList);

            if (insertErrorCode != ErrorCode.None)
            {
                return (insertErrorCode, null);
            }

            return (ErrorCode.None, itemList);
        }

       catch (Exception ex)
        {
            if (itemList.Count > 0)
            {
                foreach(var item in itemList)
                {
                    await DeletePlayerItem(uid, item.ItemCode, item.CreatedAt);
                }
            }

            _logger.ZLogError(ex,
                   $"ErrorMessage: Earn Stage Item Rewords Error");
            return (ErrorCode.EarnStageClearItemRewordsFail, null);
        }
    }

    public async Task<(ErrorCode, Int32)> EarnExpAfterClearStage(Int32 uid, Int32 stageCode, List<InStageNpc> killedNpcs)
    {
        Int32 earnExp = 0;
        try
        {
            foreach(var npcInfo in killedNpcs)
            {
                Int32 npcExp = _MasterData.StageNPCDict[stageCode].NPCExp[npcInfo.NpcCode];
                earnExp += npcExp * npcInfo.NpcCount;
            }

            var updateResult = await UpdatePlayerEXP(uid, earnExp);

            if (updateResult != ErrorCode.None)
            {
                return (updateResult, 0);
            }

            return (ErrorCode.None, earnExp);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                   $"ErrorMessage: Earn Stage Exp Rewords Error");
            return (ErrorCode.EarnStageClearExpRewordsFail, 0);
        }
    }

    public async Task<ErrorCode> UpdateLastClearStage(Int32 uid, Int32 stageCode)
    {
        try
        {
            var updateResult = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                .UpdateAsync(new { LastClearStage = stageCode });

            if (updateResult == 0)
            {
                _logger.ZLogError(
                   $"ErrorMessage: Update Player Last Clear Stage Error");
                return ErrorCode.UpdateLastClearStageFail;
            }

            return ErrorCode.None; 
        }
        catch (Exception ex) {

            _logger.ZLogError(ex,
                   $"ErrorMessage: Update Player Last Clear Stage Error");
            return ErrorCode.UpdateLastClearStageFail;
        }
    }

    public async Task<ErrorCode> UpdatePlayerEXP(Int32 uid, Int32 exp)
    {
        try
        {
            var updateResult = await _queryFactory.Query("PlayerInfo").Where("UID", uid).
                IncrementAsync("EXP", exp);

            if (updateResult == 0)
            {
                _logger.ZLogError(
                   $"ErrorMessage: Update Player Exp Error");
                return ErrorCode.UpdatePlayerExpFail;
            }

            return ErrorCode.None;
        }
        catch (Exception ex)
        {

            _logger.ZLogError(ex,
                   $"ErrorMessage: Update Player Exp Error");
            return ErrorCode.UpdatePlayerExpFail;
        }
    }
}

public class BasicPlayerStatus
{
    public const Int32 Level = 1;
    public const Int32 Exp = 0;
    public const Int32 Hp = 100;
    public const Int32 Mp = 100;
    public const Int32 Gold = 0;
}

public class ItemAttribute
{
    public const Int32 Weapon = 1;
    public const Int32 Armor = 2;
    public const Int32 Costume = 3;
    public const Int32 Potion = 4;
    public const Int32 Gold = 5;
}

public class MailTitle
{
    public const string Products = "구매 상품";
    public const string AttendanceRewords = "출석 보상";
    public const string StageClearRewords = "스테이지 클리어 보상";
}

public class ItemEnhanceSetting
{
    public const Int32 SuccessPercentage = 30;
    public const double Weight = 1.1;
}