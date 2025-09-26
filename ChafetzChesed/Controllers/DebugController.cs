using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChafetzChesed.Controllers
{
    [ApiController]
    [Route("debug")]
    public class DebugController : ControllerBase
    {
        [HttpGet("test-json")]
        public IActionResult TestJson()
        {
            var changes = new[]
            {
                new { field = "PersonalStatus", old = "רווק/ה", @new = "נשוי/ה" },
                new { field = "HouseNumber", old = "90", @new = "909" }
            };

            var json = JsonSerializer.Serialize(changes, BLL.Services.AuditLogger.JsonOpts);
            return Content(json, "application/json; charset=utf-8");
        }
    }
}
