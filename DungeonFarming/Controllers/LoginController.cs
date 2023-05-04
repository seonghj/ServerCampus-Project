using System;
using System.Threading.Tasks;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Services;
using DungeonFarming.MasterData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZLogger;
using static LogManager;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class Login : ControllerBase
{
    readonly IAccountDb _accountDb;
    readonly IGameDb _gameDb;
    private readonly IRedisDb _redisDb;
    readonly ILogger<Login> _logger;
    readonly IMasterData _MasterData;

    public Login(ILogger<Login> logger, IAccountDb accountDb
        , IGameDb gameDb, IRedisDb redisDb
        , IMasterData masterdata)
    {
        _logger = logger;
        _accountDb = accountDb;
        _gameDb = gameDb;
        _redisDb = redisDb;
        _MasterData = masterdata;
    }

    [HttpPost]
    public async Task<LoginResponse> Post(LoginRequest request)
    {
        var response = new LoginResponse();

        var errorCode = await _accountDb.VerifyAccount(request.AccountID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        errorCode = await _redisDb.InsertPlayerAuthAsync(request.AccountID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }
        (errorCode, response.PlayerAuth) = await _redisDb.GetPlayerAuthAsync(request.AccountID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        (errorCode, response.PlayerInfomation) = await _gameDb.GetPlayerInfo(request.AccountID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        (errorCode, response.PlayerItems) = await _gameDb.GetPlayerItem(response.PlayerInfomation.UID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { accountID = request.AccountID }, "Login Success");
        return response;
    }

}
