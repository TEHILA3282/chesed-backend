using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.DTOs;
using ChafetzChesed.DAL.Entities;

namespace ChafetzChesed.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _service;
        public LoansController(ILoanService service) { _service = service; }

        private Registration? GetCurrentUser() => HttpContext.Items["User"] as Registration;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLoanDto dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized("משתמש לא מאומת");
            if (dto == null) return BadRequest("בקשה ריקה");
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var loan = await _service.CreateAsync(dto, user);
            return CreatedAtAction(nameof(GetById), new { id = loan.ID }, loan);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Loan loan)
        {
            var updated = await _service.UpdateAsync(loan);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
