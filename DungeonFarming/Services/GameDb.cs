using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using CloudStructures.Structures;
using DungeonFarming.DBTableFormat;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.MasterData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;
using System.Collections;
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
                LastClearStage = 0
            });

            PlayerItem basicItem = new PlayerItem
            {
                // ItemCode 2 = 작은 칼
                UID = uid,
                ItemCode = _MasterData.ItemDict[2].Code,
                ItemUniqueID = Service.Security.ItemUniqueID(_MasterData.ItemDict[2].Code),
                ItemName = _MasterData.ItemDict[2].Name,
                Attack = _MasterData.ItemDict[2].Attack,
                Defence = _MasterData.ItemDict[2].Defence,
                Magic = _MasterData.ItemDict[2].Magic,
                EnhanceCount = 0,
                Count = 1
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
                $"[GameDB.InsertPlayer] ErrorCode : {ErrorCode.CreatePlayerFailException}");
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

    public async Task<Tuple<ErrorCode, List<PlayerItem>>> GetMailItemAsync(string uid, string mailcode)
    {
        try
        {
            var result = await _queryFactory.Query("MailItem").Select("Items")
                .Where("UID", uid).Where("MailCode", mailcode)
                .FirstOrDefaultAsync<string>();

            List <ItemCodeAndCount> ItemInMail = JsonSerializer.Deserialize<List<ItemCodeAndCount>>(result);
            List<PlayerItem> ItemList = new List<PlayerItem>();

            foreach (var it in ItemInMail)
            {
                Item itemMasterdata = _MasterData.getItemData(it.ItemCode);
                ItemList.Add(new PlayerItem
                {
                    UID = uid,
                    ItemUniqueID = Service.Security.ItemUniqueID(it.ItemCode),
                    ItemCode = it.ItemCode,
                    ItemName = itemMasterdata.Name,
                    Attack = itemMasterdata.Attack,
                    Defence = itemMasterdata.Defence,
                    Magic = itemMasterdata.Magic,
                    EnhanceCount = 0,
                    Count = it.ItemCount
                });
            }
            //IEnumerable<dynamic> insertData = ItemList.Select(item => new
            //{
            //    UID = item.UID,
            //    ItemCode = item.ItemCode,
            //    ItemUniqueID = item.ItemUniqueID,
            //    ItemName = item.ItemName,
            //    Attack = item.Attack,
            //    Defence = item.Defence,   
            //    Magic = item.Magic,
            //    EnhanceCount = item.EnhanceCount,
            //    Count = item.Count
            //});
            
            //var insertQuery = _queryFactory.Query("PlayerItem").AsInsert(insertData);
            //var affectedRows = await _queryFactory.ExecuteAsync(insertQuery);

            return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.None, ItemList);
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Get Item In Mail Error");
            return new Tuple<ErrorCode, List<PlayerItem>>(ErrorCode.GetMailItemFail, null);
        }
    }


}