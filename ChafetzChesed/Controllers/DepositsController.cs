using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChafetzChesed.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DepositsController : ControllerBase
    {
        private readonly IDepositService _service;
        private readonly ILogger<DepositsController> _logger;
        private readonly AppDbContext _context; // נוסיף כדי לקרוא DepositTypes ישירות

        private static readonly HashSet<string> AllowedPaymentMethods = new(new[]
        {
            "גביה מאשראי אחר",
            "גביה באשראי המופיע בקופת הגמ\"ח",
            "חתימה על הוראת קבע חדשה",
            "הו\"ק קיימת בקופת הגמ\"ח"
        });

        private static readonly Dictionary<string, string> PaymentMethodMap = new()
        {
            ["credit_other"] = "גביה מאשראי אחר",
            ["credit_existing"] = "גביה באשראי המופיע בקופת הגמ\"ח",
            ["new"] = "חתימה על הוראת קבע חדשה",
            ["existing"] = "הו\"ק קיימת בקופת הגמ\"ח"
        };

        public DepositsController(
            IDepositService service,
            ILogger<DepositsController> logger,
            AppDbContext context)
        {
            _service = service;
            _logger = logger;
            _context = context;
        }

        private Registration? GetLoggedInUser() => HttpContext.Items["User"] as Registration;

        private int GetInstitutionId()
        {
            if (HttpContext.Items.TryGetValue("InstitutionId", out var v) && v is int id && id > 0)
                return id;

            var u = GetLoggedInUser();
            if (u?.InstitutionId > 0) return u.InstitutionId;

            throw new InvalidOperationException("Institution not resolved");
        }

        // ==========================
        // ======  GET ALL  =========
        // ==========================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            int inst = GetInstitutionId();
            var items = await _service.GetAllAsync(inst);
            return Ok(items);
        }

        // ==========================
        // ======  GET BY ID  =======
        // ==========================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            int inst = GetInstitutionId();
            var item = await _service.GetByIdAsync(id, inst);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // ==========================
        // ======  CREATE  ==========
        // ==========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepositDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = GetLoggedInUser();
            if (user == null) return Unauthorized("משתמש לא מחובר");

            if (!dto.Amount.HasValue || dto.Amount <= 0)
                return BadRequest("סכום ההפקדה חייב להיות גדול מ-0");
            if (dto.DepositTypeId <= 0)
                return BadRequest("חובה לבחור סוג הפקדה");

            // 🔹 שלב חדש: תרגום DepositTypeId → DepositTypes.Name
            var typeName = await _context.DepositTypes
                .Where(t => t.ID == dto.DepositTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            if (typeName == null)
                return BadRequest($"לא נמצא סוג הפקדה עם מזהה {dto.DepositTypeId}");

            // ===== מיפוי שיטת תשלום =====
            string? dbPaymentMethod = null;
            if (!string.IsNullOrWhiteSpace(dto.PaymentMethod))
            {
                if (AllowedPaymentMethods.Contains(dto.PaymentMethod))
                    dbPaymentMethod = dto.PaymentMethod;
                else if (PaymentMethodMap.TryGetValue(dto.PaymentMethod, out var mapped))
                    dbPaymentMethod = mapped;
                else
                    return BadRequest($"אופן התשלום אינו חוקי: {dto.PaymentMethod}");
            }

            if (dto.IsDirectDeposit && string.IsNullOrWhiteSpace(dbPaymentMethod))
                return BadRequest("בהפקדה אוטומטית חובה לבחור אופן תשלום.");

            int inst = GetInstitutionId();

            DateTime ParseDate(string? s, DateTime fallback)
            {
                if (string.IsNullOrWhiteSpace(s)) return fallback;

                string[] formats =
                {
                    "yyyy-MM-ddTHH:mm:ss.fffK",
                    "yyyy-MM-ddTHH:mm:ss",
                    "yyyy-MM-dd"
                };

                if (DateTime.TryParseExact(s, formats,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeLocal,
                        out var dt))
                {
                    return dt;
                }
                if (DateTime.TryParse(s, out dt))
                    return dt;

                return fallback;
            }

            var deposit = new Deposit
            {
                ClientID = user.ID.ToString(),
                DepositTypeId = typeName,   // 🔹 נשמר השם, לא המספר
                Amount = dto.Amount.Value,
                PurposeDetails = dto.PurposeDetails,
                IsDirectDeposit = dto.IsDirectDeposit,
                DepositDate = ParseDate(dto.DepositDate, DateTime.UtcNow),
                DepositReceivedDate = dto.DepositReceivedDate != null
                    ? ParseDate(dto.DepositReceivedDate, DateTime.UtcNow)
                    : null,
                PaymentMethod = dbPaymentMethod,
                InstitutionId = inst
            };

            try
            {
                var created = await _service.AddAsync(deposit, inst);
                return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deposit.Create failed inst={Inst}", inst);
                var msg = ex is DbUpdateException dbex
                    ? (dbex.InnerException?.Message ?? dbex.Message)
                    : ex.Message;
                return Problem(title: "שמירת ההפקדה נכשלה", detail: msg,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        // ==========================
        // ======  UPDATE  ==========
        // ==========================
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Deposit deposit)
        {
            int inst = GetInstitutionId();

            if (!string.IsNullOrWhiteSpace(deposit.PaymentMethod) &&
                !AllowedPaymentMethods.Contains(deposit.PaymentMethod))
            {
                return BadRequest($"אופן התשלום חייב להיות אחד מהבאים: {string.Join(", ", AllowedPaymentMethods)}");
            }

            if (deposit.InstitutionId <= 0)
                deposit.InstitutionId = inst;

            if (deposit.InstitutionId != inst)
                return Forbid();

            var updated = await _service.UpdateAsync(deposit, inst);
            return Ok(updated);
        }

        // ==========================
        // ======  DELETE  ==========
        // ==========================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            int inst = GetInstitutionId();
            var deleted = await _service.DeleteAsync(id, inst);
            if (!deleted) return NotFound();
            return NoContent();
        }

        // ==========================
        // ===== DEBUG HELPERS ======
        // ==========================
        [HttpPost("debug-validate")]
        public IActionResult DebugValidate([FromBody] CreateDepositDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errs = ModelState
                    .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new
                {
                    message = "ModelState invalid",
                    errors = errs
                });
            }
            return Ok(new { message = "ModelState OK", dto });
        }

        [HttpPost("debug-echo")]
        public async Task<IActionResult> DebugEcho()
        {
            HttpContext.Request.EnableBuffering();
            using var sr = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
            HttpContext.Request.Body.Position = 0;
            var raw = await sr.ReadToEndAsync();
            HttpContext.Request.Body.Position = 0;
            return Ok(new
            {
                contentType = Request.ContentType,
                raw
            });
        }
    }
}
