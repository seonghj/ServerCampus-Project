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
public class ReceiveChatController : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;
    readonly IRedisDb _redisDb;

    public ReceiveChatController(ILogger<Login> logger, IGameDb gameDb, IRedisDb redisDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
    }

    [HttpPost]
    public async Task<ReceiveChatResponse> Post(ReceiveChatRequest request)
    {
        var response = new ReceiveChatResponse();

        Int32 uid = request.UID;
        int channel = request.Channel;
        string messageID = request.LatestMessageID;

        (var errorCode, response.Chats)= await _redisDb.ReceiveLatestChat(uid, channel, messageID);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        response.Result = errorCode;

        _logger.ZLogInformationWithPayload(new { UID = request.UID }, $"Receive chat {uid} Clear Success");
        return response;
    }
}
