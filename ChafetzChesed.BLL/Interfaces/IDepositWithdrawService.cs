using ChafetzChesed.DAL.Entities;

public interface IDepositWithdrawService
{
    Task<DepositWithdrawRequest> CreateAsync(string clientId, int institutionId, decimal amount, string text);
}
