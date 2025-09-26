using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class ExternalDataController : ControllerBase
{
    private readonly string _logPath = "ReceivedJson.txt";

    private static readonly JsonSerializerOptions PrettyUtf8 = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [HttpPost("receive")]
    public IActionResult ReceiveJson([FromBody] JsonElement payload)
    {
        string rawJson = payload.GetRawText();

        string prettyJson = JsonSerializer.Serialize(payload, PrettyUtf8);

        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        using (var sw = new StreamWriter(_logPath, append: true, encoding: utf8Bom))
        {
            sw.WriteLine(prettyJson);
            sw.WriteLine();
        }

        return new JsonResult(new
        {
            status = "received",
            raw = rawJson,
            received = payload
        });
    }
}
