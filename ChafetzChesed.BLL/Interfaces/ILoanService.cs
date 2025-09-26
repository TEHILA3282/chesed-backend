using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Entities;

namespace ChafetzChesed.BLL.Interfaces
{
    public interface ILoanService
    {
        Task<List<Loan>> GetAllAsync();
        Task<Loan?> GetByIdAsync(int id);
        Task<Loan> AddAsync(Loan loan);
        Task<Loan> UpdateAsync(Loan loan);
        Task<bool> DeleteAsync(int id);
        Task<Loan> CreateAsync(CreateLoanDto dto, Registration currentUser);
    }
}
