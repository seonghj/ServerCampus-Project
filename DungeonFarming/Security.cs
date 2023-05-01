using System;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DungeonFarming.Security;

public class Security
{
    private const string AllowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";
    public static string MakingSalt()
    {
        var bytes = new Byte[64];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return new string(bytes.Select(x => AllowableCharacters[x % AllowableCharacters.Length]).ToArray());
    }
    public static string PassWordHashing(string salt, string pw)
    {
        var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(salt + pw));
        var stringBuilder = new StringBuilder();
        foreach (var b in hash)
        {
            stringBuilder.AppendFormat("{0:x2}", b);
        }

        return stringBuilder.ToString();
    }

    public static string CreatePlayerAuth(string accountid)
    {
        var tmp = DateTime.Now.ToString("MMddyymmDDss")+accountid;
        return new string(tmp.Select(x => AllowableCharacters[x % AllowableCharacters.Length]).ToArray());
    }

    public static string CreateUID()
    {
        return DateTime.Now.ToString("MMddyyyyhhmmss");
    }
}