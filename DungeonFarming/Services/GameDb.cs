using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
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

    public async Task<ErrorCode> InsertPlayer(string AccountId)
    {
        try
        {
            var PlayerId = await _queryFactory.Query("Playerinfo").InsertGetIdAsync<int>(new
            {
                AccountID = AccountId,
                Level = 1,
                Exp = 100,
                Hp = 50,
                Mp = 60,
                Gold = 10000,
                LastStage = 0
            });

            return ErrorCode.None;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDb.InsertPlayer] ErrorCode : {ErrorCode.CreatePlayerFailException}");
            return ErrorCode.CreatePlayerFailException;
        }

    }

    public Task<ErrorCode> InsertPlayerItem(string UID)
    {
        throw new NotImplementedException();
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
            var P_Info = await _queryFactory.Query("playerinfo").Where("AccountID", AccountId).FirstOrDefaultAsync<PlayerInfo>();

            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.None, P_Info);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDB.InsertPlayer] ErrorCode : {ErrorCode.CreatePlayerFailException}");
            return new Tuple<ErrorCode, PlayerInfo>(ErrorCode.None, null);
        }
    }
}