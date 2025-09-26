using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.BLL.Services;
using ChafetzChesed.DAL.Entities;
using ChafetzChesed.Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ChafetzChesed.DAL.Data;

namespace ChafetzChesed.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly JwtService _jwtService;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;


        public AuthController(IRegistrationService registrationService, JwtService jwtService, AppDbContext context, IEmailService emailService)
        {
            _registrationService = registrationService;
            _jwtService = jwtService;
            _context = context;
            _emailService = emailService;
        }

        private Registration? GetLoggedInUser() => HttpContext.Items["User"] as Registration;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!HttpContext.Items.TryGetValue("InstitutionId", out var instIdObj) || instIdObj is not int institutionId || institutionId <= 0)
                return BadRequest(new { message = "Institution is not resolved from subdomain" });

            string hashedPassword = HashPassword(request.Password ?? string.Empty);

            var idRaw = (request.Identifier ?? string.Empty).Trim();
            var idNoLeadingZeros = idRaw.TrimStart('0');
            var idWithPadding = idNoLeadingZeros.PadLeft(9, '0');

            var users = await _registrationService.GetAllAsync();

            var user = users.FirstOrDefault(u =>
                (u.Email.ToLower() == idRaw.ToLower()
                 || u.ID == idRaw
                 || u.ID == idNoLeadingZeros
                 || u.ID == idWithPadding)
                && u.Password == hashedPassword
                && u.InstitutionId == institutionId 
            );

            if (user == null)
            {
                return Unauthorized(new { message = "אימייל/ת״ז/סיסמה שגויים או שאינם שייכים למוסד הנוכחי" });
            }

            var role = string.IsNullOrEmpty(user.Role) ? "User" : user.Role;

            var token = _jwtService.GenerateToken(user.ID.ToString(), user.Email, user.InstitutionId, role);

            return Ok(new
            {
                message = "התחברת בהצלחה",
                token,
                user = new
                {
                    user.FirstName,
                    user.LastName,
                    user.ID,
                    user.Email,
                    user.InstitutionId,
                    user.Role,
                    user.RegistrationStatus
                }
            });
        }

        [HttpGet("get-bank-details")]
        [Authorize]
        public async Task<IActionResult> GetBankDetails()
        {
            var user = GetLoggedInUser();
            if (user == null)
                return Unauthorized("משתמש לא מאומת");

            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.RegistrationId == user.ID);

            if (bankAccount == null)
                return Ok(null);

            var dto = new BankAccountUpdateDto
            {
                BankNumber = bankAccount.BankNumber,
                BranchNumber = bankAccount.BranchNumber,
                AccountNumber = bankAccount.AccountNumber,
                AccountOwnerName = bankAccount.AccountOwnerName,
                HasDirectDebit = bankAccount.HasDirectDebit
            };

            return Ok(dto);
        }

        [HttpGet("get-current-user")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var user = GetLoggedInUser();
            if (user == null)
                return Unauthorized("משתמש לא מאומת");

            return Ok(user);
        }

        [HttpGet("get-user/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _registrationService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "משתמש לא נמצא" });

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.ID,
                user.Email,
                user.InstitutionId,
                user.Role,
                user.RegistrationStatus
            });
        }
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!HttpContext.Items.TryGetValue("InstitutionId", out var instIdObj) || instIdObj is not int institutionId || institutionId <= 0)
                return BadRequest(new { message = "Institution is not resolved from subdomain" });

            var identifier = (request?.Identifier ?? "").Trim();
            if (string.IsNullOrEmpty(identifier))
                return BadRequest(new { message = "נא להזין אימייל או תעודת זהות" });

            var idNoZeros = identifier.TrimStart('0');
            var idPad = idNoZeros.PadLeft(9, '0');

            var user = await _context.Registrations
                .Where(u => u.InstitutionId == institutionId)
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == identifier.ToLower() || u.ID == identifier || u.ID == idNoZeros || u.ID == idPad);

            if (user == null) return NotFound(new { message = "משתמש לא נמצא במוסד הנוכחי" });

            var tempPwd = GenerateStrongTempPassword(12);
            user.Password = HashPassword(tempPwd);
            await _context.SaveChangesAsync();

            var subject = "איפוס סיסמה – גמ\"ח";
            var body = $@"<p>שלום {user.FirstName} {user.LastName},</p>
<p>הסיסמה הזמנית שלך היא: <b>{tempPwd}</b></p>
<p>נא להתחבר ולשנות סיסמה מיד.</p>";

            await _emailService.SendAsync(user.Email, subject, body);
            return Ok(new { message = "נשלח אליך מייל עם סיסמה זמנית" });
        }

        private static string GenerateStrongTempPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*";
            var bytes = RandomNumberGenerator.GetBytes(length);
            var arr = new char[length];
            for (int i = 0; i < length; i++) arr[i] = chars[bytes[i] % chars.Length];
            return new string(arr);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
