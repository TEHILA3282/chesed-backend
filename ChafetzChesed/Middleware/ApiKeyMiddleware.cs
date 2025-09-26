using Microsoft.Extensions.Options;

namespace ChafetzChesed.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly List<ChafetzChesed.Common.ApiClient> _clients;

    public const string HeaderName = "X-Api-Key";
    public const string QueryName = "apiKey";

    private static readonly PathString[] ProtectedPrefixes =
    [
        new PathString("/api/sync"),
        new PathString("/api/ExternalData"),   
    ];

    public ApiKeyMiddleware(RequestDelegate next, IOptions<List<ChafetzChesed.Common.ApiClient>> clients)
    {
        _next = next;
        _clients = clients.Value ?? [];
    }

    public async Task Invoke(HttpContext ctx, IConfiguration cfg, IHostEnvironment env)
    {
        bool requiresApiKey = ProtectedPrefixes.Any(p => ctx.Request.Path.StartsWithSegments(p));
        if (!requiresApiKey)
        {
            await _next(ctx);
            return;
        }

        string? key = null;
        if (ctx.Request.Headers.TryGetValue(HeaderName, out var hv) && !string.IsNullOrWhiteSpace(hv))
            key = hv.ToString();
        else if (ctx.Request.Query.TryGetValue(QueryName, out var qv) && !string.IsNullOrWhiteSpace(qv))
            key = qv.ToString();

        if (string.IsNullOrWhiteSpace(key))
        {
            if (env.IsDevelopment())
            {
                var defId = cfg.GetValue<int?>("DefaultInstitutionId") ?? 0;
                if (defId > 0)
                {
                    ctx.Items["InstitutionId"] = defId;
                    await _next(ctx);
                    return;
                }
            }

            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("Missing X-Api-Key");
            return;
        }

        var client = _clients.FirstOrDefault(c => c.ApiKey == key);
        if (client is null)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("Invalid API key");
            return;
        }

        ctx.Items["InstitutionId"] = client.InstitutionId;

        await _next(ctx);
    }
}
