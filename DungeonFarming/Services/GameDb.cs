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
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using System.Diagnostics;


namespace DungeonFarming.Services;

public class GameDb : IGameDb
{
    readonly ILogger<GameDb> _logger;
    readonly IOptions<DbConfig> _dbConfig;
    readonly IMasterData _MasterData;

    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    string[] playerItemCols = new[] { "UID", "ItemCode", "ItemName"
                , "Attack", "Defence", "Magic", "EnhanceCount", "ItemCount"};
    string[] MailCols = new[] { "UID", "Title", "ExpirationDate","IsReceive", "CreatedAt", "ItemCode", "ItemCount"};


    Int32 INF = -987654321;
    Int32 GoldAttributeCode = 5;
    Int32 WeaponAttributeCode = 1;
    Int32 ArmorAttributeCode = 2;
    Int32 PotionAttributeCode = 4;
    Int32 basicWeaponCode = 2;

    Int32 basicLevel = 1;
    Int32 basicExp = 0;
    Int32 basicHp = 100;
    Int32 basicMp = 100;
    Int32 basicGold = 0;

    Int32 mailPageSize = 20;
    string ProductsMailTitle = "구매 상품";
    string AttendanceMailTitle = "출석 보상";

    DateTime attendanceRewordsExpireDate = DateTime.Now.AddDays(30).Date;
    DateTime productsMailExpireDate = DateTime.Now.AddYears(1).Date;

    Int32 EnhanceItemPercentage = 30;
    double EnhanceWeight = 1.1;

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

    private PlayerInfo MakeBasicPlayer(string accountID)
    {
        PlayerInfo basicPlayer = new PlayerInfo
        {
            AccountID = accountID,
            Level = basicLevel,
            Exp = basicExp,
            Hp = basicHp,
            Mp = basicMp,
            Gold = basicGold,
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
            ItemCount = 1
        };

        return basicItem;
    }

    private PlayerItem MakeItem(Int32 uid, Int32 itemCode, Int32 itemCount)
    {
        Item masterItemData = _MasterData.ItemDict[itemCode];
        PlayerItem basicItem = new PlayerItem
        {
            UID = uid,
            ItemCode = itemCode,
            Attack = masterItemData.Attack,
            Defence = masterItemData.Defence,
            Magic = masterItemData.Magic,
            EnhanceCount = 0,
            ItemCount = itemCount
        };

        return basicItem;
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

    public async Task<(ErrorCode, PlayerItemForClient)> InsertPlayerItemFromMail(Int32 uid, Int32 itemCode, Int32 itemCount)
    {
        Int32 PlayerGoldBeforeUpdate = INF;
        Int32 ItemCountBeforeUpdate = INF;
        bool isExist = false;
        try
        {

            Item masterItemData = _MasterData.ItemDict[itemCode];

            PlayerItem insertData = MakeItem(uid, itemCode, itemCount);

            if (_MasterData.ItemDict[itemCode].CanOverlap == true)
            {

                if (_MasterData.ItemDict[itemCode].Attribute == GoldAttributeCode)
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

            var insertItemToPlayer = _queryFactory.Query("PlayerItem").InsertAsync(insertData);
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

    // 우편함 기능
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

            (ErrorCode errorCode, PlayerItemForClient itemInfo) = await InsertPlayerItemFromMail(uid, MailInfo.ItemCode, MailInfo.ItemCount);

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


    // 출석부
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
            DateTime ExpirationDate = DateTime.Now.AddDays(30).Date;


            var insertResult = await _queryFactory.Query("Mail")
                .InsertAsync(MakeMail(uid, AttendanceMailTitle, attendanceRewordsExpireDate, rewords.ItemCode, rewords.Count));
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
        var itemList = _MasterData.InAppProductDict[productCode].Item;
        var insertList = itemList.Select(item => new object[]
                {uid,  $"{ProductsMailTitle}_{productCode}",expirationTime
                ,false, CreateMailTime, item.ItemCode, item.ItemCount}).ToArray();
        try
        {
            var isExist = await _queryFactory.Query("Receipt").Where("ReceiptCode", receiptCode)
                .ExistsAsync();
            if (isExist == true)
            {
                return ErrorCode.ProductAlreadyPaid;
            }

            var insertQuery = _queryFactory.Query("Mail").AsInsert(MailCols, insertList);
            var insertResult = await _queryFactory.ExecuteAsync(insertQuery);

            if (insertResult != insertList.Length)
            {
                DeleteInAppProductsMail(uid, productCode, CreateMailTime, expirationTime);
                _logger.ZLogError(
                   $"ErrorMessage: Send Product Mail Error");
                return ErrorCode.None;
            }

            var insertReceiptResult = await _queryFactory.Query("Receipt").InsertAsync(new
            {
                ReceiptCode = receiptCode,
                UID = uid,
                ProductCode = productCode
            });

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

    // 강화
    public async Task<(ErrorCode, PlayerItem, bool)> EnhanceItem(Int32 uid, Int32 itemUID)
    {
        try
        {
            var itemInfo = await _queryFactory.Query("playerItem").Where("UID", uid)
                .Where("ItemUniqueID", itemUID).FirstOrDefaultAsync<PlayerItem>();

            var itemMasterData = _MasterData.ItemDict[itemInfo.ItemCode];
            var enhanceResult = false;
            if (itemInfo.EnhanceCount < itemMasterData.EnhanceMaxCount)
            {
                Int32 newEnhanceCount = itemInfo.EnhanceCount + 1;
                Random random = new Random();
                if (random.Next(100) < EnhanceItemPercentage)
                {
                    if (itemMasterData.Attribute == WeaponAttributeCode)
                    {
                        var newAttack = (int)Math.Ceiling(itemInfo.Attack * EnhanceWeight);
                        var updateResult = await _queryFactory.Query("PlayerItem").Where("ItemUniqueID", itemUID)
                            .UpdateAsync(new
                                { Attack =  newAttack, EnhanceCount = newEnhanceCount});

                        itemInfo.Attack = newAttack;
                        itemInfo.EnhanceCount = newEnhanceCount;
                    }
                    else if (itemMasterData.Attribute == ArmorAttributeCode)
                    {
                        var newDefence = (int)Math.Ceiling(itemInfo.Defence * EnhanceWeight);
                        var updateResult = await _queryFactory.Query("PlayerItem").Where("ItemUniqueID", itemUID)
                            .UpdateAsync(new
                            { Defence = newDefence, EnhanceCount = newEnhanceCount });

                        itemInfo.Defence = newDefence;
                        itemInfo.EnhanceCount = newEnhanceCount;
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
                return (ErrorCode.None, itemInfo, enhanceResult);
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
            Console.WriteLine(ex.ToString());   
            _logger.ZLogError(
                   $"ErrorMessage: Check Start Stage Error");
            return (ErrorCode.CheckStartStageError, false);
        }
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
}

