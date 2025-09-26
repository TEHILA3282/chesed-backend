using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChafetzChesed.BLL;

using ChafetzChesed.BLL.Interfaces;

[Authorize]
[ApiController]
[Route("[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _svc;
    public SearchController(ISearchService svc) => _svc = svc;

    [HttpGet("suggest")]
    public async Task<ActionResult<IEnumerable<SearchResultDto>>> Suggest([FromQuery] string q, [FromQuery] int take = 10)
    {
        var user = HttpContext.Items["User"] as Registration;
        if (string.IsNullOrWhiteSpace(q) || user == null) return Ok(Array.Empty<SearchResultDto>());
        return Ok(await _svc.SuggestAsync(q, user.InstitutionId, take));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search([FromQuery] string q, [FromQuery] int take = 50)
    {
        var user = HttpContext.Items["User"] as Registration;
        if (string.IsNullOrWhiteSpace(q) || user == null) return Ok(Array.Empty<SearchResultDto>());
        return Ok(await _svc.SearchAsync(q, user.InstitutionId, take));
    }
}
