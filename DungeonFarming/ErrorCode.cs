﻿using System;

// 1000 ~ 19999
public enum ErrorCode : UInt16
{
    None = 0,

    // Common 1000 ~
    UnhandleException = 1001,
    RedisFailException = 1002,
    InValidRequestHttpBody = 1003,
    AuthTokenFailWrongAuthToken = 1006,
    ClinetVersionNotMatch = 1007,
    VersionCheckFailWrongVersion = 1008,

    // Account 2000 ~
    CreateAccountFailException = 2001,    
    LoginFailException = 2002,
    LoginFailUserNotExist = 2003,
    LoginFailPwNotMatch = 2004,
    LoginFailSetAuthToken = 2005,
    AuthTokenMismatch = 2006,
    AuthTokenNotFound = 2007,
    AuthTokenFailWrongKeyword = 2008,
    AuthTokenFailSetNx = 2009,
    AccountIdMismatch = 2010,
    DuplicatedLogin = 2011,
    CreateAccountFailInsert = 2012,
    LoginFailAddRedis = 2014,
    CheckAuthFailNotExist = 2015,
    CheckAuthFailNotMatch = 2016,
    CheckAuthFailException = 2017,

    // Player 3000 ~
    CreatePlayerRollbackFail = 3001,
    CreatePlayerFailNoSlot = 3002,
    CreatePlayerFailException = 3003,
    PlayerNotExist = 3004,
    CountPlayersFail = 3005,
    DeletePlayerFail = 3006,
    GetPlayerInfoFail = 3007,
    InvalidPlayerInfo = 3008,
    GetPlayerItemsFail = 3009,
    PlayerCountOver = 3010,
    ItemEnhanceError = 3012,
    ItemEnhanceDisable = 3013,

    PlayerStatusChangeFail = 3020,
    PlayerIsExistGame = 3021,
    GetPlayerListFail = 3022,

    InsertPlayerItemFail = 3030,
    PlayerLoginFail = 3031,

    SendAttendenceRewordsFail = 3040,

    //GameDb 4000~ 
    GetGameDbConnectionFail = 4002,
    SetMailFail = 4010,
    GetMailFail = 4011,
    GetMailItemFail = 4012,
    AlreadyGetMailItem = 4013,
    ProductAlreadyPaid = 4014,
    ReceiptInsertError = 4015,
    MailExpirationDateOut = 4016,
    //Redis 5000-
    RedisDbConnectionFail = 5002,

    // InStage 6000~
    DisableStartStage = 6001
}