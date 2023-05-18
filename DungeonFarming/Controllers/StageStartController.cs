using System;
using System.Threading.Tasks;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.DBTableFormat;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZLogger;
using static LogManager;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class StageStart : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;
    readonly IRedisDb _redisDb;

    public StageStart(ILogger<Login> logger, IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<StageStartResponse> Post(StageStartRequest request)
    {
        Int32 uid = request.UID;
        Int32 stageCode = request.StageCode;

        var response = new StageStartResponse();

        (var errorCode, response.CanStart) = await _redisDb.CheckPlayerState(request.AccountID, PlayerState.Default);
        if (errorCode != ErrorCode.None || response.CanStart == false)
        {
            response.Result = errorCode;
            return response;
        }

        (errorCode, response.CanStart) =  await _gameDb.CheckAbleStartStage(uid, stageCode);
        if(errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        response.ItemList = _gameDb.GetStageItemInfo(uid, stageCode);
        if (response.ItemList == null)
        {
            response.Result = ErrorCode.GetStageDataFail;
            return response;
        }

        response.NPCList = _gameDb.GetStageNPCInfo(uid, stageCode);
        if (response.ItemList == null)
        {
            response.Result = ErrorCode.GetStageDataFail;
            return response;
        }

        errorCode = await _redisDb.ChangePlayerState(request.AccountID, PlayerState.InStage);

        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            response.CanStart = false;
            response.NPCList.Clear();
            response.ItemList.Clear();
            return response;
        }

        _logger.ZLogInformationWithPayload(new { UID = request.UID , Result = response.CanStart}, "Check Start Stage Success");
        return response;
    }
}
