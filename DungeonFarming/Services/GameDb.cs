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
                , "Attack", "Defence", "Magic", "EnhanceCount", "ItemCount"};

    Int32 MoneyCode = 1;

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

    public async Task<(ErrorCode, string)> InsertPlayer(string AccountId)
    {
        var uid = Service.Security.CreateUID();
        try
        {
            var Result = await _queryFactory.Query("Playerinfo").InsertAsync(new
            {
                AccountID = AccountId,
                UID = uid,
                Level = 1,
                Exp = 100,
                Hp = 50,
                Mp = 60,
                Gold = 10000,
                LastLogin = DateTime.Now.ToString("yyyy-MM-dd"),
                ConsecutiveLoginDays = 1,
                LastClearStage = 0
            });

            PlayerItem basicItem = new PlayerItem
            {
                // ItemCode 2 = 작은 칼
                UID = uid,
                ItemCode = _MasterData.ItemDict[2].Code,
                ItemUniqueID = Service.Security.MakeItemUniqueID(_MasterData.ItemDict[2].Code),
                ItemName = _MasterData.ItemDict[2].Name,
                Attack = _MasterData.ItemDict[2].Attack,
                Defence = _MasterData.ItemDict[2].Defence,
                Magic = _MasterData.ItemDict[2].Magic,
                EnhanceCount = 0,
                ItemCount = 1
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
                $"[GameDb.InsertPlayer] ErrorCode : {ErrorCode.InsertPlayerItemFail}");
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
                item.Defence, item.Magic, item.EnhanceCount, item.ItemCount}).ToArray();

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
            var PlayerItems = await _queryFactory.Query("playerItem").Where("UID", uid).GetAsync<PlayerItem>();
            
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
        var PageSize = 20;
        try
        {
            var Mails = await _queryFactory.Query("Mail").Where("UID", uid).Where("IsRead", 0)
                .Where("ExpirationDate", ">=", DateTime.Now.ToString("yyyy-MM-dd"))
                .OrderBy("CreatedAt")
                .ForPage(page, PageSize).GetAsync<Mail>();

            return new Tuple<ErrorCode, List<Mail>>(ErrorCode.None, Mails.ToList<Mail>());
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Get Mail Error");
            return new Tuple<ErrorCode, List<Mail>>(ErrorCode.GetMailFail, null);
        }
    }

    public async Task<List<PlayerItem>> MakeItemListFromMail(PlayerInfo playerInfo, List<ItemCodeAndCount> ItemInMail)
    {
        try
        {
            List<PlayerItem> ItemList = new List<PlayerItem>();
            var uid = playerInfo.UID;
            foreach (var it in ItemInMail)
            {

                if (it.ItemCode == MoneyCode)
                {
                    var updatePlayerGold = _queryFactory.Query("PlayerInfo").Where("UID", uid)
                        .AsUpdate(new { Gold = playerInfo.Gold + it.ItemCount });
                    await _queryFactory.ExecuteAsync(updatePlayerGold);
                }
                if (_MasterData.ItemDict[it.ItemCode].CanOverlap == true)
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
                    ItemCount = it.ItemCount
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

            var isRead = await _queryFactory.Query("Mail").Where("MailCode", mailcode)
                .Select("IsRead").FirstOrDefaultAsync<int>();
            if (isRead == 1) return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.AlreadyGetMailItem, null);

            var result = await _queryFactory.Query("MailItem").Select("Items")
                .Where("UID", uid).Where("MailCode", mailcode)
                .FirstOrDefaultAsync<string>();

            Console.WriteLine(result);

            List<ItemCodeAndCount> ItemInMail = JsonSerializer.Deserialize<List<ItemCodeAndCount>>(result);
            List<PlayerItem> ItemList = await MakeItemListFromMail(playerInfo, ItemInMail);

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
            
            List<ItemCodeAndCount> items = new List<ItemCodeAndCount>();
            items.Add(new ItemCodeAndCount
            {
                ItemCode = rewords.ItemCode,
                ItemCount = rewords.Count
            });

            result = await _queryFactory.Query("MailItem").InsertAsync(new MailItem
            {
                UID = uid,
                MailCode = mailCode,
                Items = JsonSerializer.Serialize<List<ItemCodeAndCount>>(items)
            });

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
                Content = "구매 상품",
                ExpirationDate = DateTime.Now.AddYears(1).Date,
                IsRead = false,
                CreatedAt = DateTime.Now,
            });
            var itemList = _MasterData.InAppProductDict[productCode].Item;
            result = await _queryFactory.Query("MailItem").InsertAsync(new MailItem
            {
                UID = uid,
                MailCode = mailCode,
                Items = JsonSerializer.Serialize<List<ItemCodeAndCount>>(itemList)
            });

            return ErrorCode.None;
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Send Product Mail Error");
            return ErrorCode.None;
        }
    }

    // 강화
    public async Task<Tuple<ErrorCode, PlayerItem>> EnhanceItem(string uid, string itemUID)
    {
        try
        {
            var item = await _queryFactory.Query("playerItem").Where("UID", uid)
                .Where("ItemUniqueID", itemUID).FirstOrDefaultAsync<PlayerItem>();

            if (item.EnhanceCount < _MasterData.ItemDict[item.ItemCode].EnhanceMaxCount)
            {
                item.EnhanceCount++;
                Random random = new Random();
                if (random.Next(10) < 3)
                {
                    item.Attack = (int)Math.Ceiling(item.Attack * 1.1);
                    item.Defence = (int)Math.Ceiling(item.Defence * 1.1);
                    item.Magic = (int)Math.Ceiling(item.Magic * 1.1);

                    _logger.ZLogInformationWithPayload(new { UID = uid },
                   $"Enhance item success: attack: " + item.Attack + "/ Defence: " + item.Defence 
                   + "/ Magic: " + item.Magic + "/ EnhanceCount: " + item.EnhanceCount);
                }
                else
                {
                    _logger.ZLogInformationWithPayload(new { UID = uid },
                   $"Enhance Fail / EnhanceCount"+ item.EnhanceCount);
                }
                var result = await _queryFactory.Query("playerItem").Where("ItemUniqueID", itemUID)
                    .UpdateAsync(item);
                return new Tuple<ErrorCode, PlayerItem>(ErrorCode.None, item);
            }
            else
            {
                _logger.ZLogError(
                   $"ErrorMessage: Item Enhance Disable");
                return new Tuple<ErrorCode, PlayerItem>(ErrorCode.ItemEnhanceDisable, null);
            }

        }
        catch {
            _logger.ZLogError(
                   $"ErrorMessage: Enhance Item Error");
            return new Tuple<ErrorCode, PlayerItem>(ErrorCode.ItemEnhanceError, null);
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

