using System;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Services;
using DungeonFarming.MasterData;
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
    private readonly IGameDb _gameDb;

    private readonly ILogger<CreateAccount> _logger;

    public CreateAccount(ILogger<CreateAccount> logger, IAccountDb accountDb
        , IGameDb gameDb)
    {
        _logger = logger;
        _accountDb = accountDb;
        _gameDb = gameDb;
    }

    [HttpPost]
    public async Task<CreateAccountResponse> Post(CreateAccountRequest request)
    {
        var response = new CreateAccountResponse();

        var errorCode = await _accountDb.CreateAccountAsync(request.AccountID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        (errorCode, var uid) = await _gameDb.InsertNewPlayer(request.AccountID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            await _accountDb.DeleteAccountAsync(request.AccountID);
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.CreateAccount], new { accountID = request.AccountID }, $"CreateAccount Success");
        return response;
    }
}