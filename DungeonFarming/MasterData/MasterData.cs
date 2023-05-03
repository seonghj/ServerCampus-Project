using System;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
using DungeonFarming.Security;
using DungeonFarming.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SqlKata.Execution;
using ZLogger;

namespace DungeonFarming.MasterData;

public class MasterData:IMasterData
{
    readonly IOptions<DbConfig> _dbConfig;
    readonly ILogger<MasterData> _logger;

    IDbConnection _dbConn;
    SqlKata.Compilers.MySqlCompiler _compiler;
    QueryFactory _queryFactory;

    public Dictionary<Int32, Item> Items { get; set; }
    public Dictionary<Int32, ItemAttribute> ItemAttributes { get; set; }

    public Dictionary<Int32, InAppProduct> InAppProducts { get; set; }

    public Dictionary<Int32, Attendance> Attendances { get; set; }

    public Dictionary<Int32, StageItem> StageItems { get; set; }

    public Dictionary<Int32, StageNPC> StageNPCs { get; set; }

    public MasterData(ILogger<MasterData> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        DBOpen();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new SqlKata.Execution.QueryFactory(_dbConn, _compiler);

        GetItemData();
        GetItemAttribute();
        GetInAppProduct();
        GetAttendance();
        GetStageItem();
        GetStageNPC();
    }

    public async Task<ErrorCode> GetItemData()
    {
        try
        {
            _logger.ZLogDebug(
                $"[GetItemData From MasterData]");

            var result = await _queryFactory.Query("MasterDataItem").GetAsync<Item>();

            Items = new Dictionary<Int32, Item>();

            foreach (var it in result.ToList()) {Items.Add(it.Code, it);}

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[[GetItemData From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> GetItemAttribute()
    {
        try
        {
            _logger.ZLogDebug(
                $"[GetItemAttribute From MasterData]");

            var result = await _queryFactory.Query("MasterDataItemAttribute").GetAsync<ItemAttribute>();

            ItemAttributes = new Dictionary<Int32, ItemAttribute>();

            foreach (var it in result.ToList()) { ItemAttributes.Add(it.Code, it); }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[[GetItemData From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> GetAttendance()
    {
        try
        {
            _logger.ZLogDebug(
                $"[GetAttendance From MasterData]");

            var result = await _queryFactory.Query("MasterDataAttendance").GetAsync<Attendance>();

            Attendances = new Dictionary<Int32, Attendance>();

            foreach (var it in result.ToList()) { Attendances.Add(it.Code, it); }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[[GetAttendance From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> GetInAppProduct()
    {
        try
        {
            _logger.ZLogDebug(
                $"[GetInAppProduct From MasterData]");

            var result = await _queryFactory.Query("masterdatainappproduct").GetAsync<InAppProductGetter>();

            InAppProducts = new Dictionary<Int32, InAppProduct>();

            foreach (var it in result.ToList())
            {
                List<ProductItems> list = JsonSerializer.Deserialize<List<ProductItems>>(it.Item);

                InAppProducts.Add(it.Code, new InAppProduct
                {
                    Code = it.Code,
                    Item = list
                });
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[[GetInAppProduct From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> GetStageItem()
    {
        try
        {
            _logger.ZLogDebug(
                $"[GetStageItem From MasterData]");

            var result = await _queryFactory.Query("MasterDataStageItem").GetAsync<StageItemGetter>();

            StageItems = new Dictionary<Int32, StageItem>();

            foreach (var it in result.ToList())
            {
                List<Int32> list = JsonSerializer.Deserialize<List<Int32>>(it.ItemCode);

                StageItems.Add(it.Code, new StageItem
                {
                    Code = it.Code,
                    ItemCode = list
                });
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[[GetStageItem From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> GetStageNPC()
    {
        try
        {
            _logger.ZLogDebug(
                $"[GetStageNPC From MasterData]");

            var result = await _queryFactory.Query("MasterDataStageNPC").GetAsync<StageNPCGetter>();

            StageNPCs = new Dictionary<Int32, StageNPC>();

            foreach (var it in result.ToList())
            {
                List<NPCInfo> list = JsonSerializer.Deserialize<List<NPCInfo>>(it.NPCinfo);

                StageNPCs.Add(it.Code, new StageNPC
                {
                    Code = it.Code,
                    NPCInfoList = list
                });
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[[GetStageNPC From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    private Task<ErrorCode> GetMasterData()
    {
        Task<ErrorCode> errorCode;
        errorCode = GetItemData();
        errorCode = GetItemAttribute();
        errorCode = GetInAppProduct();
        errorCode = GetAttendance();
        errorCode = GetStageItem();
        errorCode = GetStageNPC();
        return errorCode;
    }

    private void DBOpen()
    {
        _dbConn = new MySqlConnection(_dbConfig.Value.GameDb);

        _dbConn.Open();
    }

    private void DBClose()
    {
        _dbConn.Close();
    }

    public void Dispose()
    {
        DBClose();
    }
}