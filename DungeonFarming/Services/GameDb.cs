using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using CloudStructures.Structures;
using DungeonFarming.DBTableFormat;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;

namespace DungeonFarming.Services;

public class GameDb : IGameDb
{
    readonly ILogger<GameDb> _logger;
    readonly IOptions<DbConfig> _dbConfig;

    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    public GameDb(ILogger<GameDb> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        GameDBOpen();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new SqlKata.Execution.QueryFactory(_dbConn, _compiler);
    }

    public async Task<(ErrorCode, string)> InsertPlayer(string AccountId)
    {
        var uid = Security.Security.CreateUID();
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
        var UniqueitemID = DateTime.Now.ToString("MMssddmmhhyyyy");
        item.ItemUniqueID = UniqueitemID;
        try
        {
            var Result = await _queryFactory.Query("Playerinfo").InsertAsync(item);

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
        try
        {
            var Mails = await _queryFactory.Query("Mail").Where("UID", uid).WhereFalse("Read")
                .ForPage(page, 20).GetAsync<Mail>();

            return new Tuple<ErrorCode, List<Mail>>(ErrorCode.None, Mails.ToList<Mail>());
        }
        catch
        {
            _logger.ZLogError(
                   $"ErrorMessage: Get Mail Error");
            return new Tuple<ErrorCode, List<Mail>>(ErrorCode.GetMailFail, null);
        }
    }
}