using System;
using System.Threading.Tasks;
using DungeonFarming.RequestFormat;
using DungeonFarming.ResponseFormat;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZLogger;
using static LogManager;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class Notification : ControllerBase
{
    private readonly IRedisDb _redisDb;
    readonly ILogger<Notification> _logger;

    public Notification(ILogger<Notification> logger, IAccountDb accountDb
        , IRedisDb redisDb, IOptions<RedisHashKeys> redisHashKeys)
    {
        _logger = logger;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<NotificationResponse> Post(NotificationRequest request)
    {
        var response = new NotificationResponse();

        (var errorCode, response.NotificationList) = await _redisDb.GetNotificationAsync();
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(new { ID = request.AccountID }, "Send Notification Success");
        return response;
    }

}
