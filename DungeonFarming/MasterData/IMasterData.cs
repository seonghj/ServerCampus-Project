using DungeonFarming.Services;
using Microsoft.Extensions.Options;

namespace DungeonFarming.MasterData;
public interface IMasterData
{
    public Dictionary<Int32, Item> ItemDict { get;}

    public Dictionary<Int32, ItemAttribute> ItemAttributeDict { get;}

    public Dictionary<Int32, InAppProduct> InAppProductDict { get;}

    public Dictionary<Int32, Attendance> AttendanceDict { get; }

    public Dictionary<Int32, StageItem> StageItemDict { get; }

    public Dictionary<Int32, StageNPC> StageNPCDict { get; }

    Task<ErrorCode> getMasterData_Item();

    Task<ErrorCode> getMasterData_ItemAttribute();

    Task<ErrorCode> getMasterData_Attendance();

    Task<ErrorCode> getMasterData_InAppProduct();

    Task<ErrorCode> getMasterData_StageItem();

    Task<ErrorCode> getMasterData_StageNPC();

    Task<ErrorCode> getMasterData();

    Item getItemData(Int32 Code);

    ItemAttribute getItemAttributeData(Int32 Code);

    InAppProduct getInAppProductData(Int32 Code);

    Attendance getAttendanceData(Int32 Code);

    StageItem getStageItemData(Int32 Code);

    StageNPC getStageNPCData(Int32 Code);
   
}