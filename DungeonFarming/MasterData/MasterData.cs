using System;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonFarming.DBTableFormat;
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

    public Dictionary<Int32, Item> ItemDict { get; set; }
    public Dictionary<Int32, ItemAttribute> ItemAttributeDict { get; set; }

    public Dictionary<Int32, InAppProduct> InAppProductDict { get; set; }

    public Dictionary<Int32, Attendance> AttendanceDict { get; set; }

    public Dictionary<Int32, StageItem> StageItemDict { get; set; }

    public Dictionary<Int32, StageNPC> StageNPCDict { get; set; }

    public MasterData(ILogger<MasterData> logger, IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;

        DBOpen();

        _compiler = new SqlKata.Compilers.MySqlCompiler();
        _queryFactory = new SqlKata.Execution.QueryFactory(_dbConn, _compiler);

        getMasterData();
    }

    public async Task<ErrorCode> getMasterData_Item()
    {
        try
        {
            _logger.ZLogDebug(
                $"[Receive ItemData From MasterData]");

            var result = await _queryFactory.Query("MasterData_Item").GetAsync<Item>();

            ItemDict = new Dictionary<Int32, Item>();

            foreach (var it in result.ToList()) { ItemDict.Add(it.Code, it);}

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Receive ItemData From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> getMasterData_ItemAttribute()
    {
        try
        {
            _logger.ZLogDebug(
                $"[Receive ItemAttribute From MasterData]");

            var result = await _queryFactory.Query("MasterData_ItemAttribute").GetAsync<ItemAttribute>();

            ItemAttributeDict = new Dictionary<Int32, ItemAttribute>();

            foreach (var it in result.ToList()) { ItemAttributeDict.Add(it.Code, it); }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Receive ItemData From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> getMasterData_Attendance()
    {
        try
        {
            _logger.ZLogDebug(
                $"[Receive Attendance From MasterData]");

            var result = await _queryFactory.Query("MasterData_Attendance").GetAsync<Attendance>();

            AttendanceDict = new Dictionary<Int32, Attendance>();

            foreach (var it in result.ToList()) { AttendanceDict.Add(it.Code, it); }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Receive Attendance From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> getMasterData_InAppProduct()
    {
        try
        {
            _logger.ZLogDebug(
                $"[Receive InAppProduct From MasterData]");

            var result = await _queryFactory.Query("masterdata_inappproduct").GetAsync<InAppProductGetter>();

            InAppProductDict = new Dictionary<Int32, InAppProduct>();

            foreach (var it in result.ToList())
            {
                List<ItemCodeAndCount> list = JsonSerializer.Deserialize<List<ItemCodeAndCount>>(it.Item);

                InAppProductDict.Add(it.Code, new InAppProduct
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
                $"[Receive InAppProduct From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> getMasterData_StageItem()
    {
        try
        {
            _logger.ZLogDebug(
                $"[Receive StageItem From MasterData]");

            var result = await _queryFactory.Query("MasterData_StageItem").GetAsync<StageItemGetter>();

            StageItemDict = new Dictionary<Int32, StageItem>();

            foreach (var stageitems in result.ToList())
            {
                List<Int32> list = JsonSerializer.Deserialize<List<Int32>>(stageitems.ItemCode);

                Dictionary<Int32, Int32> itemCount = new Dictionary<Int32, Int32>();
                foreach (var it in list)
                {
                    if (itemCount.ContainsKey(it) == true)
                    {
                        itemCount[it] += 1;
                    }
                    else
                    {
                        itemCount.Add(it, 1);
                    }
                }

                StageItemDict.Add(stageitems.Code, new StageItem
                {
                    Code = stageitems.Code,
                    ItemCode = list,
                    ItemCount = itemCount
                });
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Receive StageItem From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }

    }

    public async Task<ErrorCode> getMasterData_StageNPC()
    {
        try
        {
            _logger.ZLogDebug(
                $"[Receive StageNPC From MasterData]");

            var result = await _queryFactory.Query("MasterData_StageNPC").GetAsync<StageNPCGetter>();

            StageNPCDict = new Dictionary<Int32, StageNPC>();

            foreach (var stageNPCs in result.ToList())
            {
                List<NPCInfo> list = JsonSerializer.Deserialize<List<NPCInfo>>(stageNPCs.NPCinfo);

                List<Int32> npcList = new List<Int32>();
                Dictionary<Int32, Int32> npcCount = new Dictionary<Int32, Int32>();
                Dictionary<Int32, Int32> npcExp = new Dictionary<Int32, Int32>();

                foreach (var npc in list)
                {
                    npcList.Add(npc.NPCCode);
                    npcCount.Add(npc.NPCCode, npc.Count);
                    npcExp.Add(npc.NPCCode, npc.Exp);
                }

                StageNPCDict.Add(stageNPCs.Code, new StageNPC
                {
                    Code = stageNPCs.Code,
                    NPCInfoList = list,
                    NPCList = npcList,
                    NPCCount = npcCount,
                    NPCExp = npcExp
                });
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Receive StageNPC From MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public async Task<ErrorCode> getMasterData()
    {
        try
        {
            _logger.ZLogDebug(
                $"[Receive ItemData From MasterData]");

            var result1 = await _queryFactory.Query("MasterData_Item").GetAsync<Item>();

            ItemDict = new Dictionary<Int32, Item>();

            foreach (var it in result1.ToList()) { ItemDict.Add(it.Code, it); }

            _logger.ZLogDebug(
                $"[Receive ItemAttribute From MasterData]");

            var result2 = await _queryFactory.Query("MasterData_ItemAttribute").GetAsync<ItemAttribute>();

            ItemAttributeDict = new Dictionary<Int32, ItemAttribute>();

            foreach (var it in result2.ToList()) { ItemAttributeDict.Add(it.Code, it); }

            _logger.ZLogDebug(
                $"[Receive Attendance From MasterData]");

            var result3 = await _queryFactory.Query("MasterData_Attendance").GetAsync<Attendance>();

            AttendanceDict = new Dictionary<Int32, Attendance>();

            foreach (var it in result3.ToList()) { AttendanceDict.Add(it.Code, it); }

            _logger.ZLogDebug(
                $"[Receive InAppProduct From MasterData]");

            var result4 = await _queryFactory.Query("masterdata_inappproduct").GetAsync<InAppProductGetter>();

            InAppProductDict = new Dictionary<Int32, InAppProduct>();

            foreach (var it in result4.ToList())
            {
                List<ItemCodeAndCount> list = JsonSerializer.Deserialize<List<ItemCodeAndCount>>(it.Item);

                InAppProductDict.Add(it.Code, new InAppProduct
                {
                    Code = it.Code,
                    Item = list
                });
            }

            _logger.ZLogDebug(
                $"[Receive StageItem From MasterData]");

            var result5 = await _queryFactory.Query("MasterData_StageItem").GetAsync<StageItemGetter>();

            StageItemDict = new Dictionary<Int32, StageItem>();

            foreach (var stageitems in result5.ToList())
            {
                List<Int32> list = JsonSerializer.Deserialize<List<Int32>>(stageitems.ItemCode);

                Dictionary<Int32, Int32> itemCount = new Dictionary<Int32, Int32>();
                foreach (var it in list)
                {
                    if (itemCount.ContainsKey(it) == true)
                    {
                        itemCount[it] += 1;
                    }
                    else
                    {
                        itemCount.Add(it, 1);
                    }
                }

                StageItemDict.Add(stageitems.Code, new StageItem
                {
                    Code = stageitems.Code,
                    ItemCode = list,
                    ItemCount = itemCount
                });
            }

            _logger.ZLogDebug(
                $"[Receive StageNPC From MasterData]");

            var result6 = await _queryFactory.Query("MasterData_StageNPC").GetAsync<StageNPCGetter>();

            StageNPCDict = new Dictionary<Int32, StageNPC>();

            foreach (var stageNPCs in result6.ToList())
            {
                List<NPCInfo> list = JsonSerializer.Deserialize<List<NPCInfo>>(stageNPCs.NPCinfo);

                List<Int32> npcList = new List<Int32>();
                Dictionary<Int32, Int32> npcCount = new Dictionary<Int32, Int32>();
                Dictionary<Int32, Int32> npcExp = new Dictionary<Int32, Int32>();

                foreach (var npc in list)
                {
                    npcList.Add(npc.NPCCode);
                    npcCount.Add(npc.NPCCode, npc.Count);
                    npcExp.Add(npc.NPCCode, npc.Exp);
                }

                StageNPCDict.Add(stageNPCs.Code, new StageNPC
                {
                    Code = stageNPCs.Code,
                    NPCInfoList = list,
                    NPCList = npcList,
                    NPCCount = npcCount,
                    NPCExp = npcExp
                });
            }

            return ErrorCode.None;
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,
                $"[Receive MasterData] ErrorCode: {ErrorCode.None}");
            return ErrorCode.CreateAccountFailException;
        }
    }

    public Item getItemData(Int32 Code)
    {
        return ItemDict[Code];
    }

    public ItemAttribute getItemAttributeData(Int32 Code)
    {
        return ItemAttributeDict[Code];
    }

    public InAppProduct getInAppProductData(Int32 Code)
    {
        return InAppProductDict[Code];
    }

    public Attendance getAttendanceData(Int32 Code)
    {
        return AttendanceDict[Code];
    }

    public StageItem getStageItemData(Int32 Code)
    {
        return StageItemDict[Code];
    }

    public StageNPC getStageNPCData(Int32 Code)
    {
        return StageNPCDict[Code];
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