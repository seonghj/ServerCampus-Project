using System;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Services;
using DungeonFarming.MasterData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DungeonFarming.Security;
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

    private readonly IMasterData _MasterData;

    public CreateAccount(ILogger<CreateAccount> logger, IAccountDb accountDb
        , IGameDb gameDb, IMasterData masterData)
    {
        _logger = logger;
        _accountDb = accountDb;
        _gameDb = gameDb;
        _MasterData = masterData;
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

        (errorCode, var uid) = await _gameDb.InsertPlayer(request.AccountID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            await _accountDb.DeleteAccountAsync(request.AccountID);
            return response;
        }

        PlayerItem basicItem = new PlayerItem
        {
            // ItemCode 2 = 나무 칼
            UID = uid,
            ItemCode = _MasterData.Items[2].Code,
            ItemUniqueID = Security.Security.ItemUniqueID(),

            Attack = _MasterData.Items[2].Attack,
            Defence = _MasterData.Items[2].Defence,
            Magic = _MasterData.Items[2].Magic,
            EnhanceCount = 0,
            Count = 1
        };

        errorCode = await _gameDb.InsertPlayerItem(uid, basicItem);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.CreateAccount], new { accountID = request.AccountID }, $"CreateAccount Success");
        return response;
    }
}