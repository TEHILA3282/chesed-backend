using ChafetzChesed.BLL.Exceptions;
using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.BLL.Services;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChafetzChesed.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("registration")]
    [Route("api/registration")] // תמיכה גם ב-/api/registration כגיבוי
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

            if (!string.IsNullOrWhiteSpace(headerSlug))
            {
                var slug = headerSlug.Trim().ToLowerInvariant();
                var inst = _context.Institutions
                    .AsNoTracking()
                    .FirstOrDefault(i => i.Subdomain != null && i.Subdomain.ToLower() == slug);
                if (inst != null) return inst.InstitutionId;
            }

            if (HttpContext.Items.TryGetValue("InstitutionId", out var v) && v is int id && id > 0)
                return id;

            // ברירת מחדל
            return 1;
        }

        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            _logger.LogDebug("Ping called | TraceId={TraceId}", HttpContext.TraceIdentifier);
            return Ok(new { ok = true, at = DateTime.UtcNow });
        }

        [HttpPost("debug-body")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugBody()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            _logger.LogInformation("DebugBody | ContentType={CT} Length={Len} | TraceId={TraceId}",
                Request.ContentType, Request.ContentLength, HttpContext.TraceIdentifier);

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

        // =========================
        //  GET /check-exists
        // =========================
        [HttpGet("check-exists")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> CheckExists(
            [FromQuery] string email,
            [FromQuery] string id,
            [FromQuery] int institutionId)
        {
            var cleanEmail = Clean(email)?.ToLowerInvariant();
            var cleanId = (id ?? string.Empty).Trim();

            _logger.LogInformation(
                "GET /registration/check-exists | Email={Email} ID={Id} Inst={Inst} | TraceId={TraceId}",
                cleanEmail, cleanId, institutionId, HttpContext.TraceIdentifier);

            try
            {
                var exists = await _registrationService.ExistsAsync(cleanEmail, cleanId, institutionId);

                _logger.LogInformation(
                    "CheckExists result | Email={Email} ID={Id} Inst={Inst} Exists={Exists} | TraceId={TraceId}",
                    cleanEmail, cleanId, institutionId, exists, HttpContext.TraceIdentifier);

                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CheckExists failed | Email={Email} ID={Id} Inst={Inst} | TraceId={TraceId}",
                    cleanEmail, cleanId, institutionId, HttpContext.TraceIdentifier);

                return StatusCode(500, new ProblemDetails
                {
                    Title = "שגיאה בלתי צפויה בבדיקת משתמש קיים",
                    Status = 500,
                    Detail = ex.Message
                });
            }
        }

        // =========================
        //  POST /registration
        // =========================
        [HttpPost]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> Register(
            [FromBody] RegistrationCreateDto dto,
            [FromHeader(Name = "X-Institution-Id")] int? instHeader = null,
            [FromHeader(Name = "X-Institution-Slug")] string? slugHeader = null)
        {
            if (dto is null)
            {
                _logger.LogWarning("Register | Missing body (null) | TraceId={TraceId}", HttpContext.TraceIdentifier);
                return BadRequest(new ProblemDetails { Title = "גוף הבקשה חסר או אינו JSON", Status = 400 });
            }

            var institutionId = ResolveInstitutionIdSafe(instHeader, slugHeader);

            _logger.LogInformation(
                "POST /registration | X-Inst-Id={XId} X-Inst-Slug={XSlug} ResolvedInst={Resolved} | TraceId={TraceId}",
                Request.Headers["X-Institution-Id"].ToString(),
                Request.Headers["X-Institution-Slug"].ToString(),
                institutionId,
                HttpContext.TraceIdentifier);

            // ניקוי והקשחה
            dto.ID = (dto.ID ?? string.Empty).Trim();
            dto.Email = (Clean(dto.Email) ?? string.Empty).ToLowerInvariant();
            dto.Password = dto.Password?.Trim();

            // ולידציות בסיסיות
            if (string.IsNullOrWhiteSpace(dto.ID) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                _logger.LogWarning("Register | Missing required fields | TraceId={TraceId}", HttpContext.TraceIdentifier);
                return BadRequest(new ProblemDetails { Title = "חסרים שדות חובה", Status = 400 });
            }

            if (string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName))
            {
                _logger.LogWarning("Register | Missing name fields | TraceId={TraceId}", HttpContext.TraceIdentifier);
                return BadRequest(new ProblemDetails { Title = "שם פרטי/משפחה חסר", Status = 400 });
            }

            var zeout = dto.ID.PadLeft(9, '0');
            if (zeout.Length != 9 || !zeout.All(char.IsDigit))
            {
                _logger.LogWarning("Register | Invalid ID={ID} | TraceId={TraceId}", dto.ID, HttpContext.TraceIdentifier);
                return BadRequest(new ProblemDetails { Title = "תעודת זהות לא תקינה", Status = 400 });
            }

            var dob = ParseDob(dto.DateOfBirth);
            if (dob.HasValue)
            {
                if (dob.Value.Date > DateTime.Today)
                {
                    _logger.LogWarning("Register | Future DOB={DOB} | TraceId={TraceId}", dob, HttpContext.TraceIdentifier);
                    return BadRequest(new ProblemDetails { Title = "תאריך לידה עתידי אינו תקין", Status = 400 });
                }

                if (dob.Value < new DateTime(1900, 1, 1))
                {
                    _logger.LogWarning("Register | Out-of-range DOB={DOB} | TraceId={TraceId}", dob, HttpContext.TraceIdentifier);
                    return BadRequest(new ProblemDetails { Title = "תאריך לידה מחוץ לטווח מותר", Status = 400 });
                }
            }

            try
            {
                if (await _registrationService.ExistsAsync(dto.Email, zeout, institutionId))
                {
                    _logger.LogInformation("Register | Conflict (exists) Email={Email} ID={ID} Inst={Inst} | TraceId={TraceId}",
                        dto.Email, zeout, institutionId, HttpContext.TraceIdentifier);
                    return Conflict(new ProblemDetails { Title = "האימייל או הת״ז כבר רשומים במוסד", Status = 409 });
                }

                var entity = new Registration
                {
                    ID = zeout,
                    Email = dto.Email,
                    Password = HashPassword(dto.Password),
                    FirstName = Clean(dto.FirstName)!,
                    LastName = Clean(dto.LastName)!,
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

                _logger.LogInformation("Register | Created ID={ID} Email={Email} Inst={Inst} | TraceId={TraceId}",
                    entity.ID, entity.Email, entity.InstitutionId, HttpContext.TraceIdentifier);

                return Created($"/registration/{entity.ID}", new { id = entity.ID, message = "נרשמת בהצלחה" });
            }
            catch (DbUpdateException ex)
            {
                var raw = ex.InnerException?.Message ?? ex.Message;
                _logger.LogWarning(ex,
                    "Registration save failed. Inst={Inst} ID={ID} Email={Email} TraceId={TraceId} | RawMessage={Raw}",
                    institutionId, dto.ID, dto.Email, HttpContext.TraceIdentifier, raw);

                var pd = new ProblemDetails { Status = 400, Title = "שגיאה בשמירת הנתונים" };
                var upper = raw.ToUpperInvariant();

                if (upper.Contains("FOREIGN KEY"))
                {
                    pd.Title = "מוסד לא תקין (InstitutionId)";
                }
                else if (upper.Contains("UNIQUE") || upper.Contains("IX_") || upper.Contains("PRIMARY KEY"))
                {
                    pd.Status = 409;
                    pd.Title = "האימייל או הת״ז כבר קיימים";
                }
                else if (upper.Contains("CANNOT INSERT THE VALUE NULL"))
                {
                    pd.Title = "שדה חובה חסר";
                }
                else if (upper.Contains("STRING OR BINARY DATA WOULD BE TRUNCATED"))
                {
                    pd.Title = "ערך חורג מאורך העמודה";
                }

                return StatusCode(pd.Status ?? 400, pd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Register | Unexpected error | Email={Email} ID={ID} Inst={Inst} | TraceId={TraceId}",
                    dto.Email, dto.ID, institutionId, HttpContext.TraceIdentifier);

                return StatusCode(500, new ProblemDetails
                {
                    Title = "שגיאה בלתי צפויה בהרשמה",
                    Status = 500,
                    Detail = ex.Message
                });
            }
        }

        // =========================
        //  PUT /update-personal
        // =========================
        [HttpPut("update-personal")]
        [Authorize]
        public async Task<IActionResult> UpdatePersonalDetails([FromBody] RegistrationUpdateDto dto)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                _logger.LogWarning("UpdatePersonalDetails | Unauthorized (no user) | TraceId={TraceId}", HttpContext.TraceIdentifier);
                return Unauthorized("משתמש לא מאומת");
            }

            var instId = ResolveInstitutionIdSafe();
            _logger.LogInformation("UpdatePersonalDetails | User={UserId} Inst={Inst} | TraceId={TraceId}",
                user.ID, instId, HttpContext.TraceIdentifier);

            try
            {
                var ok = await _registrationService.UpdatePartialAsync(
                    user.ID, dto, instId, user.ID);

                _logger.LogInformation("UpdatePersonalDetails | Result={Ok} | User={UserId} Inst={Inst} | TraceId={TraceId}",
                    ok, user.ID, instId, HttpContext.TraceIdentifier);

                if (!ok) return Ok("אין שינוי לשמירה");
                return Ok("הפרטים האישיים עודכנו בהצלחה");
            }
            catch (EmailAlreadyExistsException ex)
            {
                _logger.LogWarning(ex, "UpdatePersonalDetails | Email exists | User={UserId} Inst={Inst} | TraceId={TraceId}",
                    user.ID, instId, HttpContext.TraceIdentifier);
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "UpdatePersonalDetails | DB conflict | User={UserId} Inst={Inst} | TraceId={TraceId}",
                    user.ID, instId, HttpContext.TraceIdentifier);
                return Conflict(new { message = "האימייל כבר קיים במוסד." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePersonalDetails | Unexpected | User={UserId} Inst={Inst} | TraceId={TraceId}",
                    user.ID, instId, HttpContext.TraceIdentifier);

                return StatusCode(500, new ProblemDetails
                {
                    Title = "שגיאה בלתי צפויה בעדכון פרטים אישיים",
                    Status = 500,
                    Detail = ex.Message
                });
            }
        }

        // =========================
        //  PUT /update-bank
        // =========================
        [HttpPut("update-bank")]
        [Authorize]
        public async Task<IActionResult> UpdateBankDetails([FromBody] BankAccountUpdateDto dto)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                _logger.LogWarning("UpdateBankDetails | Unauthorized (no user) | TraceId={TraceId}", HttpContext.TraceIdentifier);
                return Unauthorized("משתמש לא מאומת");
            }

            int inst = ResolveInstitutionIdSafe();
            _logger.LogInformation("UpdateBankDetails | Start | User={UserId} Inst={Inst} | TraceId={TraceId}",
                user.ID, inst, HttpContext.TraceIdentifier);

            try
            {
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

                _logger.LogInformation("UpdateBankDetails | Success | User={UserId} Inst={Inst} | TraceId={TraceId}",
                    user.ID, inst, HttpContext.TraceIdentifier);

                return Ok("פרטי החשבון עודכנו בהצלחה");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateBankDetails | Unexpected | User={UserId} Inst={Inst} | TraceId={TraceId}",
                    user.ID, inst, HttpContext.TraceIdentifier);

                return StatusCode(500, new ProblemDetails
                {
                    Title = "שגיאה בלתי צפויה בעדכון פרטי חשבון",
                    Status = 500,
                    Detail = ex.Message
                });
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}