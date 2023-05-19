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
using System.Security.Cryptography;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class AttendenceStatus : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;

    public AttendenceStatus(ILogger<Login> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    [HttpPost]
    public async Task<AttendenceStatusResponse> Post(AttendenceStatusRequest request)
    {
        var response = new AttendenceStatusResponse();

        (var errorCode, response.AttendenceDays) = await _gameDb.GetPlayerAttendenceDays(request.UID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(new { UID = request.UID }, "Check Attendence Status Success");
        return response;
    }
}
