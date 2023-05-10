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

    string[] playerItemCols = new[] { "UID", "ItemCode", "ItemName"
                , "Attack", "Defence", "Magic", "EnhanceCount", "ItemCount"};
    string[] MailItemCols = new[] { "PlayerUID", "MailCode", "ItemCode", "ItemCount"};

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

    public async Task<(ErrorCode, Int32)> InsertNewPlayer(string AccountId)
    {
        try
        {
            var Result = await _queryFactory.Query("Playerinfo").InsertAsync(new
            {
                AccountID = AccountId,
                Level = basicLevel,
                Exp = basicExp,
                Hp = basicHp,
                Mp = basicMp,
                Gold = basicGold,
                LastLoginTime = DateTime.Now.Date,
                ConsecutiveLoginDays = 0,
                LastClearStage = 0
            });

            var uid = await _queryFactory.Query("Playerinfo").Where("AccountID", AccountId)
                .Select("UID").FirstOrDefaultAsync<int>();

            PlayerItem basicItem = new PlayerItem
            {
                UID = uid,
                ItemCode = _MasterData.ItemDict[basicWeaponCode].Code,
                ItemName = _MasterData.ItemDict[basicWeaponCode].Name,
                Attack = _MasterData.ItemDict[basicWeaponCode].Attack,
                Defence = _MasterData.ItemDict[basicWeaponCode].Defence,
                Magic = _MasterData.ItemDict[basicWeaponCode].Magic,
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
            return (ErrorCode.CreatePlayerFailException, -1);
        }

    }

    public async Task<ErrorCode> InsertPlayerItem(Int32 UID, PlayerItem item)
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

    public async Task<(ErrorCode, List<PlayerItemForClient>)> InsertItemListToPlayer(Int32 uid, List<PlayerItem> itemList)
    {
        try
        {
            if (itemList.Count() == 0) { return (ErrorCode.InsertPlayerItemFail, null); }

            List<PlayerItem> insertList = new List<PlayerItem>();
            List<PlayerItemForClient> returnList = new List<PlayerItemForClient>();
            foreach (var item in itemList)
            {

                if (_MasterData.ItemDict[item.ItemCode].CanOverlap == true)
                {
                    if (_MasterData.ItemDict[item.ItemCode].Attribute == GoldAttributeCode)
                    {
                        var updatePlayerGold = await _queryFactory.Query("PlayerInfo")
                            .IncrementAsync("Gold", item.ItemCount);
                    }

                    bool isExist = await _queryFactory.Query("PlayerItem").Where("itemCode", item.ItemCode)
                        .ExistsAsync();

                    if (isExist == true)
                    {
                        var updateItemCount = await _queryFactory.Query("PlayerItem").Where("UID", uid).Where("ItemCode", item.ItemCode)
                           .IncrementAsync("ItemCount", item.ItemCount);
                        continue;
                    }
                }
                insertList.Add(item);
                returnList.Add(new PlayerItemForClient
                {
                    ItemCode = item.ItemCode,
                    Attack = item.Attack,
                    Defence = item.Defence,
                    EnhanceCount = item.EnhanceCount,
                    Magic = item.Magic,
                    ItemCount = item.ItemCount,
                }) ;
            }

            var insertData = insertList.Select(item => new object[]
                { item.UID, item.ItemCode, item.ItemName, item.Attack,
                item.Defence, item.Magic, item.EnhanceCount, item.ItemCount}).ToArray();

            var insertItemToPlayer = _queryFactory.Query("PlayerItem").AsInsert(playerItemCols, insertData);
            await _queryFactory.ExecuteAsync(insertItemToPlayer);



            return (ErrorCode.None, returnList);
        }
        catch(Exception ex) 
        {
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

            return (ErrorCode.None, PlayerInfomation);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.GetPlayer] ErrorCode : {ErrorCode.GetPlayerInfoFail}");
            return (ErrorCode.None, null);
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

    public List<PlayerItem> MakeItemListFromMail(Int32 uid, List<MailItem> ItemInMail)
    {
        try
        {
            List<PlayerItem> ItemList = new List<PlayerItem>();
            foreach (var it in ItemInMail)
            {
                Item itemMasterdata = _MasterData.getItemData(it.ItemCode);
                ItemList.Add(new PlayerItem
                {
                    UID = uid,
                    ItemCode = it.ItemCode,
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

    public async Task<(ErrorCode, List<PlayerItemForClient>)> GetItemFromMailAsync(Int32 uid, Int32 mailcode)
    {
        try
        {
            var MailInfo = await _queryFactory.Query("Mail").Where("MailCode", mailcode).Where("IsReceive",false)
                .SelectRaw("ExpirationDate").FirstOrDefaultAsync();

            if (MailInfo.ExpirationDate < DateTime.Now.Date)
            {
                return (ErrorCode.MailExpirationDateOut, null);
            }

            var mailItems = await _queryFactory.Query("MailItem")
                .Where("PlayerUID", uid).Where("MailCode", mailcode)
                .GetAsync<MailItem>();

            List<PlayerItem> ItemList = MakeItemListFromMail(uid, mailItems.ToList<MailItem>());
            List<PlayerItemForClient> ItemListForClient = new List<PlayerItemForClient>();
            (var errorCode, ItemListForClient) = await InsertItemListToPlayer(uid, ItemList);
            var updateIsRead = _queryFactory.Query("Mail").Where("UID", uid).Where("MailCode", mailcode)
                    .AsUpdate(new { IsReceive = true });
            await _queryFactory.ExecuteAsync(updateIsRead);
            return (ErrorCode.None, ItemListForClient);
        }
        catch(Exception ex)
        {
            _logger.ZLogError(ex,
                   $"ErrorMessage: Get Item In Mail Error");
            return (ErrorCode.GetMailItemFail, null);
        }
    }


    // 출석부
    public async Task<ErrorCode> SendAttendenceRewordsMail(Int32 uid)
    {
        try
        {
            (ErrorCode errorCode, PlayerInfo playerInfo) = await GetPlayerInfo(uid);
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



            var result = await _queryFactory.Query("PlayerInfo").Where("UID", uid)
                    .UpdateAsync(new
                    {
                        ConsecutiveLoginDays = NextAttendenceDay,
                        LastLoginTime = DateTime.Now.Date
                    });
            playerInfo.LastLoginTime = DateTime.Now.Date;
            playerInfo.ConsecutiveLoginDays = NextAttendenceDay;

            Attendance rewords = _MasterData.AttendanceDict[playerInfo.ConsecutiveLoginDays];

            DateTime CreateMailTime = DateTime.Now;

            result = await _queryFactory.Query("Mail").InsertAsync(new Mail
            {
                UID = uid,
                Title = "출석 보상",
                ExpirationDate = DateTime.Now.AddDays(30).Date,
                IsReceive = false,
                CreatedAt = CreateMailTime,
            });

            var getMailCode = _queryFactory.Query("mail").Where("UID", uid)
                .Where("CreatedAt", CreateMailTime).Select("LAST_INSERT_ID() as MailCode");
            var mailCode = await getMailCode.GetAsync<int>();

            result = await _queryFactory.Query("MailItem").InsertAsync(new MailItem
            {
                PlayerUID = uid,
                MailCode = mailCode.FirstOrDefault(),
                ItemCode = rewords.ItemCode,
                ItemCount = rewords.Count
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
    public async Task<ErrorCode> InAppProductSentToMail(Int32 uid, Int32 productCode, string receiptCode)
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


            DateTime CreateMailTime = DateTime.Now;
            var mailCode = await _queryFactory.Query("Mail").InsertGetIdAsync<int>(new Mail
            {
                UID = uid,
                Title = "구매 상품",
                ExpirationDate = DateTime.Now.AddYears(1).Date,
                IsReceive = false,
                CreatedAt = CreateMailTime,
            });
            var itemList = _MasterData.InAppProductDict[productCode].Item;


            var insertList = itemList.Select(item => new object[]
                {uid, mailCode, item.ItemCode, item.ItemCount}).ToArray();
            var insertQuery = _queryFactory.Query("MailItem").AsInsert(MailItemCols, insertList);
            await _queryFactory.ExecuteAsync(insertQuery);

            return ErrorCode.None;
        }
        catch(Exception ex)
        {
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
                   $"Enhance item success: attack: {item.Attack} / Defence: {item.Defence} / EnhanceCount: {item.EnhanceCount}");

                    enhanceResult = true;
                }
                else
                {
                    _logger.ZLogInformationWithPayload(new { ItemUniqueID = itemUID },
                   $"Enhance Fail");

                    enhanceResult = false;
                }
                var result = await _queryFactory.Query("playerItem").Where("ItemUniqueID", itemUID)
                    .UpdateAsync(item);
                return (ErrorCode.None, item, enhanceResult);
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

