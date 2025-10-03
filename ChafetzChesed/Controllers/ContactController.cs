using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
[Route("api/[controller]")] // מאפשר גם /api/contact
public class ContactController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ContactController> _logger;

    public ContactController(AppDbContext db, ILogger<ContactController> logger)
    {
        _db = db; _logger = logger;
    }

    private int ResolveInstitutionId()
    {
        if (HttpContext.Items.TryGetValue("InstitutionId", out var v) && v is int id && id > 0)
            return id;

        if (HttpContext.Items.TryGetValue("User", out var u) && u is Registration r && r.InstitutionId > 0)
            return r.InstitutionId;

        throw new InvalidOperationException("Institution not resolved");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactRequestCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) ||
            string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Subject) ||
            string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest("יש למלא את כל השדות");
        }

        int instId;
        try
        {
            instId = ResolveInstitutionId();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Contact.Create: failed to resolve institution");
            return BadRequest("Institution is not resolved");
        }

        var entity = new ContactRequest
        {
            InstitutionId = instId,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Subject = dto.Subject.Trim(),
            Message = dto.Message.Trim()
        };

        try
        {
            _db.ContactRequests.Add(entity);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Contact.Create DB error inst={Inst}", instId);
            return Problem(title: "שמירת הפניה נכשלה", detail: ex.InnerException?.Message ?? ex.Message, statusCode: 400);
        }

        return Ok(new { id = entity.ID });
    }
}
