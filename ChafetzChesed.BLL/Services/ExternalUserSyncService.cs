using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.DAL.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace ChafetzChesed.BLL.Services
{
    public class ExternalUserSyncService : IExternalUserSyncService
    {
        private readonly HttpClient _http;
        private readonly IRegistrationService _registrationService;
        private readonly ILogger<ExternalUserSyncService> _logger;

        public ExternalUserSyncService(HttpClient http, IRegistrationService registrationService, ILogger<ExternalUserSyncService> logger)
        {
            _http = http;
            _registrationService = registrationService;
            _logger = logger;
        }

        public async Task<int> SyncAsync()
        {
            try
            {
                var users = await _http.GetFromJsonAsync<List<Registration>>("https://example.com/api/users");
                int updated = 0;

                if (users != null)
                {
                    foreach (var user in users)
                    {
                        bool success = await _registrationService.UpdateAsync(user);
                        if (success) updated++;
                    }
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "שגיאה בסנכרון משתמשים חיצוניים");
                return 0;
            }
        }
    }
}
