using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
[Route("api/[controller]")] // מאפשר גם /api/institutions/...
public class InstitutionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public InstitutionsController(AppDbContext db) => _db = db;

    // GET /institutions/123/public-info  וגם  /api/institutions/123/public-info
    [HttpGet("{institutionId:int}/public-info")]
    [AllowAnonymous]
    public async Task<ActionResult<InstitutionPublicInfoDto>> GetPublicInfoById(int institutionId)
        => await ToDtoAsync(institutionId);

    // GET /institutions/public-info  וגם  /api/institutions/public-info
    // (מתבסס על InstitutionId שב־HttpContext.Items מהמיידלוור)
    [HttpGet("public-info")]
    [AllowAnonymous]
    public async Task<ActionResult<InstitutionPublicInfoDto>> GetPublicInfoFromContext()
    {
        if (!HttpContext.Items.TryGetValue("InstitutionId", out var v) || v is not int id || id <= 0)
            return BadRequest("Institution is not resolved");

        return await ToDtoAsync(id);
    }

    private async Task<ActionResult<InstitutionPublicInfoDto>> ToDtoAsync(int id)
    {
        // שימי לב לשמות העמודות/שדות: InstitutionId, ContactPhone, AvailabilityText
        var inst = await _db.Institutions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InstitutionId == id);

        if (inst is null) return NotFound();

        return Ok(new InstitutionPublicInfoDto
        {
            InstitutionId = inst.InstitutionId,                  // ← חשוב לפרונט
            Phone = inst.ContactPhone ?? "03-0000000",
            AvailabilityText = inst.AvailabilityText ?? "א׳–ה׳ 09:30–10:30"
        });
    }
}
