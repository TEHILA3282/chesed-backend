using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using ChafetzChesed.BLL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChafetzChesed.BLL.Services
{
    public class DepositTypeService : IDepositTypeService
    {
        private readonly AppDbContext _context;

        public DepositTypeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DepositType>> GetAllAsync() =>
            await _context.DepositTypes.ToListAsync();

        public async Task<DepositType> GetByIdAsync(int id) =>
            await _context.DepositTypes.FindAsync(id);

        public async Task<DepositType> AddAsync(DepositType depositType)
        {
            _context.DepositTypes.Add(depositType);
            await _context.SaveChangesAsync();
            return depositType;
        }

        public async Task<DepositType> UpdateAsync(DepositType depositType)
        {
            _context.DepositTypes.Update(depositType);
            await _context.SaveChangesAsync();
            return depositType;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.DepositTypes.FindAsync(id);
            if (entity == null) return false;

            _context.DepositTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
