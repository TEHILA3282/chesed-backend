using ChafetzChesed.DAL;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;

public class DepositWithdrawService : IDepositWithdrawService
{
    private readonly AppDbContext _db;
    public DepositWithdrawService(AppDbContext db) { _db = db; }

    public async Task<DepositWithdrawRequest> CreateAsync(string clientId, int institutionId, decimal amount, string text)
    {
        var entity = new DepositWithdrawRequest
        {
            ClientID = clientId,
            InstitutionId = institutionId,
            Amount = amount,
            RequestText = text,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.DepositWithdrawRequests.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }
}
