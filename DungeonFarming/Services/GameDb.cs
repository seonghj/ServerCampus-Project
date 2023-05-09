using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using CloudStructures.Structures;
using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;
using System.Collections;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SqlKata;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static Humanizer.In;

namespace DungeonFarming.Services;

public class GameDb : IGameDb
{
    readonly ILogger<GameDb> _logger;
    readonly IOptions<DbConfig> _dbConfig;
    readonly IMasterData _MasterData;

    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    string[] playerItemCols = new[] { "UID", "ItemCode", "ItemUniqueID", "ItemName"
                , "Attack", "Defence", "Magic", "EnhanceCount", "ItemCount", "IsBreak"};
    string[] MailItemCols = new[] { "UID", "MailCode", "ItemCode", "ItemCount"};

    Int32 MoneyAttributeCode = 5;
    Int32 WeaponAttributeCode = 1;
    Int32 ArmorAttributeCode = 2;
    Int32 basicWeaponCode = 2;

    Int32 basicLevel = 1;
    Int32 basicExp = 0;
    Int32 basicHp = 100;
    Int32 basicMp = 100;
    Int32 basicGold = 0;

    Int32 mailPageSize = 20;

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

    public async Task<(ErrorCode, string)> InsertNewPlayer(string AccountId)
    {
        var uid = Service.Security.CreateUID();
        try
        {
            var Result = await _queryFactory.Query("Playerinfo").InsertAsync(new
            {
                AccountID = AccountId,
                UID = uid,
                Level = basicLevel,
                Exp = basicExp,
                Hp = basicHp,
                Mp = basicMp,
                Gold = basicGold,
                LastLoginTime = DateTime.Now.Date,
                ConsecutiveLoginDays = 0,
                LastClearStage = 0
            });

            PlayerItem basicItem = new PlayerItem
            {
                UID = uid,
                ItemCode = _MasterData.ItemDict[basicWeaponCode].Code,
                ItemUniqueID = Service.Security.MakeItemUniqueID(basicWeaponCode),
                ItemName = _MasterData.ItemDict[basicWeaponCode].Name,
                Attack = _MasterData.ItemDict[basicWeaponCode].Attack,
                Defence = _MasterData.ItemDict[basicWeaponCode].Defence,
                Magic = _MasterData.ItemDict[basicWeaponCode].Magic,
                EnhanceCount = 0,
                ItemCount = 1,
                IsBreak = false
            };

            Result = await _queryFactory.Query("PlayerItem").InsertAsync(basicItem);

            return (ErrorCode.None, uid);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDb.InsertPlayer] ErrorCode : {ErrorCode.CreatePlayerFailException}");
            return (ErrorCode.CreatePlayerFailException, uid);
        }

    }

    public async Task<ErrorCode> InsertPlayerItem(string UID, PlayerItem item)
    {
        try
        {
            var Result = await _queryFactory.Query("PlayerItem").InsertAsync(item);

            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDb.InsertItem] ErrorCode : {ErrorCode.InsertPlayerItemFail}");
            return ErrorCode.CreatePlayerFailException;
        }
    }

    public async Task<ErrorCode> InsertItemListToPlayer(List<PlayerItem> itemList)
    {
        try
        {
            if (itemList.Count() != 0)
            {
                var insertData = itemList.Select(item => new object[]
                { item.UID, item.ItemCode, item.ItemUniqueID, item.ItemName, item.Attack,
                item.Defence, item.Magic, item.EnhanceCount, item.ItemCount, item.IsBreak}).ToArray();

                var insertItemToPlayer = _queryFactory.Query("PlayerItem").AsInsert(playerItemCols, insertData);
                await _queryFactory.ExecuteAsync(insertItemToPlayer);
            }
            return ErrorCode.None;
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Insert Item List to Player Error");
            return ErrorCode.InsertPlayerItemFail;
        }
    }

    public async Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfo(string AccountId)
    {
        try
        {
            var PlayerInfomation = await _queryFactory.Query("playerinfo").Where("AccountID", AccountId).FirstOrDefaultAsync<PlayerInfo>();

            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.None, PlayerInfomation);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.GetPlayer] ErrorCode : {ErrorCode.GetPlayerInfoFail}");
            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.None, null);
        }
    }

    public async Task<Tuple<ErrorCode, PlayerInfo>> GetPlayerInfoIntoUID(string uid)
    {
        try
        {
            var PlayerInfomation = await _queryFactory.Query("playerinfo").Where("UID", uid).FirstOrDefaultAsync<PlayerInfo>();

            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.None, PlayerInfomation);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.GetPlayer] ErrorCode : {ErrorCode.GetPlayerInfoFail}");
            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.None, null);
        }
    }

    public async Task<Tuple<ErrorCode, List<PlayerItem>>> GetPlayerItem(string uid)
    {
        try
        {
            var PlayerItems = await _queryFactory.Query("playerItem")
                .Where("UID", uid).Where("IsBreak", false).GetAsync<PlayerItem>();
            
            return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.None, PlayerItems.ToList<PlayerItem>());
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.GetPlayerItems] ErrorCode : {ErrorCode.GetPlayerItemsFail}");
            return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.GetPlayerItemsFail, null);
        }
    }

    // 우편함 기능
    public async Task<Tuple<ErrorCode, List<Mail>>> GetMailAsync(string uid, Int32 page)
    {
        try
        {
            var Mails = await _queryFactory.Query("Mail").Where("UID", uid).Where("IsRead", 0)
                .Where("ExpirationDate", ">=", DateTime.Now.Date)
                .OrderBy("CreatedAt")
                .ForPage(page, mailPageSize).GetAsync<Mail>();

            return new Tuple<ErrorCode, List<Mail>>(ErrorCode.None, Mails.ToList<Mail>());
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Get Mail Error");
            return new Tuple<ErrorCode, List<Mail>>(ErrorCode.GetMailFail, null);
        }
    }

    public async Task<List<PlayerItem>> MakeItemListFromMail(PlayerInfo playerInfo, List<MailItem> ItemInMail)
    {
        try
        {
            List<PlayerItem> ItemList = new List<PlayerItem>();
            var uid = playerInfo.UID;
            foreach (var it in ItemInMail)
            {
                var itemMasterData = _MasterData.ItemDict[it.ItemCode];
                if (itemMasterData.Attribute == MoneyAttributeCode)
                {
                    var updatePlayerGold = _queryFactory.Query("PlayerInfo").Where("UID", uid)
                        .AsUpdate(new { Gold = playerInfo.Gold + it.ItemCount });
                    await _queryFactory.ExecuteAsync(updatePlayerGold);
                }
                if (itemMasterData.CanOverlap == true)
                {
                    var itemInfo = await _queryFactory.Query("PlayerItem").Where("UID", uid)
                        .Where("ItemCode", it.ItemCode).FirstOrDefaultAsync<PlayerItem>();


                    if (itemInfo != null)
                    {
                        var updateItem = _queryFactory.Query("PlayerItem").Where("ItemUniqueID", itemInfo.ItemUniqueID)
                        .AsUpdate(new { ItemCount = itemInfo.ItemCount + it.ItemCount });
                        await _queryFactory.ExecuteAsync(updateItem);
                        continue;
                    }
                }
                Item itemMasterdata = _MasterData.getItemData(it.ItemCode);
                ItemList.Add(new PlayerItem
                {
                    UID = uid,
                    ItemCode = it.ItemCode,
                    ItemUniqueID = Service.Security.MakeItemUniqueID(it.ItemCode),
                    ItemName = itemMasterdata.Name,
                    Attack = itemMasterdata.Attack,
                    Defence = itemMasterdata.Defence,
                    Magic = itemMasterdata.Magic,
                    EnhanceCount = 0,
                    ItemCount = it.ItemCount,
                    IsBreak = false,
                });
            }
            return ItemList;
        }
        catch {
            _logger.ZLogError(
                   $"ErrorMessage: Update CanOverlap Item Count Error");
            return null;
        }

    }

    public async Task<Tuple<ErrorCode, List<PlayerItem>>> GetMailItemAsync(string uid, string mailcode)
    {
        try
        {
            (ErrorCode errorCode, PlayerInfo playerInfo) = await GetPlayerInfoIntoUID(uid);

            var MailInfo = await _queryFactory.Query("Mail").Where("MailCode", mailcode)
                .SelectRaw("IsRead, ExpirationDate").FirstOrDefaultAsync();
            if (MailInfo.IsRead == 1) return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.AlreadyGetMailItem, null);
            if (MailInfo.ExpirationDate < DateTime.Now.Date) return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.MailExpirationDateOut, null);

            var mailItems = await _queryFactory.Query("MailItem")
                .Where("UID", uid).Where("MailCode", mailcode)
                .GetAsync<MailItem>();

            List<PlayerItem> ItemList = await MakeItemListFromMail(playerInfo, mailItems.ToList<MailItem>());

            errorCode = await InsertItemListToPlayer(ItemList);
            var updateIsRead = _queryFactory.Query("Mail").Where("UID", uid).Where("MailCode", mailcode)
                    .AsUpdate(new { IsRead = 1 });
            await _queryFactory.ExecuteAsync(updateIsRead);
            return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.None, ItemList);
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Get Item In Mail Error");
            return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.GetMailItemFail, null);
        }
    }


    // 출석부
    public async Task<Tuple<ErrorCode, PlayerInfo>> LoginAndUpdateAttendenceDay(string accountid)
    {
        try
        {
            (ErrorCode errorCode, PlayerInfo playerInfo) = await GetPlayerInfo(accountid);
            if (errorCode != ErrorCode.None)
            {
                return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.PlayerLoginFail, null);
            }

            TimeSpan DateDifference = DateTime.Now.Date - playerInfo.LastLoginTime.Date;
            Int32 NextAttendenceDay = 0;

            if (DateDifference.Days != 1)
            {
                NextAttendenceDay = 1;
            }
            else NextAttendenceDay = playerInfo.ConsecutiveLoginDays + 1;


            
            var result = await _queryFactory.Query("PlayerInfo").Where("AccountID", accountid)
                    .UpdateAsync(new
                    {
                        ConsecutiveLoginDays = NextAttendenceDay,
                        LastLoginTime = DateTime.Now.Date
                    });
            playerInfo.LastLoginTime = DateTime.Now.Date;
            playerInfo.ConsecutiveLoginDays = NextAttendenceDay;

            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.None, playerInfo);
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: PlayerLogin  Error");
            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.PlayerLoginFail, null);
        }
    }

    public async Task<ErrorCode> SendAttendenceRewordsMail(string uid)
    {
        try
        {
            (ErrorCode errorCode, PlayerInfo playerInfo) = await GetPlayerInfoIntoUID(uid);
            if(errorCode != ErrorCode.None)
            {
                return ErrorCode.SendAttendenceRewordsFail;
            }
            Attendance rewords = _MasterData.AttendanceDict[playerInfo.ConsecutiveLoginDays];

            string mailCode = Service.Security.MakeMailKey();
            var result = await _queryFactory.Query("Mail").InsertAsync(new Mail
            {
                UID = uid,
                MailCode = mailCode,
                Title = "출석 보상",
                Content = playerInfo.ConsecutiveLoginDays.ToString() + "일 출석 보상",
                ExpirationDate = DateTime.Now.AddDays(30).Date,
                IsRead = false,
                CreatedAt = DateTime.Now,
            });

            result = await _queryFactory.Query("MailItem").InsertAsync(new MailItem
            {
                UID = uid,
                MailCode = mailCode,
                ItemCode = rewords.ItemCode,
                ItemCount = rewords.Count
            }) ;

            return ErrorCode.None;
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Send Attendence Rewords Error");
            return ErrorCode.SendAttendenceRewordsFail;
        }
    }


    // 인앱 구매
    public async Task<ErrorCode> InAppProductSentToMail(string uid, Int32 productCode, string receiptCode)
    {
        try
        {
            var isExist = await _queryFactory.Query("Receipt").Where("ReceiptCode", receiptCode)
                .ExistsAsync();
            if (isExist == true)
            {
                return ErrorCode.ProductAlreadyPaid;
            }

            var result = await _queryFactory.Query("Receipt").InsertAsync(new
            {
                ReceiptCode = receiptCode,
                UID = uid,
                ProductCode = productCode
            });

            var mailCode = Service.Security.MakeMailKey();
            result = await _queryFactory.Query("Mail").InsertAsync(new Mail
            {
                UID = uid,
                MailCode = mailCode,
                Title = "구매 상품",
                Content = "구매 상품 " + productCode + "번",
                ExpirationDate = DateTime.Now.AddYears(1).Date,
                IsRead = false,
                CreatedAt = DateTime.Now,
            });
            var itemList = _MasterData.InAppProductDict[productCode].Item;

            string[] MailItemCols = new[] { "UID", "MailCode", "ItemCode", "ItemCount" };
            var insertList = itemList.Select(item => new object[]
                {uid, mailCode, item.ItemCode, item.ItemCount}).ToArray();
            Console.WriteLine(insertList.Length);
            var insertQuery = _queryFactory.Query("MailItem").AsInsert(MailItemCols, insertList);
            await _queryFactory.ExecuteAsync(insertQuery);

            return ErrorCode.None;
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex.ToString());
            _logger.ZLogError(
                   $"ErrorMessage: Send Product Mail Error");
            return ErrorCode.None;
        }
    }

    // 강화
    public async Task<Tuple<ErrorCode, PlayerItem, bool>> EnhanceItem(string uid, string itemUID)
    {
        try
        {
            var item = await _queryFactory.Query("playerItem").Where("UID", uid)
                .Where("ItemUniqueID", itemUID).Where("IsBreak", false).FirstOrDefaultAsync<PlayerItem>();

            var itemMasterData = _MasterData.ItemDict[item.ItemCode];
            var enhanceResult = false;
            if (item.EnhanceCount < itemMasterData.EnhanceMaxCount)
            {
                item.EnhanceCount++;
                Random random = new Random();
                if (random.Next(10) < 3)
                {
                    if (itemMasterData.Attribute == WeaponAttributeCode)
                        item.Attack = (int)Math.Ceiling(item.Attack * 1.1);
                    else if(itemMasterData.Attribute == ArmorAttributeCode)
                        item.Defence = (int)Math.Ceiling(item.Defence * 1.1);

                    _logger.ZLogInformationWithPayload(new { ItemUniqueID = itemUID },
                   $"Enhance item success: attack: " + item.Attack + "/ Defence: " + item.Defence 
                    + "/ EnhanceCount: " + item.EnhanceCount);

                    enhanceResult = true;
                }
                else
                {
                    item.IsBreak = true;

                    _logger.ZLogInformationWithPayload(new { ItemUniqueID = itemUID },
                   $"Enhance Fail");

                    enhanceResult = false;
                }
                var result = await _queryFactory.Query("playerItem").Where("ItemUniqueID", itemUID)
                    .UpdateAsync(item);
                return new Tuple<ErrorCode, PlayerItem, bool>(ErrorCode.None, item, enhanceResult);
            }
            else
            {
                _logger.ZLogError(
                   $"ErrorMessage: Item Enhance Disable");
                return new Tuple<ErrorCode, PlayerItem, bool>(ErrorCode.ItemEnhanceDisable, null, false);
            }

        }
        catch {
            _logger.ZLogError(
                   $"ErrorMessage: Enhance Item Error");
            return new Tuple<ErrorCode, PlayerItem, bool>(ErrorCode.ItemEnhanceError, null, false);
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

