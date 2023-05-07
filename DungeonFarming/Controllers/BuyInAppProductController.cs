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
public class BuyInAppProduct : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;

    public BuyInAppProduct(ILogger<Login> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    [HttpPost]
    public async Task<BuyProductResponse> Post(BuyProductRequest request)
    {
        var response = new BuyProductResponse();

        var errorCode = await _gameDb.InAppProductSentToMail(request.UID, request.ProductCode, request.ReceiptCode);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(EventIdDic[EventType.Login], new { UID = request.UID }, "Item In Mail Send Success");
        return response;
    }

}
