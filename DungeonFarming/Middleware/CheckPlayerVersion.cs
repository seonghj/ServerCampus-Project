using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.Services;
using Microsoft.AspNetCore.Http;
using DungeonFarming.Security;
using Microsoft.Extensions.Options;

namespace DungeonFarming.Middleware;

public class CheckPlayerVersion
{
    private readonly RequestDelegate _next;
    private readonly IOptions<Versions> _version;

    public CheckPlayerVersion(RequestDelegate next, IOptions<Versions> version)
    {
        _version = version;
        _next = next;
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

        string ClientVer;
        string MasterDataVer;


        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 4096, true))
        {
            var bodyStr = await reader.ReadToEndAsync();

            if (await CheckBodyData(context, bodyStr))
            {
                return;
            }

            var document = JsonDocument.Parse(bodyStr);

            if (CheckJsonFormat(context, document, out ClientVer, out MasterDataVer))
            {
                return;
            }

            if (await CheckVersion(context, ClientVer, MasterDataVer))
            {
                return;
            }
        }

        context.Request.Body.Position = 0;

        await _next(context);
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

    private bool CheckJsonFormat(HttpContext context, JsonDocument document
        , out string ClientVer, out string MasterDataVer)
    {
        try
        {
            ClientVer = document.RootElement.GetProperty("ClientVersion").GetString();
            MasterDataVer = document.RootElement.GetProperty("MasterDataVersion").GetString();
            return false;
        }
        catch
        {
            ClientVer = "";
            MasterDataVer = "";
            var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
            {
                result = ErrorCode.VersionCheckFailWrongVersion
            });

            var bytes = Encoding.UTF8.GetBytes(errorJsonResponse);
            context.Response.Body.WriteAsync(bytes, 0, bytes.Length);

            return true;
        }
    }

    private async Task<bool> CheckVersion(HttpContext context, string ClientVersion, string MasterDataVersion)
    {
        if (string.CompareOrdinal(ClientVersion, _version.Value.Client) == 0 &&
            string.CompareOrdinal(MasterDataVersion, _version.Value.MasterData) == 0)
        {
            return false;
        }


        var errorJsonResponse = JsonSerializer.Serialize(new MiddlewareResponse
        {
            result = ErrorCode.ClinetVersionNotMatch
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
