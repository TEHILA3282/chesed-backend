using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Entities;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChafetzChesed.Controllers
{
    [ApiController]
    [Route("freeze-requests")]
    public class FreezeRequestsController : ControllerBase
    {
        private readonly IFreezeRequestService _svc;
        public FreezeRequestsController(IFreezeRequestService svc) => _svc = svc;

        private Registration? CurrentUser => HttpContext.Items["User"] as Registration;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFreezeRequestDto dto)
        {
            var user = CurrentUser;
            if (user is null) return Unauthorized();

            var claim = User.FindFirst("InstitutionId")?.Value;
            var institutionId = int.TryParse(claim, out var id) ? id : user.InstitutionId;

            var created = await _svc.CreateAsync(dto, user, institutionId);
            return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await Task.FromResult(id));
        }
    }
}
