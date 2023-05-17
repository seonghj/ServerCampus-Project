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
public class FarmingItem : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;
    readonly IRedisDb _redisDb;

    public FarmingItem(ILogger<Login> logger, IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<FarmingItemResponse> Post(FarmingItemRequest request)
    {
        var response = new FarmingItemResponse();

        InStageItem currFarmingItem = await _redisDb.GetFarmingItem(request.UID, request.ItemCode, request.StageCode);

        response.Result = _gameDb.CheckCanFarmingItem(request.ItemCode, request.ItemCount, request.StageCode, currFarmingItem);

        if (response.Result != ErrorCode.None)
        {
            response.Result = ErrorCode.NotExistItemInStage;
            return response;
        }

        Int32 maxCount = _gameDb.GetItemMaxCount(request.ItemCode, request.StageCode);

        var errorCode = await _redisDb.PlayerFarmingItem(request.UID, request.ItemCode
            , request.ItemCount, request.StageCode, maxCount);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }
        response.Result = errorCode;
        _logger.ZLogInformationWithPayload(new { UID = request.UID}, "Save Farming Item Data In Redis Success");
        return response;
    }
}
