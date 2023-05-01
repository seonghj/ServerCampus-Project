using System;
using System.Threading.Tasks;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

    public Login(ILogger<Login> logger, IAccountDb accountDb
        , IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _accountDb = accountDb;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<LoginResponse> Post(LoginRequest request)
    {
        var response = new LoginResponse();

        var errorCode = await _accountDb.VerifyAccount(request.ID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        errorCode = await _redisDb.CreatePlayerAuthAsync(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }
        (errorCode, response.P_Auth) = await _redisDb.GetPlayerAuthAsync(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        (errorCode, response.P_Info) = await _gameDb.GetPlayerInfo(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { ID = request.ID }, "Login Success");
        return response;
    }

}
