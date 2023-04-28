using DungeonFarming.ModelDB;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using CloudStructures;
using CloudStructures.Structures;
using ZLogger;
using static LogManager;

namespace DungeonFarming.Services;

public class RedisDb : IMemoryDb
{
    public RedisConnection _Conn;
    private static readonly ILogger<RedisDb> s_logger = GetLogger<RedisDb>();

    public void Init(string address)
    {
        var config = new RedisConfig("default", address);
        _Conn = new RedisConnection(config);

        s_logger.ZLogDebug($"userDbAddress:{address}");
    }
}