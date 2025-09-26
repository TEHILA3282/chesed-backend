using Microsoft.AspNetCore.Mvc;
using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ChafetzChesed.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DepositTypesController : ControllerBase
    {
        private readonly IDepositTypeService _service;

        public DepositTypesController(IDepositTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<DepositType>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DepositType>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<DepositType>> Create(DepositType depositType)
        {
            var created = await _service.AddAsync(depositType);
            return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
        }

        [HttpPut]
        public async Task<ActionResult<DepositType>> Update(DepositType depositType)
        {
            var updated = await _service.UpdateAsync(depositType);
            return Ok(updated);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound();
            return NoContent();
        }
    }
}