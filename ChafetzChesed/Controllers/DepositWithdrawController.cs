using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChafetzChesed.DAL.Entities;

[Route("deposits/withdrawals")]
[ApiController]
[Authorize]
public class DepositWithdrawController : ControllerBase
{
    private readonly IDepositWithdrawService _service;
    public DepositWithdrawController(IDepositWithdrawService service) { _service = service; }

    private Registration? GetCurrentUser() => HttpContext.Items["User"] as Registration;

    [HttpPost]
    public async Task<ActionResult<DepositWithdrawResponseDto>> Create([FromBody] CreateDepositWithdrawDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = GetCurrentUser();
        if (user == null) return Unauthorized();

        var entity = await _service.CreateAsync(
            clientId: user.ID,                 
            institutionId: user.InstitutionId,  
            amount: dto.Amount,
            text: dto.RequestText
        );

        return Created(string.Empty, new DepositWithdrawResponseDto
        {
            Id = entity.ID,
            Amount = entity.Amount,
            RequestText = entity.RequestText,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        });
    }
}
