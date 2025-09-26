using ChafetzChesed.DAL.Entities;

namespace ChafetzChesed.BLL.Interfaces
{
    public interface IDepositService
    {
        Task<List<Deposit>> GetAllAsync(int institutionId);
        Task<Deposit?> GetByIdAsync(int id, int institutionId);
        Task<Deposit> AddAsync(Deposit deposit, int institutionId);
        Task<Deposit> UpdateAsync(Deposit deposit, int institutionId);
        Task<bool> DeleteAsync(int id, int institutionId);
    }
}
