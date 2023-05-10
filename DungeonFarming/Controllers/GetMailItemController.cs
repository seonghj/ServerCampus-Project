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
public class GetMailItem : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;

    public GetMailItem(ILogger<Login> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    [HttpPost]
    public async Task<MailItemResponse> Post(MailItemRequest request)
    {
        var response = new MailItemResponse();

        (var errorCode, response.Items) = await _gameDb.GetItemFromMailAsync(request.UID, request.MailCode);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(new { UID = request.UID }, "Item In Mail Send Success");
        return response;
    }
}
