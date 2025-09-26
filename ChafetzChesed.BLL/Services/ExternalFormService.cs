using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common.Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ChafetzChesed.BLL.Services
{
    public class ExternalFormService : IExternalFormService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExternalFormService> _logger;

        public ExternalFormService(HttpClient http, ILogger<ExternalFormService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<ExternalForm>> GetFormsAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<ExternalForm>>("https://example.com/api/forms");
                return result ?? new List<ExternalForm>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה בקבלת טפסים חיצוניים");
                return new List<ExternalForm>();
            }
        }
    }
}
