//using System;
//using System.Threading.Tasks;
//using DungeonFarming.ModelDB;
//using DungeonFarming.ModelReqRes;
//using DungeonFarming.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using ZLogger;
//using static LogManager;

//namespace APIServer.Controllers;

//[ApiController]
//[Route("[controller]")]
//public class CreateCharacter : ControllerBase
//{
//    readonly ICharacterDb _characterDb;
//    readonly IMemoryDb _memoryDb;
//    readonly ILogger<CreateCharacter> _logger;

//    // DB를 바꿀때 GameDB부분을 수정해서 사용하기만 하면 됨
//    public CreateCharacter(ILogger<CreateCharacter> logger, ICharacterDb characterDb, IMemoryDb memoryDb)
//    {
//        _logger = logger;
//        _characterDb = characterDb;
//        _memoryDb = memoryDb;
//    }

//    [HttpPost]
//    public async Task<PkCreateCharacterResp> Post(PkCreateCharacterReq request)
//    {
//        //var userInfo = (AuthUser)HttpContext.Items[nameof(AuthUser)]!;

//        var response = new PkCreateCharacterResp();

//        (var errorCode, var characterId) = await _characterDb.InsertCharacter(request.ID);
//        if (errorCode != ErrorCode.None)
//        {
//            response.Result = errorCode;
//            return response;
//        }

//        _logger.ZLogInformationWithPayload(EventIdDic[EventType.CreateCharacter], new { ID = request.ID},
//            $"CreateCharacter Success");
//        return response;
//    }

//    //public async Task<(ErrorCode, Int64)> CreateDB(Int64 accountId, string nickName)
//    //{
//    //    Int64 characterId = 0;
//    //    var errorCode = ErrorCode.None;

//    //    try
//    //    {
//    //        (errorCode, characterId) = await _characterDb.InsertCharacter(accountId, nickName);

//    //        errorCode = await _characterDb.InsertCharacterItem(characterId);
//    //    }
//    //    catch (Exception e)
//    //    {
//    //        DeleteCharacter(nickName, characterId);
//    //        _logger.ZLogError(e, $"[CreateCharacter] ErrorCode : {errorCode}, characterId : {characterId}");
//    //    }

//    //    return (ErrorCode.None, characterId);
//    //}
//}
