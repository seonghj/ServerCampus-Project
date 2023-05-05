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
    readonly IMasterData _masterdata;

    public GetMailItem(ILogger<Login> logger, IGameDb gameDb
        , IMasterData masterData)
    {
        _logger = logger;
        _gameDb = gameDb;
        _masterdata = masterData;
    }

    [HttpPost]
    public async Task<MailItemResponse> Post(MailItemRequest request)
    {
        var response = new MailItemResponse();

        (var errorCode, response.Items) = await _gameDb.GetMailItemAsync(request.UID, request.MailCode);

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { UID = request.UID }, "Item In Mail Send Success");
        return response;
    }
}
