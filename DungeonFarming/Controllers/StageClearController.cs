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
public class StageClear : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;
    readonly IRedisDb _redisDb;

    public StageClear(ILogger<Login> logger, IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<StageClearResponse> Post(StageClearRequest request)
    {
        var response = new StageClearResponse();

        List<int> currKilledNpcs = new List<int>(await _redisDb.GetKilledNPCList(request.UID, request.StageCode));

        var errorCode = _gameDb.CheckClearStage(request.StageCode, currKilledNpcs);

        if (errorCode != ErrorCode.None)
        {
            response.Result = ErrorCode.PlayerClearStageDisable;
            return response;
        }



        response.Result = errorCode;
        _logger.ZLogInformationWithPayload(new { UID = request.UID }, $"Player Stage {request.StageCode} Clear Success");
        return response;
    }
}
