﻿using System;
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

namespace DungeonFarming.Controllers;

[ApiController]
[Route("[controller]")]
public class ReceiveMail : ControllerBase
{
    readonly IGameDb _gameDb;
    readonly ILogger<Login> _logger;

    public ReceiveMail(ILogger<Login> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    [HttpPost]
    public async Task<MailResponse> Post(MailRequest request)
    {
        var response = new MailResponse();

        (var errorCode, response.MailList) = await _gameDb.GetMailAsync(request.UID, request.Page);
        if (errorCode != ErrorCode.None)
        {
            response.Result = errorCode;
            return response;
        }

        _logger.ZLogInformationWithPayload(new { UID = request.UID }, "Mail Send Success");
        return response;
    }
}
