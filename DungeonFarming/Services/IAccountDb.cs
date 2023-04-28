using System;
using System.Threading.Tasks;


namespace DungeonFarming.Services;

public interface IAccountDb
{
    public Task<ErrorCode> CreateAccountAsync(String AccountId, String pw);
    
    public Task<ErrorCode> VerifyAccount(String AccountId, String pw);

    public Task<ErrorCode> DeleteAccountAsync(String AccountId);
}