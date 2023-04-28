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

    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;


    public AccountDb(ILogger<AccountDb> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        AccountDBOpen();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new SqlKata.Execution.QueryFactory(_dbConn, _compiler);
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

            var count = await _queryFactory.Query("account").InsertAsync(new
            {
                ID = id,
                Password = pw
            });

            if (count != 1)
            {
                return ErrorCode.CreateAccountFailInsert;
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
            var accountInfo = await _queryFactory.Query("account").Where("ID", id).FirstOrDefaultAsync<Account>();

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

    public async Task<ErrorCode> DeleteAccountAsync(String id)
    {
        try
        {
            _logger.ZLogDebug(
                $"[DeleteAccount] ID: {id}");

            var count = await _queryFactory.Query("account").Where(
                "ID",
                "=",
                id
            ).DeleteAsync();

            if (count != 1)
            {
                return ErrorCode.CreateAccountFailInsert;
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[AccountDb.DeleteAccount] ErrorCode: {ErrorCode.CreateAccountFailException}, ID: {id}");
            return ErrorCode.CreateAccountFailException;
        }
    }


    private void AccountDBOpen()
    {
        _dbConn = new MySqlConnection(_dbConfig.Value.AccountDb);

        _dbConn.Open();
    }

    private void AccountDBClose()
    {
        _dbConn.Close();
    }
}