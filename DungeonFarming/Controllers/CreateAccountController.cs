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
public class CreateAccount : ControllerBase
{
    private readonly IAccountDb _accountDb;
    private readonly ICharacterDb _characterDb;
    private readonly ILogger<CreateAccount> _logger;

    public CreateAccount(ILogger<CreateAccount> logger, IAccountDb accountDb)
    {
        _logger = logger;
        _accountDb = accountDb;
    }

    [HttpPost]
    public async Task<PkCreateAccountRes> Post(PkCreateAccountReq request)
    {
        var response = new PkCreateAccountRes();

        var errorCode = await _accountDb.CreateAccountAsync(request.ID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.CreateAccount], new { ID = request.ID }, $"CreateAccount Success");
        return response;
    }
}
