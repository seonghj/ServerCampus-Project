using System;
using System.Threading.Tasks;
using DungeonFarming.ModelReqRes;
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
    readonly ILogger<Login> _logger;

    public Login(ILogger<Login> logger, IAccountDb accountDb, IGameDb gameDb)
    {
        _logger = logger;
        _accountDb = accountDb;
        _gameDb = gameDb;
    }

    [HttpPost]
    public async Task<PkLoginResponse> Post(PkLoginRequest request)
    {
        var response = new PkLoginResponse();
        // ID, PW 검증
        var errorCode = await _accountDb.VerifyAccount(request.ID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { ID = request.ID}, "Login Success");

        (errorCode, response.P_Info) = await _gameDb.GetPlayerInfo(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        return response;
    }

}
