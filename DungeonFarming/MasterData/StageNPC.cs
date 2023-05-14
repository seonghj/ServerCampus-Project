namespace DungeonFarming.MasterData;
public class StageNPCGetter
{
    public Int32 Code { get; set; }
    public String NPCinfo { set; get; }
}

public class StageNPC
{
    public Int32 Code { get; set; }

    public List<NPCInfo> NPCInfoList { set; get; }

    public List<Int32> NPCList { set; get; }

    public Dictionary<Int32, Int32> NPCCount { set; get; }

    public Dictionary<Int32, Int32> NPCExp { set; get; }
}

public class NPCInfo
{
    public Int32 NPCCode { get; set;}
    public Int32 Count { get; set; }
    public Int32 Exp { get; set; }
}