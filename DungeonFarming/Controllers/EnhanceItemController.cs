using System;
using System.Threading.Tasks;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.DBTableFormat;
using DungeonFarming.MasterData;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZLogger;
using static LogManager;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class EnhanceItem : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;

    public EnhanceItem(ILogger<Login> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    [HttpPost]
    public async Task<EnhanceItemResponse> Post(EnhanceItemRequest request)
    {
        var response = new EnhanceItemResponse();

        (var errorCode, response.ItemInfo, response.EnhanceResult) = await _gameDb.EnhanceItem(request.UID, request.ItemUniqueID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(new { UID = request.UID }, "Try Enhance Item Success");
        return response;
    }

}
