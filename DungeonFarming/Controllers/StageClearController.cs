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

        Int32 uid = request.UID;
        Int32 stageCode = request.StageCode;

        List<InStageNpc> currKilledNpcList= await _redisDb.GetKilledNPCList(uid, stageCode);

        var errorCode = _gameDb.CheckClearStage(request.StageCode, currKilledNpcList);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        List<InStageItem> itemList = await _redisDb.GetFarmingItemListAll(uid, stageCode);

        (errorCode, response.EarnItemList) = await _gameDb.EarnItemAfterStageClear(uid, itemList);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }
        errorCode = await _redisDb.DeleteFarmingItemList(uid, stageCode);

        (errorCode, response.EarnEXP) = await _gameDb.EarnExpAfterClearStage(uid, stageCode, currKilledNpcList);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        errorCode = await _redisDb.DeleteKilledNPCList(uid, stageCode);

        response.Result = errorCode;
        _logger.ZLogInformationWithPayload(new { UID = request.UID }, $"Player Stage {request.StageCode} Clear Success");
        return response;
    }
}
