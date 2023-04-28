using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.ModelDB;
using DungeonFarming.ModelReqRes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;

namespace DungeonFarming.Services;

public class CharacterDb : ICharacterDb
{
    readonly ILogger<CharacterDb> _logger;
    readonly IOptions<DbConfig> _dbConfig;

    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    public CharacterDb(ILogger<CharacterDb> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        _dbConn = new MySqlConnection(_dbConfig.Value.CharacterDb);

        _dbConn.Open();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new SqlKata.Execution.QueryFactory(_dbConn, _compiler);
    }

    public async Task<ErrorCode> InsertCharacter(string Id)
    {
        try
        {
            var characterId = await _queryFactory.Query("charinfo").InsertGetIdAsync<int>(new
            {
                ID = Id,
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
                $"[GameDb.InsertCharacter] ErrorCode : {ErrorCode.CreateCharacterFailException}");
            return ErrorCode.CreateCharacterFailException;
        }

    }

    public Task<ErrorCode> InsertCharacterItem(string Id)
    {
        throw new NotImplementedException();
    }

    private void Open()
    {
        _dbConn = new MySqlConnection(_dbConfig.Value.CharacterDb);

        _dbConn.Open();
    }

    public async Task<Tuple<ErrorCode, string>> GetCharacterInfo(string Id)
    {
        try
        {
            var cinfo = await _queryFactory.Query("charinfo").Where("ID", Id).FirstOrDefaultAsync<CharInfo>();

            string CInfojson = JsonSerializer.Serialize<CharInfo>(cinfo);

            return new Tuple<ErrorCode, string>(ErrorCode.None, CInfojson);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex,
                $"[GameDb.InsertCharacter] ErrorCode : {ErrorCode.CreateCharacterFailException}");
            return new Tuple<ErrorCode, string>(ErrorCode.None, "");
        }
    }
}