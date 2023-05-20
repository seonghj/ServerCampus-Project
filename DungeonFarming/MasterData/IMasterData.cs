using DungeonFarming.Services;
using Microsoft.Extensions.Options;

namespace DungeonFarming.MasterData;
public interface IMasterData
{
    //private Dictionary<Int32, Item> ItemDict { get;}

    //public Dictionary<Int32, ItemAttribute> ItemAttributeDict { get;}

    //public Dictionary<Int32, InAppProduct> InAppProductDict { get;}

    //public Dictionary<Int32, Attendance> AttendanceDict { get; }

    //public Dictionary<Int32, StageItem> StageItemDict { get; }

    //public Dictionary<Int32, StageNPC> StageNPCDict { get; }

    Task<ErrorCode> InitMasterData_Item();

    Task<ErrorCode> InitMasterData_ItemAttribute();

    Task<ErrorCode> InitMasterData_Attendance();

    Task<ErrorCode> InitMasterData_InAppProduct();

    Task<ErrorCode> InitMasterData_StageItem();

    Task<ErrorCode> InitMasterData_StageNPC();

    Task<ErrorCode> InitMasterData();

    Item getItemData(Int32 Code);

    ItemAttribute getItemAttributeData(Int32 Code);

    InAppProduct getInAppProductData(Int32 Code);

    Attendance getAttendanceData(Int32 Code);

    StageItem getStageItemData(Int32 Code);

    StageNPC getStageNPCData(Int32 Code);
   
}