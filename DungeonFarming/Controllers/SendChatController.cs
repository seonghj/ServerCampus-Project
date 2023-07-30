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
using StackExchange.Redis;

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class SendChatController : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;
    readonly IRedisDb _redisDb;

    public SendChatController(ILogger<Login> logger, IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<SendChatResponse> Post(SendChatRequest request)
    {
        var response = new SendChatResponse();

        Int32 uid = request.UID;
        Int32 channel = request.Channel;
        string message = request.Message;

        var errorCode = await _redisDb.SendChat(uid, channel, message);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        response.Result = errorCode;
        _logger.ZLogInformationWithPayload(new { UID = request.UID }, $"Send chat {uid} Clear Success");
        return response;
    }
}
