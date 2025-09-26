using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using ChafetzChesed.BLL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChafetzChesed.BLL.Services
{
    public class DepositService : IDepositService
    {
        private readonly AppDbContext _context;
        public DepositService(AppDbContext context) => _context = context;

        public async Task<List<Deposit>> GetAllAsync() =>
            await _context.Deposits
                .AsNoTracking()
                .ToListAsync();

        public async Task<Deposit> GetByIdAsync(int id) =>
            await _context.Deposits
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.ID == id);

        public async Task<Deposit> AddAsync(Deposit deposit)
        {
            if (deposit.InstitutionId <= 0)
                throw new InvalidOperationException("InstitutionId חסר ב־Deposit. יש להזרים אותו לפני השמירה.");

            _context.Deposits.Add(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task<Deposit> UpdateAsync(Deposit deposit)
        {
            if (deposit.InstitutionId <= 0)
                throw new InvalidOperationException("InstitutionId חסר ב־Deposit.");

            _context.Deposits.Update(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Deposits.FindAsync(id);
            if (entity == null) return false;

            _context.Deposits.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

     
        public async Task<List<Deposit>> GetAllAsync(int institutionId) =>
            await _context.Deposits
                .Where(d => d.InstitutionId == institutionId)
                .AsNoTracking()
                .ToListAsync();

        public async Task<Deposit> GetByIdAsync(int id, int institutionId) =>
            await _context.Deposits
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.ID == id && d.InstitutionId == institutionId);

        public async Task<Deposit> AddAsync(Deposit deposit, int institutionId)
        {
            if (institutionId <= 0) throw new InvalidOperationException("InstitutionId לא תקין");
            deposit.InstitutionId = institutionId;

            _context.Deposits.Add(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task<Deposit> UpdateAsync(Deposit deposit, int institutionId)
        {
            if (deposit.InstitutionId != institutionId)
                throw new InvalidOperationException("InstitutionId לא תואם.");

            _context.Deposits.Update(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task<bool> DeleteAsync(int id, int institutionId)
        {
            var entity = await _context.Deposits
                .FirstOrDefaultAsync(d => d.ID == id && d.InstitutionId == institutionId);
            if (entity == null) return false;

            _context.Deposits.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
