using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using ChafetzChesed.BLL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChafetzChesed.BLL.Services
{
    public class LoanTypeService : ILoanTypeService
    {
        private readonly AppDbContext _context;

        public LoanTypeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LoanType>> GetAllAsync() =>
            await _context.LoanTypes.ToListAsync();

        public async Task<LoanType> GetByIdAsync(int id) =>
            await _context.LoanTypes.FindAsync(id);

        public async Task<LoanType> AddAsync(LoanType loanType)
        {
            _context.LoanTypes.Add(loanType);
            await _context.SaveChangesAsync();
            return loanType;
        }

        public async Task<LoanType> UpdateAsync(LoanType loanType)
        {
            _context.LoanTypes.Update(loanType);
            await _context.SaveChangesAsync();
            return loanType;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.LoanTypes.FindAsync(id);
            if (entity == null) return false;

            _context.LoanTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
