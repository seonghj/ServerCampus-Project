using DungeonFarming.Services;
using Microsoft.Extensions.Options;

namespace DungeonFarming.MasterData;
public interface IMasterData
{
    public Dictionary<Int32, Item> Items { get; set; }

    public Dictionary<Int32, ItemAttribute> ItemAttributes { get; set; }

    public Dictionary<Int32, InAppProduct> InAppProducts { get; set; }

    public Dictionary<Int32, Attendance> Attendances { get; set; }

    public Dictionary<Int32, StageItem> StageItems { get; set; }

    public Dictionary<Int32, StageNPC> StageNPCs { get; set; }

    Task<ErrorCode> GetItemData();

    Task<ErrorCode> GetItemAttribute();

    Task<ErrorCode> GetAttendance();

    Task<ErrorCode> GetInAppProduct();

    Task<ErrorCode> GetStageItem();

    Task<ErrorCode> GetStageNPC();

    Task<ErrorCode> GetMasterData();
}