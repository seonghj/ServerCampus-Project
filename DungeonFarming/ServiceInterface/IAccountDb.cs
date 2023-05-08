using System;
using System.Threading.Tasks;


namespace DungeonFarming.Services;

public interface IAccountDb : IDisposable
{
    public Task<ErrorCode> CreateAccountAsync(string AccountId, string pw);

    public Task<ErrorCode> VerifyAccount(string AccountId, string pw);

    public Task<ErrorCode> DeleteAccountAsync(string AccountId);
}