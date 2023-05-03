using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Http;
using DungeonFarming.Security;
using Microsoft.Extensions.Options;

namespace DungeonFarming.Middleware;

public class CheckValidPlayer
{
    private readonly IRedisDb _RedisDb;
    private readonly RequestDelegate _next;
    private readonly IOptions<Versions> _version;

    public CheckValidPlayer(RequestDelegate next, IRedisDb redisDb, IOptions<Versions> version)
    {
        _RedisDb = redisDb;
        _next = next;
        _version = version;
    }

    public async Task Invoke(HttpContext context)
    {
        var formString = context.Request.Path.Value;
        if (string.Compare(formString, "/CreateAccount", StringComparison.OrdinalIgnoreCase) == 0)
        {
            await _next(context);

            return;
        }

        context.Request.EnableBuffering();

        string AuthToken;
        string AccountID;
        string ClientVersion;
        string MasterDataVersion;
        string userLockKey = "";


        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 4096, true))
        {
            var bodyStr = await reader.ReadToEndAsync();

            if (await CheckBodyData(context, bodyStr))
            {
                return;
            }

            Console.WriteLine(bodyStr);
            var document = JsonDocument.Parse(bodyStr);

            if (CheckJsonFormat(context, document, out AccountID, out AuthToken, out ClientVersion, out MasterDataVersion))
            {
                return;
            }


            if (string.Compare(formString, "/Login", StringComparison.OrdinalIgnoreCase) == 1)
            {

                var (GetAuthResult, userInfo) = await _RedisDb.GetPlayerAuthAsync(AccountID);
                if (GetAuthResult != ErrorCode.None)
                {
                    return;
                }

                if (await CheckAuthToken(context, userInfo, AuthToken))
                {
                    return;
                }

                userLockKey = Security.Security.MakePlayerLockKey(AccountID);
                if (await SetLock(context, userLockKey))
                {
                    return;
                }

                context.Items[nameof(AuthPlayer)] = userInfo;
            }
        }

        context.Request.Body.Position = 0;

        await _next(context);

        await _RedisDb.DeleteRequestLockAsync(userLockKey);
    }

    private async Task<bool> SetLock(HttpContext context, string AuthToken)
    {
        if (await _RedisDb.SetRequestLockAsync(AuthToken))
        {
            return false;
        }


        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.AuthTokenFailSetNx
        });
        var bytes = Encoding.UTF8.GetBytes(errorJsonResponse);
        await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        return true;
    }

    private async Task<bool> CheckBodyData(HttpContext context, string bodyStr)
    {
        if (string.IsNullOrEmpty(bodyStr) == false)
        {
            return false;
        }

        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.InValidRequestHttpBody
        });
        var bytes = Encoding.UTF8.GetBytes(errorJsonResponse);
        await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);

        return true;
    }

    private bool CheckJsonFormat(HttpContext context, JsonDocument document, out string accountID,
        out string authToken, out string ClientVersion, out string MasterDataVersion)
    {
        try
        {
            accountID = document.RootElement.GetProperty("AccountID").GetString();
            authToken = document.RootElement.GetProperty("AuthToken").GetString();
            ClientVersion = document.RootElement.GetProperty("AccountID").GetString();
            MasterDataVersion = document.RootElement.GetProperty("AuthToken").GetString();
            return false;
        }
        catch
        {
            accountID = ""; authToken = "";
            ClientVersion = ""; MasterDataVersion = "";
            var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
            {
                result = ErrorCode.AuthTokenFailWrongKeyword
            });

            var bytes = Encoding.UTF8.GetBytes(errorJsonResponse);
            context.Response.Body.WriteAsync(bytes, 0, bytes.Length);

            return true;
        }
    }

    private static async Task<bool> CheckAuthToken(HttpContext context, AuthPlayer userInfo, string authToken)
    {
        if (string.CompareOrdinal(userInfo.AuthToken, authToken) == 0)
        {
            return false;
        }


        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.AuthTokenFailWrongAuthToken
        });
        var bytes = Encoding.UTF8.GetBytes(errorJsonResponse);
        await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);

        return true;
    }

    //private static async Task<bool> CheckVersion(HttpContext context, string ClientVersion, string MasterDataVersion)
    //{
    //    if (string.CompareOrdinal(ClientVersion, _version.Value.Client) == 0)
    //    {
    //        return false;
    //    }


    //    var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
    //    {
    //        result = ErrorCode.ClinetVersionNotMatch
    //    });
    //    var bytes = Encoding.UTF8.GetBytes(errorJsonResponse);
    //    await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);

    //    return true;
    //}

    public class MiddlewareResponse
    {
        public ErrorCode result { get; set; }
    }
}
