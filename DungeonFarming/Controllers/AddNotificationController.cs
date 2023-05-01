using System;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
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
public class AddNotification : ControllerBase
{
    private readonly IRedisDb _redisDb;
    readonly ILogger<Notification> _logger;
    readonly IOptions<RedisHashKeys> _hashkey;

    public AddNotification(ILogger<Notification> logger, IRedisDb redisDb, IOptions<RedisHashKeys> redisHashKeys)
    {
        _logger = logger;
        _redisDb = redisDb;
        _hashkey = redisHashKeys;
    }

    [HttpPost]
    public async Task<AddNotificationResponse> Post(AddNotificationRequest request)
    {
        var response = new AddNotificationResponse();
        var NotificationKey = _hashkey.Value.Notification;

        var errorCode = await _redisDb.SetNotificationAsync(NotificationKey, request.Title, request.Content);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(request.Title , "Add Notification Success");
        return response;
    }

}
