using ChafetzChesed.DAL.Entities;

namespace ChafetzChesed.BLL.Interfaces
{
    public interface IDepositTypeService
    {
        Task<List<DepositType>> GetAllAsync();
        Task<DepositType> GetByIdAsync(int id);
        Task<DepositType> AddAsync(DepositType depositType);
        Task<DepositType> UpdateAsync(DepositType depositType);
        Task<bool> DeleteAsync(int id);
    }
}
