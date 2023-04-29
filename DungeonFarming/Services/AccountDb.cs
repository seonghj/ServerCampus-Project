using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
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


    public async Task<ErrorCode> CreateAccountAsync(String AccountId, String pw)
    {
        try
        {
            _logger.ZLogDebug(
                $"[CreateAccount] ID: {AccountId}, Password: {pw}");

            var count = await _queryFactory.Query("account").InsertAsync(new
            {
                ID = AccountId,
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
                $"[AccountDb.CreateAccount] ErrorCode: {ErrorCode.CreateAccountFailException}, ID: {AccountId}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> VerifyAccount(String AccountId, String pw)
    {
        try
        {
            // 존재하는 계정인지 체크
            var accountInfo = await _queryFactory.Query("account").Where("ID", AccountId).FirstOrDefaultAsync<Account>();

            if (accountInfo is null)
            {
                return ErrorCode.LoginFailUserNotExist;
            }
            if (accountInfo.Password != pw)
            {
                _logger.ZLogError(
                    $"[AccountDb.VerifyAccount] ErrorCode: {ErrorCode.LoginFailPwNotMatch}, ID: {AccountId}");
                return ErrorCode.LoginFailPwNotMatch;
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[AccountDb.VerifyAccount] ErrorCode: {ErrorCode.LoginFailException}, ID:  {AccountId}");
            return ErrorCode.LoginFailException;
        }
    }

    public async Task<ErrorCode> DeleteAccountAsync(String AccountId)
    {
        try
        {
            _logger.ZLogDebug(
                $"[DeleteAccount] ID: {AccountId}");

            var count = await _queryFactory.Query("account").Where(
                "ID",
                "=",
                AccountId
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
                $"[AccountDb.DeleteAccount] ErrorCode: {ErrorCode.CreateAccountFailException}, ID: {AccountId}");
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

    public void Dispose()
    {
        AccountDBClose();
    }
}