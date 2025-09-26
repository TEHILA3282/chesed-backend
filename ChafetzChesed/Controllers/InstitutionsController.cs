using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("[controller]")] 
public class InstitutionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public InstitutionsController(AppDbContext db) => _db = db;

    [HttpGet("{institutionId:int}/public-info")]
    public ActionResult<InstitutionPublicInfoDto> GetPublicInfoById(int institutionId)
        => ToDto(institutionId);

    [HttpGet("public-info")]
    public ActionResult<InstitutionPublicInfoDto> GetPublicInfoFromContext()
    {
        if (!HttpContext.Items.TryGetValue("InstitutionId", out var v) || v is not int id || id <= 0)
            return BadRequest("Institution is not resolved");
        return ToDto(id);
    }

    private ActionResult<InstitutionPublicInfoDto> ToDto(int id)
    {
        var inst = _db.Institutions.AsNoTracking().FirstOrDefault(x => x.InstitutionId == id);
        if (inst is null) return NotFound();

        return Ok(new InstitutionPublicInfoDto
        {
            Phone = inst.ContactPhone ?? "03-0000000",
            AvailabilityText = inst.AvailabilityText ?? "א'-ה' 09:30–10:30",
        });
    }
}
