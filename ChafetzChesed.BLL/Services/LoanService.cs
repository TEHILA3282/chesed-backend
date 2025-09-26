using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChafetzChesed.BLL.Services
{
    public class LoanService : ILoanService
    {
        private readonly AppDbContext _context;

        public LoanService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Loan>> GetAllAsync() =>
            await _context.Loans
                .Include(l => l.Guarantors)
                .AsNoTracking()
                .ToListAsync();

        public async Task<Loan?> GetByIdAsync(int id) =>
            await _context.Loans
                .Include(l => l.Guarantors)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ID == id);

        public async Task<Loan> AddAsync(Loan loan)
        {
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
            return loan;
        }

        public async Task<Loan> UpdateAsync(Loan loan)
        {
            _context.Loans.Update(loan);
            await _context.SaveChangesAsync();
            return loan;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Loans.FindAsync(id);
            if (entity == null) return false;

            _context.Loans.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<Loan> CreateAsync(CreateLoanDto dto, Registration currentUser)
        {
            if (currentUser == null) throw new UnauthorizedAccessException("משתמש לא מאומת");

            var guarantors = (dto.Guarantors ?? new List<GuarantorDto>())
                .Where(g =>
                    !string.IsNullOrWhiteSpace(g.IdNumber) ||
                    !string.IsNullOrWhiteSpace(g.FullName) ||
                    !string.IsNullOrWhiteSpace(g.Phone) ||
                    !string.IsNullOrWhiteSpace(g.Occupation) ||
                    !string.IsNullOrWhiteSpace(g.City) ||
                    !string.IsNullOrWhiteSpace(g.Street) ||
                    !string.IsNullOrWhiteSpace(g.HouseNumber) ||
                    !string.IsNullOrWhiteSpace(g.LoanLink) ||
                    !string.IsNullOrWhiteSpace(g.Email))
                .Select(g => new LoanGuarantor
                {
                    IdNumber = g.IdNumber?.Trim(),
                    FullName = g.FullName?.Trim(),
                    Phone = g.Phone?.Trim(),
                    Occupation = g.Occupation?.Trim(),
                    City = g.City?.Trim(),
                    Street = g.Street?.Trim(),
                    HouseNumber = g.HouseNumber?.Trim(),
                    LoanLink = g.LoanLink?.Trim(),
                    Email = g.Email?.Trim()
                })
                .ToList();

            var loan = new Loan
            {
                ClientID = currentUser.ID,
                LoanTypeID = dto.LoanTypeId,
                LoanDate = DateTime.UtcNow,
                Amount = dto.Amount,
                InstallmentsCount = dto.PaymentsCount,
                Purpose = dto.LoanPurpose,
                PurposeDetails = dto.Description,
                InstitutionId = currentUser.InstitutionId,
                Guarantors = guarantors
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
            return loan;
        }
    }
}
