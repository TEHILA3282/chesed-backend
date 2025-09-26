using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using ChafetzChesed.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using ChafetzChesed.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChafetzChesed.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly IExternalFormService _externalFormService;
        private readonly IExternalUserSyncService _externalUserSyncService;
        private readonly IEmailService _email;
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _log;

        public AdminController(
            IRegistrationService registrationService,
            IExternalFormService externalFormService,
            IExternalUserSyncService externalUserSyncService,
            IEmailService email,
            AppDbContext context,
            ILogger<AdminController> log
        )
        {
            _registrationService = registrationService;
            _externalFormService = externalFormService;
            _externalUserSyncService = externalUserSyncService;
            _email = email;
            _context = context;
            _log = log;
        }

        private Registration? GetCurrentUser()
        {
            return HttpContext.Items["User"] as Registration;
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Registration>>> GetPendingAsync()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized("משתמש לא מאומת");
            var pending = await _registrationService.GetPendingAsync(user.InstitutionId);
            return Ok(pending);
        }

    

        [HttpPost("sync-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncUsersAsync()
        {
            try
            {
                var updatedCount = await _externalUserSyncService.SyncAsync();
                return Ok(new { message = "משתמשים עודכנו בהצלחה" });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "שגיאה בסנכרון המשתמשים");
                return StatusCode(500, new { message = "אירעה שגיאה בסנכרון המשתמשים" });
            }
        }

        [HttpGet("approved")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Registration>>> GetApprovedAsync()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized("משתמש לא מאומת");
            var approved = await _registrationService.GetByStatusAsync(user.InstitutionId, "מאושר");
            return Ok(approved);
        }

        [HttpGet("rejected")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Registration>>> GetRejectedAsync()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized("משתמש לא מאומת");
            var rejected = await _registrationService.GetByStatusAsync(user.InstitutionId, "נדחה");
            return Ok(rejected);
        }

        [HttpPost("update-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatusAsync([FromBody] UpdateStatusDto request)
        {
            // נטען את המשתמש
            var reg = await _context.Registrations
                .Include(r => r.Institution)
                .FirstOrDefaultAsync(r => r.ID == request.RegistrationId);

            if (reg == null)
                return NotFound("משתמש לא נמצא");

            var oldStatus = reg.RegistrationStatus;

            reg.RegistrationStatus = request.NewStatus;
            reg.StatusUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var becameApproved = oldStatus != "מאושר" && request.NewStatus == "מאושר";
            if (becameApproved)
            {
                try
                {
                    var gmachName = reg.Institution?.Name ?? "הגמ\"ח";
                    var subject = $"{gmachName} — כניסתך למערכת אושרה";
                    var body = BuildApprovalEmailBody(reg, gmachName);

                    await _email.SendAsync(reg.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "כשל בשליחת מייל אישור למשתמש {Id}", request.RegistrationId);
                }
            }

            return Ok(new { message = "הסטטוס עודכן בהצלחה" });
        }

        private static string BuildApprovalEmailBody(Registration reg, string gmachName)
        {
            var firstName = string.IsNullOrWhiteSpace(reg.FirstName) ? "שלום" : reg.FirstName;

            return $@"
            <div dir='rtl' style='font-family:Heebo,Arial,sans-serif;font-size:16px;'>
              <p>{firstName} יקר/ה,</p>
              <p>שמחים לבשר כי בקשתך לאישור כניסה ל<strong>{gmachName}</strong> אושרה בהצלחה.</p>
              <p>כעת ניתן להתחבר למערכת באמצעות הפרטים שלך.</p>
              <p style='margin-top:12px'>בכל שאלה נשמח לעזור.</p>
              <p style='margin-top:24px'>בברכה,<br/>{gmachName}</p>
            </div>";
        }


        //[HttpGet("test-local")]
        //public IActionResult TestLocal()
        //{
        //    var filePath = @"C:\Users\משתמש\Downloads\Examples.xlsx";
        //    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //    using var workbook = new XLWorkbook(stream);
        //    var worksheet = workbook.Worksheets.First();
        //    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        //    var actions = new List<AccountAction>();

        //    foreach (var row in rows)
        //    {
        //        try
        //        {
        //            var action = new AccountAction
        //            {
        //                InstitutionId = int.Parse(row.Cell(2).GetString().Trim()), 
        //                Zeout = row.Cell(3).GetString().Trim(),                    
        //                Seder = int.Parse(row.Cell(4).GetString().Trim()),        
        //                Perut = row.Cell(5).GetString().Trim(),                    
        //                Important = int.Parse(row.Cell(6).GetString().Trim()),     
        //                CreatedAt = DateTime.Now
        //            };

        //            actions.Add(action);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"❌ שגיאה בשורה: {ex.Message}");
        //        }
        //    }

        //    _context.AccountActions.AddRange(actions);
        //    _context.SaveChanges();

        //    return Ok($"✅ Loaded {actions.Count} rows from local file.");
        //}


        //[HttpGet("load-messages-test")]
        //public IActionResult LoadMessagesTest()
        //{
        //    var filePath = @"C:\Users\משתמש\Downloads\Examples.xlsx";
        //    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //    using var workbook = new XLWorkbook(stream);
        //    var worksheet = workbook.Worksheets.First();
        //    var rows = worksheet.RangeUsed().RowsUsed().Skip(1).ToList();

        //    Console.WriteLine($"🟡 נקראו {rows.Count} שורות מהקובץ");

        //    var messages = new List<Message>();
        //    var failedRows = new List<string>();
        //    int rowNumber = 2;

        //    foreach (var row in rows)
        //    {
        //        try
        //        {
        //            var institutionId = int.TryParse(row.Cell(2).GetValue<string>()?.Trim(), out var instId) ? instId : 0;
        //            var zeoutRaw = row.Cell(3).GetValue<string>()?.Trim() ?? "";
        //            var seder = int.TryParse(row.Cell(4).GetValue<string>()?.Trim(), out var sed) ? sed : 0;
        //            var perut = row.Cell(5).GetValue<string>()?.Trim() ?? "";
        //            var important = int.TryParse(row.Cell(6).GetValue<string>()?.Trim(), out var imp) ? imp : 0;

        //            var zeout = (zeoutRaw == "0" || string.IsNullOrWhiteSpace(zeoutRaw))
        //                ? "0"
        //                : zeoutRaw.TrimStart('0').PadLeft(9, '0');

        //            Console.WriteLine($"✅ שורה {rowNumber}: institution={institutionId}, seder={seder}, zeoutRaw='{zeoutRaw}', → zeout='{zeout}', perut='{perut}', important={important}");

        //            var message = new Message
        //            {
        //                InstitutionId = institutionId,
        //                Zeout = zeout,
        //                Seder = seder,
        //                Perut = perut,
        //                Important = important,
        //                CreatedAt = DateTime.Now
        //            };

        //            messages.Add(message);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"❌ שגיאה בשורה {rowNumber}: {ex.Message}");
        //            failedRows.Add($"שורה {rowNumber}");
        //        }

        //        rowNumber++;
        //    }

        //    _context.Messages.AddRange(messages);
        //    _context.SaveChanges();

        //    Console.WriteLine($"✅ נשמרו {messages.Count} הודעות");
        //    Console.WriteLine($"⚠️ שורות שנכשלו: {failedRows.Count}");

        //    return Ok(new
        //    {
        //        successCount = messages.Count,
        //        failCount = failedRows.Count,
        //        failed = failedRows
        //    });
        //}

    }
}
