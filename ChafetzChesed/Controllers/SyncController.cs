using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.BLL.Services;
using ChafetzChesed.Common;
using System.Linq;

namespace ChafetzChesed.Controllers;

[ApiController]
[Route("sync")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDeltaSyncService _delta;
    public SyncController(AppDbContext db, IDeltaSyncService delta) { _db = db; _delta = delta; }

    private int GetInstitutionId()
    {
        if (HttpContext.Items.TryGetValue("InstitutionId", out var v) && v is int id && id > 0) return id;
        throw new InvalidOperationException("Institution not resolved");
    }

 

    [HttpGet("deposits/{lastId:int}")]
    public async Task<IActionResult> GetDepositsRaw(int lastId, [FromQuery] int limit = 200)
    {
        int instId = GetInstitutionId();
        var res = await _delta.GetDeltaAsync(
            _db.Deposits.AsNoTracking(),
            instId, lastId, limit,
            d => new DepositDto(
                d.ID, d.InstitutionId, d.ClientID, d.DepositDate,
                d.DepositTypeID, d.Amount, d.PurposeDetails,
                d.IsDirectDeposit, d.DepositReceivedDate, d.PaymentMethod
            ),
            idPropertyName: "ID"
        );
        return Ok(res.Items);
    }



    [HttpGet("loans/{lastId:int}")]
    public async Task<IActionResult> GetLoansRaw(int lastId, [FromQuery] int limit = 200)
    {
        int instId = GetInstitutionId();
        var res = await _delta.GetDeltaAsync(
            _db.Loans.AsNoTracking(),
            instId, lastId, limit,
            l => new LoanDto(
                l.ID, l.InstitutionId, l.ClientID, l.Amount,
                l.InstallmentsCount, l.LoanDate, l.Purpose, l.PurposeDetails,
                l.IsDeleted, l.UpdatedAt, l.LoanTypeID
            ),
            idPropertyName: "ID"
        );
        return Ok(res.Items);
    }

 

    [HttpGet("registration/full")]
    public async Task<IActionResult> GetRegistrationFullRaw(
      [FromQuery] int page = 1,
      [FromQuery] int limit = 200,
      [FromQuery] string? status = null)
    {
        int instId = GetInstitutionId();

        var q = _db.Registrations.AsNoTracking()
            .Where(u => u.InstitutionId == instId);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(u => u.RegistrationStatus == status);

        q = q.OrderBy(u => u.ID);

        var list = await q.Skip((page - 1) * limit).Take(limit).ToListAsync();

        var items = list.Select(u => new RegistrationSyncDto(
            u.ID,
            u.InstitutionId,
            u.FirstName,
            u.LastName,
            u.Email,
            u.PhoneNumber,
            u.RegistrationStatus
        ));

        return Ok(items);
    }


    [HttpGet("bank-accounts/{lastId:int}")]
    public async Task<IActionResult> GetBankAccountsRaw(int lastId, [FromQuery] int limit = 200)
    {
        int instId = GetInstitutionId();

        var q = _db.BankAccounts
            .AsNoTracking()
            .Where(b =>
                b.Id > lastId &&
                _db.Registrations.Any(r => r.ID == b.RegistrationId && r.InstitutionId == instId))
            .OrderBy(b => b.Id)
            .Take(Math.Clamp(limit, 1, 1000));

        var list = await q.ToListAsync();

        var items = list.Select(b => new BankAccountDto(
            b.Id,
            instId,
            b.RegistrationId,
            b.BankNumber,
            b.BranchNumber,
            b.AccountNumber,
            b.AccountOwnerName,
            b.HasDirectDebit
        )).ToList();

        return Ok(items);
    }


    [HttpGet("deposit-withdraw-requests/{lastId:int}")]
    public async Task<IActionResult> GetDepositWithdrawRequestsRaw(int lastId, [FromQuery] int limit = 200)
    {
        int instId = GetInstitutionId();
        var res = await _delta.GetDeltaAsync(
            _db.DepositWithdrawRequests.AsNoTracking(),
            instId, lastId, limit,
            r => new DepositWithdrawRequestDto(
                r.ID,
                r.InstitutionId,
                r.ClientID,
                r.Amount,
                r.CreatedAt,
                r.Status
            ),
            idPropertyName: "ID"
        );
        return Ok(res.Items);
    }

    [HttpGet("freeze-requests/{lastId:int}")]
    public async Task<IActionResult> GetFreezeRequestsRaw(int lastId, [FromQuery] int limit = 200)
    {
        int instId = GetInstitutionId();
        var res = await _delta.GetDeltaAsync(
            _db.FreezeRequests.AsNoTracking(),
            instId, lastId, limit,
            f => new FreezeRequestDto(
                f.ID, f.InstitutionId, f.ClientID, f.RequestType, f.Reason, f.Acknowledged, f.CreatedAt
            ),
            idPropertyName: "ID"
        );
        return Ok(res.Items);
    }


    [HttpGet("contact-requests/{lastId:int}")]
    public async Task<IActionResult> GetContactRequestsRaw(int lastId, [FromQuery] int limit = 200)
    {
        int instId = GetInstitutionId();

        var res = await _delta.GetDeltaAsync(
            _db.ContactRequests.AsNoTracking(),
            instId, lastId, limit,
            c => new ContactRequestSyncDto(
                c.ID,
                c.InstitutionId,
                c.FirstName,
                c.LastName,
                c.Email,
                c.Subject,
                c.Message,
                c.CreatedAt
            ),
            idPropertyName: "ID"
        );

        return Ok(res.Items);
    }


    [HttpGet("audit-logs/{lastId:int}")]
    public async Task<IActionResult> GetAuditLogsRaw(
        int lastId,
        [FromQuery] int limit = 200,
        [FromQuery] string? entity = null,
        [FromQuery] string? entityId = null)
    {
        int instId = GetInstitutionId();

        var query = _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.InstitutionId == instId && a.ID > lastId);

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(a => a.EntityId == entityId);

        var res = await _delta.GetDeltaAsync(
            query.OrderBy(a => a.ID),
            instId,
            lastId,
            Math.Clamp(limit, 1, 1000),
            a => new AuditLogDto(
                a.ID,
                a.InstitutionId,
                a.Entity,
                a.EntityId,
                a.ChangedAt,
                a.ChangedBy,
                a.ChangesJson
            ),
            idPropertyName: "ID"
        );

        return Ok(res.Items);
    }



    [HttpGet("loan-guarantors/{lastId:int}")]
    public async Task<IActionResult> GetLoanGuarantorsRaw(int lastId, [FromQuery] int limit = 200)
    {
        int instId = GetInstitutionId();

        var q = _db.LoanGuarantors
            .AsNoTracking()
            .Where(g =>
                g.Id > lastId &&
                _db.Loans.Any(l => l.ID == g.LoanId && l.InstitutionId == instId))
            .OrderBy(g => g.Id)
            .Take(Math.Clamp(limit, 1, 1000));

        var list = await q.ToListAsync();

        var items = list.Select(g => new LoanGuarantorSyncDto(
            g.Id, instId, g.LoanId, g.IdNumber, g.FullName, g.Phone
        )).ToList();

        return Ok(items);
    }
}
