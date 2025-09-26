using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using ChafetzChesed.BLL.Interfaces;
using ChafetzChesed.BLL.Services;
using ChafetzChesed.Common;
using ChafetzChesed.Common.Utilities;
using ChafetzChesed.DAL.Data;
using ChafetzChesed.Middleware;

var builder = WebApplication.CreateBuilder(args);

/* --------- Logging --------- */
builder.Logging.AddEventLog();

/* --------- JWT settings --------- */
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            NameClaimType = "sub",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

/* --------- CORS (לוקאלי בלבד) --------- */
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost4300", policy =>
    {
        policy.WithOrigins("http://localhost:4300")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

/* --------- Infra --------- */
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

/* --------- DI --------- */
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDepositTypeService, DepositTypeService>();
builder.Services.AddScoped<ILoanTypeService, LoanTypeService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IDepositService, DepositService>();
builder.Services.AddScoped<IExternalFormService, ExternalFormService>();
builder.Services.AddScoped<IExternalUserSyncService, ExternalUserSyncService>();
builder.Services.AddScoped<IDepositWithdrawService, DepositWithdrawService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFreezeRequestService, FreezeRequestService>();
builder.Services.AddScoped<IAccountActionsService, AccountActionsService>();
builder.Services.AddScoped<JwtService>();
builder.Services.Configure<List<ApiClient>>(builder.Configuration.GetSection("ApiClients"));
builder.Services.AddScoped<IDeltaSyncService, DeltaSyncService>();

builder.Services.AddScoped<IAuditLogger, AuditLogger>();

/* --------- DB --------- */
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

/* --------- Controllers & JSON --------- */
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
    o.JsonSerializerOptions.Converters.Add(new NullableDateTimeConverter());
});

/* --------- Swagger --------- */
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChafetzChesed API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "נא להזין את ה־JWT Token (רק את הטוקן, בלי המילה Bearer)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

/* --------- Error Handling --------- */
app.UseExceptionHandler("/error");
app.Map("/error", (HttpContext ctx, ILogger<Program> log) =>
{
    var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;
    log.LogError("Unhandled error. traceId={TraceId}", traceId);
    return Results.Problem(
        title: "Unhandled server error",
        statusCode: StatusCodes.Status500InternalServerError,
        extensions: new Dictionary<string, object?> { ["traceId"] = traceId }
    );
});

app.UseStatusCodePages();

/* --------- Swagger UI --------- */
app.UseSwagger(c =>
{
    // תמיכה ב-PathBase (למשל כשאפליקציה מתארחת תחת /api ב-IIS)
    c.PreSerializeFilters.Add((swagger, req) =>
    {
        var basePath = req.PathBase.HasValue ? req.PathBase.Value : string.Empty;
        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = basePath } };
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("swagger/v1/swagger.json", "ChafetzChesed API v1");
    c.RoutePrefix = "swagger"; // זמין ב- /swagger (או /api/swagger אם יש PathBase=/api)
});

app.UseHttpsRedirection();

/* CORS – רק לפיתוח מקומי */
app.UseCors("AllowLocalhost4300");

/* --------- Middlewares לאזור ה-API --------- */
/* חשוב: כשמריצים כ-Application תחת /api ב-IIS, ה-PathBase יהיה "/api"
   ואז Request.Path לא יתחיל ב-/api. לכן בודקים גם PathBase. */
app.UseWhen(ctx =>
{
    var path = ctx.Request.Path.Value ?? string.Empty;
    var basePath = ctx.Request.PathBase.Value ?? string.Empty;

    return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
           || basePath.Equals("/api", StringComparison.OrdinalIgnoreCase)
           || basePath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
},
branch =>
{
    branch.UseMiddleware<ApiKeyMiddleware>();            
    branch.UseMiddleware<InstitutionResolverMiddleware>(); 
    branch.UseMiddleware<JwtMiddleware>();                 
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok("API is up"));
app.MapGet("/health", () => Results.Ok("healthy"));


app.MapControllers();

app.Run();
