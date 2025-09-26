using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChafetzChesed.BLL.Exceptions;
using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.BLL.Services;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;

namespace ChafetzChesed.Controllers
{
    [ApiController]
    [Route("registration")]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly AppDbContext _context;
        private readonly ILogger<RegistrationController> _logger;
        private readonly IEmailService _email;
        private readonly IAuditLogger _audit;

        public RegistrationController(
            IRegistrationService registrationService,
            AppDbContext context,
            ILogger<RegistrationController> logger,
            IEmailService email,
            IAuditLogger audit)
        {
            _registrationService = registrationService;
            _context = context;
            _logger = logger;
            _email = email;
            _audit = audit;
        }

        private Registration? GetCurrentUser() => HttpContext.Items["User"] as Registration;

        private int ResolveInstitutionIdSafe(int? headerId = null, string? headerSlug = null)
        {
            if (headerId is int hid && hid > 0) return hid;
            if (HttpContext.Items.TryGetValue("InstitutionId", out var v) && v is int id && id > 0)
                return id;
            return 1;
        }

        [HttpPost("debug-body")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugBody()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            return Ok(new { contentType = Request.ContentType, length = Request.ContentLength, raw });
        }

        private static string? Clean(string? s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var cleaned = new string(s.Where(ch => ch != '\u200e' && ch != '\u200f').ToArray());
            return cleaned.Trim();
        }

        private static DateTime? ParseDob(string? s)
        {
            s = Clean(s);
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d))
                return d;

            if (DateTime.TryParseExact(s, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out d))
                return d;

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out d))
                return d;

            return null;
        }

        [HttpGet("check-exists")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> CheckExists(
            [FromQuery] string email,
            [FromQuery] string id,
            [FromQuery] int institutionId)
        {
            var exists = await _registrationService.ExistsAsync(email, id, institutionId);
            return Ok(exists);
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> Register(
            [FromBody] RegistrationCreateDto dto,
            [FromHeader(Name = "X-Institution-Id")] int? instHeader = null,
            [FromHeader(Name = "X-Institution-Slug")] string? slugHeader = null)
        {
            if (dto is null)
                return BadRequest("גוף הבקשה חסר או אינו בפורמט JSON תקין");

            var institutionId = ResolveInstitutionIdSafe(instHeader, slugHeader);

            if (string.IsNullOrWhiteSpace(dto.ID) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("חסרים שדות חובה");

            if (await _registrationService.ExistsAsync(dto.Email, dto.ID, institutionId))
                return Conflict("האימייל או תעודת הזהות כבר רשומים במוסד");

            var dob = ParseDob(dto.DateOfBirth);

            var entity = new Registration
            {
                ID = (dto.ID ?? string.Empty).PadLeft(9, '0'),
                Email = (Clean(dto.Email) ?? string.Empty).ToLowerInvariant(),
                Password = HashPassword(dto.Password),
                FirstName = Clean(dto.FirstName),
                LastName = Clean(dto.LastName),
                PhoneNumber = Clean(dto.PhoneNumber),
                LandlineNumber = Clean(dto.LandlineNumber),
                DateOfBirth = dob?.Date,
                PersonalStatus = Clean(dto.PersonalStatus),
                Street = Clean(dto.Street),
                City = Clean(dto.City),
                HouseNumber = Clean(dto.HouseNumber),
                RegistrationStatus = "ממתין",
                InstitutionId = institutionId,
                StatusUpdatedAt = DateTime.Now
            };

            await _registrationService.AddAsync(entity);

            return Created($"/api/registration/{entity.ID}", new { id = entity.ID, message = "נרשמת בהצלחה" });
        }

        [HttpPut("update-personal")]
        [Authorize]
        public async Task<IActionResult> UpdatePersonalDetails([FromBody] RegistrationUpdateDto dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized("משתמש לא מאומת");

            try
            {
                var ok = await _registrationService.UpdatePartialAsync(
                    user.ID, dto, ResolveInstitutionIdSafe(), user.ID);

                if (!ok) return Ok("אין שינוי לשמירה");
                return Ok("הפרטים האישיים עודכנו בהצלחה");
            }
            catch (EmailAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "האימייל כבר קיים במוסד." });
            }
        }

        [HttpPut("update-bank")]
        [Authorize]
        public async Task<IActionResult> UpdateBankDetails([FromBody] BankAccountUpdateDto dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized("משתמש לא מאומת");

            int inst = ResolveInstitutionIdSafe();

            var acct = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.RegistrationId == user.ID && b.InstitutionId == inst);

            var before = acct == null ? null : new
            {
                acct.BankNumber,
                acct.BranchNumber,
                acct.AccountNumber,
                acct.AccountOwnerName,
                acct.HasDirectDebit
            };

            if (acct == null)
            {
                acct = new BankAccount
                {
                    RegistrationId = user.ID,
                    InstitutionId = inst,
                    BankNumber = dto.BankNumber,
                    BranchNumber = dto.BranchNumber,
                    AccountNumber = dto.AccountNumber,
                    AccountOwnerName = dto.AccountOwnerName,
                    HasDirectDebit = dto.HasDirectDebit
                };
                _context.BankAccounts.Add(acct);
            }
            else
            {
                if (acct.InstitutionId <= 0) acct.InstitutionId = inst;
                acct.BankNumber = dto.BankNumber;
                acct.BranchNumber = dto.BranchNumber;
                acct.AccountNumber = dto.AccountNumber;
                acct.AccountOwnerName = dto.AccountOwnerName;
                acct.HasDirectDebit = dto.HasDirectDebit;
            }

            await _context.SaveChangesAsync();

            await _audit.LogAsync(new AuditLog
            {
                InstitutionId = inst,
                Entity = "BankAccount",
                EntityId = acct.RegistrationId,
                ChangedBy = user.ID,
                ChangesJson = JsonSerializer.Serialize(new
                {
                    Before = before,
                    After = new
                    {
                        acct.BankNumber,
                        acct.BranchNumber,
                        acct.AccountNumber,
                        acct.AccountOwnerName,
                        acct.HasDirectDebit
                    }
                }, AuditLogger.JsonOpts)
            });

            await _email.SendAsync(
                user.Email,
                "עדכון פרטי חשבון בנק",
                "פרטי חשבון הבנק שלך עודכנו בהצלחה במערכת.");

            return Ok("פרטי החשבון עודכנו בהצלחה");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}
