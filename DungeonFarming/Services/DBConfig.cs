namespace DungeonFarming.Services;

public class DbConfig
{
    public String MasterDb { get; set; }
    public String AccountDb { get; set; }
    public String CharacterDb { get; set; }
    public String Memcached { get; set; }
}