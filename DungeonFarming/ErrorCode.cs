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
    PlayerArmorTypeMisMatch = 3011,
    PlayerHelmetTypeMisMatch = 3012,
    PlayerCloakTypeMisMatch = 3012,
    PlayerDressTypeMisMatch = 3013,
    PlayerPantsTypeMisMatch = 3012,
    PlayerMustacheTypeMisMatch = 3012,
    PlayerArmorCodeMisMatch = 3013,
    PlayerHelmetCodeMisMatch = 3014,
    PlayerCloakCodeMisMatch = 3015,
    PlayerDressCodeMisMatch = 3016,
    PlayerPantsCodeMisMatch = 3017,
    PlayerMustacheCodeMisMatch = 3018,
    PlayerHairCodeMisMatch = 3019,
    PlayerCheckCodeError = 3010,
    PlayerLookTypeError = 3011,

    PlayerStatusChangeFail = 3012,
    PlayerIsExistGame = 3013,
    GetPlayerListFail = 3014,

    //GameDb 4000~ 
    GetGameDbConnectionFail = 4002,

    //Redis 5000-
    RedisDbConnectionFail = 5002
}