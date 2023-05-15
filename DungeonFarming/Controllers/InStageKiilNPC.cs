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
public class KillNPC : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;
    readonly IRedisDb _redisDb;

    public KillNPC(ILogger<Login> logger, IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<KillNPCResponse> Post(KillNPCRequest request)
    {
        var response = new KillNPCResponse();

        List<int> currKilledNpcs = await _redisDb.GetKilledNPCList(request.UID, request.StageCode);

        response.Result = _gameDb.CheckCanKillNPC(request.NPCCode, request.StageCode, currKilledNpcs);

        if (response.Result != ErrorCode.None)
        {
            response.Result = ErrorCode.NotExistItemInStage;
            return response;
        }

        var errorCode = await _redisDb.PlayerKillNPC(request.UID, request.NPCCode, request.StageCode);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }
        response.Result = errorCode;
        _logger.ZLogInformationWithPayload(new { UID = request.UID }, "Save Killed NPC Data In Redis Success");
        return response;
    }
}
