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
public class Attendence : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;

    public Attendence(ILogger<Login> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    //[HttpPost]
    //public async Task<AttendenceRequest> Post(AttendenceRequest request)
    //{
    //    var response = new AttendenceResponse();

    //    (var errorCode, response.Items) =

    //    _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { UID = request.UID }, "Attendence Check Success");
    //    return response;
    //}
}
