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
using DungeonFarming.MasterData;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class StageStopController : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;
    readonly IRedisDb _redisDb;

    public StageStopController(ILogger<Login> logger, IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<StageStopResponse> Post(StageStopRequest request)
    {
        var response = new StageStopResponse();

        Int32 uid = request.UID;
        Int32 stageCode = request.StageCode;

        (var errorCode, var isInStage) = await _redisDb.CheckPlayerState(request.AccountID, PlayerState.InStage);
        if (errorCode != ErrorCode.None || isInStage == false)
        {
            response.Result = errorCode;
            return response;
        }

        errorCode = await _redisDb.ChangePlayerState(request.AccountID, PlayerState.Default);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        errorCode = await _redisDb.InitInStageData(uid, stageCode);

        response.Result = errorCode;
        _logger.ZLogInformationWithPayload(new { UID = request.UID }, $"Player Stage {request.StageCode} Clear Success");
        return response;
    }
}
