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
    readonly ICharacterDb _characterDb;
    readonly ILogger<Login> _logger;

    public Login(ILogger<Login> logger, IAccountDb accountDb, ICharacterDb characterDb)
    {
        _logger = logger;
        _accountDb = accountDb;
        _characterDb = characterDb;
    }

    [HttpPost]
    public async Task<PkLoginResponse> Post(PkLoginRequest request)
    {
        var response = new PkLoginResponse();
        // ID, PW 검증
        var (errorCode, accountId) = await _accountDb.VerifyAccount(request.ID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { ID = request.ID}, "Login Success");

        (errorCode, response.CharInfo) = await _characterDb.GetCharacterInfo(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        return response;
    }

}
