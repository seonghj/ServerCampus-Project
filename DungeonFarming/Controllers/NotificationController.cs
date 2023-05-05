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
    readonly IOptions<RedisHashKeys> _hashkey;

    public Notification(ILogger<Notification> logger, IAccountDb accountDb
        , IRedisDb redisDb, IOptions<RedisHashKeys> redisHashKeys)
    {
        _logger = logger;
        _redisDb = redisDb;
        _hashkey = redisHashKeys;
    }

    [HttpPost]
    public async Task<NotificationResponse> Post(NotificationRequest request)
    {
        var response = new NotificationResponse();
        var NotificationKey = _hashkey.Value.Notification;

        (var errorCode, response.NotificationList) = await _redisDb.GetNotificationAsync(NotificationKey);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(new { ID = request.AccountID }, "Send Notification Success");
        return response;
    }

}
