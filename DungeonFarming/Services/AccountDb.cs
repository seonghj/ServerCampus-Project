using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DungeonFarming.ModelDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;

namespace DungeonFarming.Services;

public class AccountDb : IAccountDb
{
    readonly IOptions<DbConfig> _dbConfig;
    readonly ILogger<AccountDb> _logger;

    IDbConnection _accountdbConn, _chardbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory1, _queryFactory2;


    public AccountDb(ILogger<AccountDb> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        AccountDBOpen();
        CharacterDBOpen();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory1 = new SqlKata.Execution.QueryFactory(_accountdbConn, _compiler);
        _queryFactory2 = new SqlKata.Execution.QueryFactory(_chardbConn, _compiler);
    }

    public void Dispose()
    {
        AccountDBClose();
    }

    public async Task<ErrorCode> CreateAccountAsync(String id, String pw)
    {
        try
        {
            _logger.ZLogDebug(
                $"[CreateAccount] ID: {id}, Password: {pw}");

            var count = await _queryFactory1.Query("account").InsertAsync(new {
                                                                    ID = id,
                                                                    Password = pw                
            });            
            
            if(count != 1)
            {
                return ErrorCode.CreateAccountFailInsert;
            }
            count = await _queryFactory2.Query("charinfo").InsertAsync(new
            {
                ID = id,
                Level = 1,
                Exp = 100,
                Hp = 50,
                Mp = 60,
                Gold = 10000,
                LastStage = 0
            });
            if (count != 1)
            {
                return ErrorCode.CreateCharacterFailException;
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[AccountDb.CreateAccount] ErrorCode: {ErrorCode.CreateAccountFailException}, ID: {id}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<Tuple<ErrorCode, Int64>> VerifyAccount(String id, String pw)
    {
        try
        {
            // 존재하는 계정인지 체크
            var accountInfo = await _queryFactory1.Query("account").Where("ID", id).FirstOrDefaultAsync<Account>();

            if (accountInfo is null)
            {
                return new Tuple<ErrorCode, Int64>(ErrorCode.LoginFailUserNotExist, 0);
            }
            if (accountInfo.Password != pw)
            {
                _logger.ZLogError(
                    $"[AccountDb.VerifyAccount] ErrorCode: {ErrorCode.LoginFailPwNotMatch}, ID: {id}");
                return new Tuple<ErrorCode, Int64>(ErrorCode.LoginFailPwNotMatch, 0);
            }

            return new Tuple<ErrorCode, Int64>(ErrorCode.None, accountInfo.AccountId);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[AccountDb.VerifyAccount] ErrorCode: {ErrorCode.LoginFailException}, ID:  {id}");
            return new Tuple<ErrorCode, Int64>(ErrorCode.LoginFailException, 0);
        }
    }


    private void AccountDBOpen()
    {
        _accountdbConn = new MySqlConnection(_dbConfig.Value.AccountDb);

        _accountdbConn.Open();
    }

    private void AccountDBClose()
    {
        _accountdbConn.Close();
    }

    private void CharacterDBOpen()
    {
        _chardbConn = new MySqlConnection(_dbConfig.Value.CharacterDb);

        _chardbConn.Open();
    }

    private void CharacterDBClose()
    {
        _chardbConn.Close();
    }
}

public class DbConfig
{
    public String MasterDb { get; set; }
    public String AccountDb { get; set; }
    public String CharacterDb { get; set; }
    public String Memcached { get; set; }
}