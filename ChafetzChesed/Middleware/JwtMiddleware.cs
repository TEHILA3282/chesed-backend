using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.Common;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChafetzChesed.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JwtSettings _jwtSettings;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
                ?? throw new InvalidOperationException("Missing Jwt settings in configuration (section 'Jwt').");
        }

        public async Task Invoke(HttpContext context, IRegistrationService registrationService)
        {
            var path = context.Request.Path.Value?.ToLower();

            if (path != null && (
                path.StartsWith("/api/auth/login") ||
                path.StartsWith("/api/auth/register") ||
                path.StartsWith("/api/auth/get-user") ||
                path.StartsWith("/swagger") ||
                path.StartsWith("/favicon") ||
                path.StartsWith("/api/deposittypes") ||
                path.StartsWith("/api/loantypes") ||
                path.StartsWith("/index.html") ||
                path.StartsWith("/api/auth/forgot-password") ||
                path.StartsWith("/api/institutions/public-info") ||
                path.StartsWith("/api/registration/check-exists")
            ))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();

            if (!string.IsNullOrWhiteSpace(token))
            {
                var ok = await AttachUserToContext(context, registrationService, token);
                if (!ok) return;
            }

            await _next(context);
        }

        private async Task<bool> AttachUserToContext(HttpContext context, IRegistrationService registrationService, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                var userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
                var role = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

                // נעדיף InstitutionId מהטוקן, אבל נשלים מה-Headers/Items אם צריך
                int tokenInstitutionId = 0;
                var institutionIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "InstitutionId")?.Value;
                int.TryParse(institutionIdClaim, out tokenInstitutionId);

                var resolvedInstitutionId = ResolveInstitutionId(context, tokenInstitutionId);
                if (resolvedInstitutionId > 0)
                    context.Items["InstitutionId"] = resolvedInstitutionId;

                // ⚠️ חשוב: החתימה החדשה דורשת institutionId
                var user = await registrationService.GetByIdAsync(userId, resolvedInstitutionId);
                if (user == null || user.RegistrationStatus == "נדחה")
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("גישה נדחתה – המשתמש אינו מאושר או לא קיים");
                    return false;
                }

                var path = context.Request.Path.Value?.ToLower() ?? "";

                if (path.StartsWith("/api/admin") && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("גישה חסומה – מנהלים בלבד");
                    return false;
                }

                // התאמת מוסד מהנתיב (אם קיים) מול זה שבטוקן/נפתר
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && int.TryParse(parts[2], out int pathInstitutionId))
                {
                    if (resolvedInstitutionId > 0 && pathInstitutionId != resolvedInstitutionId)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("גישה נדחתה – מוסד לא תואם לטוקן/כותרות");
                        return false;
                    }
                }

                context.Items["User"] = user;
                return true;
            }
            catch
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("טוקן לא חוקי או פג תוקף");
                return false;
            }
        }

        private static int ResolveInstitutionId(HttpContext ctx, int fromToken)
        {
            if (fromToken > 0) return fromToken;

            // קודם Items (מידלוורים קודמים)
            if (ctx.Items.TryGetValue("InstitutionId", out var v) && v is int ok && ok > 0)
                return ok;

            // כותרת שהקליינט שם
            if (int.TryParse(ctx.Request.Headers["X-Institution-Id"].FirstOrDefault(), out var fromHeader) && fromHeader > 0)
                return fromHeader;

            return 0;
        }
    }
}
