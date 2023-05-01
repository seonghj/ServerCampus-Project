using System;
using System.Threading.Tasks;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Services;
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
    readonly IOptions<Versions> _version;

    public Login(ILogger<Login> logger, IAccountDb accountDb
        , IGameDb gameDb, IRedisDb redisDb, IOptions<Versions> version)
    {
        _logger = logger;
        _accountDb = accountDb;
        _gameDb = gameDb;
        _redisDb = redisDb;
        _version = version;
    }

    [HttpPost]
    public async Task<LoginResponse> Post(LoginRequest request)
    {
        var response = new LoginResponse();

        if (request.ClientVersion != _version.Value.Client) 
        {
            response.Result = ErrorCode.ClinetVersionNotMatch;
            return response;
        }

        var errorCode = await _accountDb.VerifyAccount(request.ID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        errorCode = await _redisDb.InsertPlayerAuthAsync(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }
        (errorCode, response.PlayerAuth) = await _redisDb.GetPlayerAuthAsync(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        (errorCode, response.PlayerInfomation) = await _gameDb.GetPlayerInfo(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { ID = request.ID }, "Login Success");
        return response;
    }

}
