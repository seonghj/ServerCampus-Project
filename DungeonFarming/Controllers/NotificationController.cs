using System;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZLogger;
using static LogManager;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class Notification : ControllerBase
{
    readonly IAccountDb _accountDb;
    private readonly IRedisDb _redisDb;
    readonly ILogger<Login> _logger;

    public Notification(ILogger<Login> logger, IAccountDb accountDb, IRedisDb redisDb)
    {
        _logger = logger;
        _accountDb = accountDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<NotificationResponse> Post(NotificationRequest request)
    {
        var response = new NotificationResponse();

        (var errorCode, var CheckAuthResult) = await _redisDb.CheckPlayerAuthAsync(request.AccountID, request.AuthToken);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }
        else if (CheckAuthResult == false)
        {
            response.Result = ErrorCode.AuthTokenNotFound;
            return response;
        }
        (errorCode, response.NotificationList) = await _redisDb.GetNotificationAsync("notice");
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }


        _logger.ZLogInformationWithPayload(new { ID = request.AccountID }, "Send Notification Success");
        return response;
    }

}
