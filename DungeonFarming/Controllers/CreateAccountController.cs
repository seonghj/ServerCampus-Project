using System;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
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

        var errorCode = await _accountDb.CreateAccountAsync(request.ID, request.Password);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        (errorCode, var uid) = await _gameDb.InsertPlayer(request.ID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            await _accountDb.DeleteAccountAsync(request.ID);
            return response;
        }

        PlayerItem basicItem = new PlayerItem
        {
            UID = uid,
            ItemCode = "1",
            ItemUniqueID = "tmp",

            Attack = 10,
            Defence = 5,
            Magic = 1,
            EnhanceCount = 0,
            Count = 1
        };

        errorCode = await _gameDb.InsertPlayerItem(uid, basicItem);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.CreateAccount], new { ID = request.ID }, $"CreateAccount Success");
        return response;
    }
}