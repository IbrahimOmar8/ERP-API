using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.MemoryStorage;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=erp.db"));

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddScoped<ERPTask.Services.InvoicePrintService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Application.Inerfaces.Integration.IRealtimeBroadcaster, ERPTask.Services.RealtimeBroadcaster>();

// SMTP / email
builder.Services.Configure<ERPTask.Services.SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<Application.Inerfaces.Notifications.IEmailService, ERPTask.Services.SmtpEmailService>();

// Hangfire — in-memory storage (no external DB needed; survives until restart).
// Switch to Hangfire.Storage.SQLite for persistence across restarts in production.
builder.Services.AddHangfire(c => c
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddScoped<ERPTask.Jobs.EtaRetryJob>();
builder.Services.AddScoped<ERPTask.Jobs.LowStockAlertJob>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        // Pick scheme dynamically: X-API-Key header → ApiKey, else JWT
        options.DefaultScheme = "JwtOrApiKey";
        options.DefaultChallengeScheme = "JwtOrApiKey";
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        // Allow SignalR to send the access token as a query-string
        // (browsers can't set Authorization headers on WebSocket upgrades)
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ERPTask.Auth.ApiKeyAuthenticationOptions, ERPTask.Auth.ApiKeyAuthenticationHandler>(
        ERPTask.Auth.ApiKeyAuthenticationOptions.Scheme, _ => { })
    .AddPolicyScheme("JwtOrApiKey", "JwtOrApiKey", options =>
    {
        options.ForwardDefaultSelector = ctx =>
            ctx.Request.Headers.ContainsKey("X-API-Key")
                ? ERPTask.Auth.ApiKeyAuthenticationOptions.Scheme
                : JwtBearerDefaults.AuthenticationScheme;
    });

builder.Services.AddAuthorization();

// Rate-limit anonymous login endpoints to slow down brute-force attacks.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-login", httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ERP API - نظام المخازن ونقاط البيع", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token (without 'Bearer ' prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(
        builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://127.0.0.1:3000" })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles(); // serves wwwroot/uploads/...
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ERPTask.Hubs.EventsHub>("/hubs/events");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new ERPTask.Auth.HangfireAdminAuthorization() },
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    var defaultPassword = builder.Configuration["DefaultAdminPassword"] ?? "Admin@1234";
    await DbSeeder.SeedAsync(db, defaultPassword);
}

// Hangfire scheduled jobs
RecurringJob.AddOrUpdate<ERPTask.Jobs.EtaRetryJob>(
    "eta-retry",
    j => j.RunAsync(CancellationToken.None),
    "*/15 * * * *"); // every 15 minutes

RecurringJob.AddOrUpdate<ERPTask.Jobs.LowStockAlertJob>(
    "low-stock-alert",
    j => j.RunAsync(CancellationToken.None),
    "0 8 * * *"); // daily at 08:00 UTC

app.Run();
