using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using Microsoft.AspNetCore.Http;

namespace DungeonFarming.Middleware;

public class CheckPlayerAuth
{
    private readonly Services.IRedisDb _RedisDb;
    private readonly RequestDelegate _next;

    public CheckPlayerAuth(RequestDelegate next, Services.IRedisDb redisDb)
    {
        _RedisDb = redisDb;
        _next = next;
    }
}
