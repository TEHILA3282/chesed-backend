using System.Text.Json;
using System.Globalization;
using ChafetzChesed.BLL.Exceptions;
using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChafetzChesed.BLL.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _email;
        private readonly IAuditLogger _audit;
        private readonly ILogger<RegistrationService> _log;

        public RegistrationService(
            AppDbContext context,
            IEmailService email,
            IAuditLogger audit,
            ILogger<RegistrationService> log)
        {
            _context = context;
            _email = email;
            _audit = audit;
            _log = log;
        }

        private static string NormalizeEmail(string? email)
            => (email ?? string.Empty).Trim().ToLowerInvariant();

        private async Task SendEmailSafeAsync(string to, string subject, string body)
        {
            try { await ((dynamic)_email).SendEmailAsync(to, subject, body); }
            catch { try { await ((dynamic)_email).SendAsync(to, subject, body); } catch { } }
        }

        public async Task<IEnumerable<Registration>> GetAllAsync()
            => await _context.Registrations.AsNoTracking().ToListAsync();

        public async Task<Registration?> GetByIdAsync(string id, int institutionId)
            => await _context.Registrations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ID == id && r.InstitutionId == institutionId);

        public async Task<Registration> AddAsync(Registration registration)
        {
            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();
            return registration;
        }

        public async Task<bool> DeleteAsync(string id, int institutionId)
        {
            var reg = await _context.Registrations.FindAsync(institutionId, id);
            if (reg == null) return false;
            _context.Registrations.Remove(reg);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateStatusAsync(string registrationId, int institutionId, string newStatus)
        {
            var registration = await _context.Registrations.FindAsync(institutionId, registrationId);
            if (registration == null) return false;
            registration.RegistrationStatus = newStatus;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<Registration>> GetPendingAsync(int institutionId)
            => await _context.Registrations
                .AsNoTracking()
                .Where(r => r.RegistrationStatus == "ממתין" && r.InstitutionId == institutionId)
                .ToListAsync();

        public async Task<List<Registration>> GetByStatusAsync(int institutionId, string status)
            => await _context.Registrations
                .AsNoTracking()
                .Where(r => r.InstitutionId == institutionId && r.RegistrationStatus == status)
                .ToListAsync();

        public async Task<bool> ExistsAsync(string email, string id, int institutionId)
        {
            var normalizedId = (id ?? string.Empty).PadLeft(9, '0');
            var normalizedEmail = NormalizeEmail(email);

            return await _context.Registrations.AnyAsync(r =>
                r.InstitutionId == institutionId &&
                (r.Email == normalizedEmail || r.ID == normalizedId) &&
                r.RegistrationStatus != "נדחה");
        }

        public async Task<bool> UpdateAsync(Registration updated)
        {
            var existing = await _context.Registrations.FindAsync(updated.InstitutionId, updated.ID);
            if (existing == null) return false;

            // שמירה על הסיסמה הקיימת אם לא שינית אותה בחוץ
            updated.Password = existing.Password;

            _context.Entry(existing).CurrentValues.SetValues(updated);
            existing.StatusUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePartialAsync(string userId, RegistrationUpdateDto dto, int institutionId, string actorId)
        {
            var existing = await _context.Registrations
                .FirstOrDefaultAsync(r => r.ID == userId && r.InstitutionId == institutionId);
            if (existing == null) return false;

            var changes = new List<object>();

            if (dto.Email is not null)
            {
                var newEmail = NormalizeEmail(dto.Email);
                var oldEmail = NormalizeEmail(existing.Email);

                if (!string.Equals(newEmail, oldEmail, StringComparison.Ordinal))
                {
                    bool emailExists = await _context.Registrations.AnyAsync(r =>
                        r.InstitutionId == institutionId &&
                        r.Email == newEmail &&
                        r.ID != userId &&
                        r.RegistrationStatus != "נדחה");

                    if (emailExists)
                        throw new EmailAlreadyExistsException(dto.Email!);

                    changes.Add(new { field = "Email", old = existing.Email, @new = dto.Email });
                    existing.Email = newEmail;
                }
                else
                {
                    dto.Email = null; // אין שינוי
                }
            }

            void Set<T>(string field, T? newVal, T? curVal, Action apply)
            {
                if (newVal is null) return;
                if (!EqualityComparer<T?>.Default.Equals(newVal, curVal))
                {
                    changes.Add(new { field, old = curVal, @new = newVal });
                    apply();
                }
            }

            static DateTime? ParseDob(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (DateTime.TryParseExact(s, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    return d;
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out d))
                    return d;
                return null;
            }

            Set("FirstName", dto.FirstName, existing.FirstName, () => existing.FirstName = dto.FirstName!);
            Set("LastName", dto.LastName, existing.LastName, () => existing.LastName = dto.LastName!);
            Set("PhoneNumber", dto.PhoneNumber, existing.PhoneNumber, () => existing.PhoneNumber = dto.PhoneNumber);
            Set("LandlineNumber", dto.LandlineNumber, existing.LandlineNumber, () => existing.LandlineNumber = dto.LandlineNumber);

            var newDob = ParseDob(dto.DateOfBirth);
            Set("DateOfBirth",
                newDob?.Date,
                existing.DateOfBirth?.Date,
                () => existing.DateOfBirth = newDob!.Value.Date);

            Set("PersonalStatus", dto.PersonalStatus, existing.PersonalStatus, () => existing.PersonalStatus = dto.PersonalStatus);
            Set("Street", dto.Street, existing.Street, () => existing.Street = dto.Street);
            Set("City", dto.City, existing.City, () => existing.City = dto.City);
            Set("HouseNumber", dto.HouseNumber, existing.HouseNumber, () => existing.HouseNumber = dto.HouseNumber);

            if (changes.Count == 0) return false;

            existing.StatusUpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var json = JsonSerializer.Serialize(changes, AuditLogger.JsonOpts);
            _log.LogInformation("About to write AuditLog. userId={UserId}, inst={Inst}, json={Json}", userId, institutionId, json);

            await _audit.LogAsync(new AuditLog
            {
                InstitutionId = institutionId,
                Entity = "Registration",
                EntityId = existing.ID,
                ChangedBy = string.IsNullOrWhiteSpace(actorId) ? "system" : actorId,
                ChangesJson = json
            });

            await SendEmailSafeAsync(existing.Email, "העדכון נקלט", "תודה, העדכון נקלט במערכת.");
            return true;
        }
    }
}
