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
public class RequestMail : ControllerBase
{
    readonly IGameDb _gameDb;
    private readonly IRedisDb _redisDb;
    readonly ILogger<Login> _logger;
    readonly IOptions<Versions> _version;

    public RequestMail(ILogger<Login> logger, IGameDb gameDb
        , IRedisDb redisDb, IOptions<Versions> version)
    {
        _logger = logger;
        _gameDb = gameDb;
        _redisDb = redisDb;
        _version = version;
    }

    [HttpPost]
    public async Task<MailResponse> Post(MailRequest request)
    {
        var response = new MailResponse();

        if (request.ClientVersion != _version.Value.Client)
        {
            response.Result = ErrorCode.ClinetVersionNotMatch;
            return response;
        }

        (var errorCode, response.MailList) = await _gameDb.GetMailAsync(request.UID, request.Page);

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { UID = request.UID }, "Mail Send Success");
        return response;
    }
}
