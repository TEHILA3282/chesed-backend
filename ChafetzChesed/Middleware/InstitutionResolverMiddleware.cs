using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using ChafetzChesed.DAL.Data;

public class InstitutionResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<InstitutionResolverMiddleware> _logger;
    private readonly IConfiguration _config;

    private const int DefaultInstitutionId = 1;
    private static readonly Regex SlugRegex =
        new(@"^[a-z0-9-]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public InstitutionResolverMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<InstitutionResolverMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _config = config;
    }

    public async Task Invoke(HttpContext context, AppDbContext db)
    {
        // אם כבר נקבע InstitutionId ע"י מידלוור אחר – לצאת
        if (context.Items.TryGetValue("InstitutionId", out var v) && v is int okId && okId > 0)
        {
            await _next(context);
            return;
        }

        try
        {
            // ✅ קודם לנסות לזהות מתוך PathBase (כאשר מאוחסן תחת תת־נתיב /<slug>)
            var pb = context.Request.PathBase.Value;
            if (!string.IsNullOrEmpty(pb))
            {
                var baseSlug = pb.Trim('/')
                                 .Split('/', StringSplitOptions.RemoveEmptyEntries)
                                 .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(baseSlug))
                {
                    var idFromBase = await FindBySlugAsync(db, baseSlug.ToLowerInvariant());
                    if (idFromBase is int okBase && okBase > 0)
                    {
                        context.Items["InstitutionId"] = okBase;
                        await _next(context);
                        return;
                    }
                }
            }

            // המשך לוגיקה רגילה: מפתחות/כותרות/Referer/Path מלא
            int? instId = await ResolveInstitutionIdAsync(context, db);
            if (instId is int ok && ok > 0)
            {
                context.Items["InstitutionId"] = ok;
            }
            else
            {
                _logger.LogWarning("InstitutionId could not be resolved for {PathBase}{Path} | Host={Host}",
                    context.Request.PathBase, context.Request.Path, context.Request.Host);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed resolving InstitutionId");
        }

        await _next(context);
    }

    private async Task<int?> ResolveInstitutionIdAsync(HttpContext ctx, AppDbContext db)
    {
        // apiKey דרך query
        if (ctx.Request.Query.TryGetValue("apiKey", out var apiKeyQuery))
        {
            var apiKey = apiKeyQuery.ToString().Trim();
            var id = ResolveFromApiKey(apiKey);
            if (id.HasValue) return id;
        }

        // apiKey דרך header
        if (ctx.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
        {
            var apiKey = apiKeyHeader.ToString().Trim();
            var id = ResolveFromApiKey(apiKey);
            if (id.HasValue) return id;
        }

        // מזהה מפורש בכותרת
        if (ctx.Request.Headers.TryGetValue("X-Institution-Id", out StringValues idHeader) &&
            int.TryParse(idHeader.ToString(), out var fromHeaderId) && fromHeaderId > 0)
            return fromHeaderId;

        // slug מפורש בכותרת
        if (ctx.Request.Headers.TryGetValue("X-Institution-Slug", out var slugHeader))
        {
            var slug = slugHeader.ToString().Trim().ToLowerInvariant();
            var found = await FindBySlugAsync(db, slug);
            if (found is int okId) return okId;
        }

        // Referer
        if (ctx.Request.Headers.TryGetValue("Referer", out var referer) &&
            Uri.TryCreate(referer.ToString(), UriKind.Absolute, out var refUri))
        {
            var fromRef = await ResolveFromPathAsync(db, ctx.Request.PathBase, new PathString(refUri.AbsolutePath));
            if (fromRef is int okRef) return okRef;
        }

        // ✅ PathBase + Path יחד
        var fromPath = await ResolveFromPathAsync(db, ctx.Request.PathBase, ctx.Request.Path);
        if (fromPath is int okPath) return okPath;

        // (אופציונלי) ברירת מחדל בסביבה מקומית בלבד
        var host = ctx.Request.Host.Host?.ToLowerInvariant() ?? "";
        if (host is "localhost" or "127.0.0.1") return DefaultInstitutionId;

        return null;
    }

    private int? ResolveFromApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var clients = _config.GetSection("ApiClients").Get<List<ApiClientConfig>>() ?? new();
        var client = clients.FirstOrDefault(c => c.ApiKey == apiKey);
        return client?.InstitutionId;
    }

    // מקבלת גם PathBase וגם Path, ומרכיבה מהם נתיב מלא
    private async Task<int?> ResolveFromPathAsync(AppDbContext db, PathString pathBase, PathString path)
    {
        var full = $"{pathBase}{path}";                // למשל: "/chaiad/api/auth/login"
        var segments = full.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return null;

        // אם מתחיל ב-"api" אז ה-segment הראשון הוא ה-slug (כשמארחים "/<slug>/api/...")
        var candidate = segments[0].ToLowerInvariant();
        if (string.Equals(candidate, "api", StringComparison.OrdinalIgnoreCase))
        {
            // במקרה כזה אין slug בנתיב (אתר בשורש) – לא מחזירים מזהה
            return null;
        }

        if (!SlugRegex.IsMatch(candidate)) return null;
        return await FindBySlugAsync(db, candidate);
    }

    private Task<int?> FindBySlugAsync(AppDbContext db, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult<int?>(null);

        var cacheKey = $"inst:slug:{slug}";
        if (_cache.TryGetValue(cacheKey, out int cachedId))
            return Task.FromResult<int?>(cachedId);

        return LoadAndCacheAsync(db, slug, cacheKey);
    }

    private async Task<int?> LoadAndCacheAsync(AppDbContext db, string slug, string cacheKey)
    {
        var entity = await db.Institutions
            .AsNoTracking()
            .Where(i => i.IsActive)
            .FirstOrDefaultAsync(i => i.Subdomain != null && i.Subdomain.ToLower() == slug);

        if (entity == null) return null;

        _cache.Set(cacheKey, entity.InstitutionId, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return entity.InstitutionId;
    }

    private sealed class ApiClientConfig
    {
        public int InstitutionId { get; set; }
        public string Name { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }
}
