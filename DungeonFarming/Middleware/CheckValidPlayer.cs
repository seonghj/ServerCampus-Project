using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DungeonFarming.Middleware;

public class CheckValidPlayer
{
    private readonly IRedisDb _RedisDb;
    private readonly RequestDelegate _next;
    private readonly IOptions<Versions> _version;

    public CheckValidPlayer(RequestDelegate next, IRedisDb redisDb)
    {
        _RedisDb = redisDb;
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var formString = context.Request.Path.Value;
        if (string.Compare(formString, "/CreateAccount", StringComparison.OrdinalIgnoreCase) == 0 ||
            string.Compare(formString, "/Login", StringComparison.OrdinalIgnoreCase) == 0)
        {
            await _next(context);

            return;
        }

        context.Request.EnableBuffering();

        string AuthToken;
        string AccountID;
        string userLockKey = "";


        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 4096, true))
        {
            var bodyStr = await reader.ReadToEndAsync();

            if (await CheckBodyData(context, bodyStr))
            {
                return;
            }

            var document = JsonDocument.Parse(bodyStr);

            if (CheckJsonFormat(context, document, out AccountID, out AuthToken))
            {
                return;
            }


            var (GetAuthResult, userInfo) = await _RedisDb.GetPlayerAuthAsync(AccountID);
            if (GetAuthResult != ErrorCode.None)
            {
                return;
            }

            if (await CheckAuthToken(context, userInfo, AuthToken))
            {
                return;
            }

            userLockKey = Service.Security.MakePlayerLockKey(AccountID);
            if (await SetLock(context, userLockKey))
            {
                return;
            }

            context.Items[nameof(AuthPlayer)] = userInfo;
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
        out string authToken)
    {
        try
        {
            accountID = document.RootElement.GetProperty("AccountID").GetString();
            authToken = document.RootElement.GetProperty("AuthToken").GetString();
            return false;
        }
        catch
        {
            accountID = ""; 
            authToken = "";
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

    public class MiddlewareResponse
    {
        public ErrorCode result { get; set; }
    }
}
