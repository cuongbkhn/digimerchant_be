using System.Text;
using DigiMerchantBE.Data;
using DigiMerchantBE.Entities;
using DigiMerchantBE.Middlewares;
using DigiMerchantBE.Options;
using DigiMerchantBE.Security;
using DigiMerchantBE.Services;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("api-logging-rules.json", optional: true, reloadOnChange: true);

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("Missing Jwt configuration.");
var fileLoggingSection = builder.Configuration.GetSection("FileLogging");
builder.Services.Configure<FileLoggingOptions>(fileLoggingSection);
var fileLoggingOptions = fileLoggingSection.Get<FileLoggingOptions>() ?? new FileLoggingOptions();
builder.Services.Configure<ApiLoggingOptions>(builder.Configuration.GetSection("ApiLogging"));
builder.Services.Configure<RefreshTokenCookieOptions>(builder.Configuration.GetSection("RefreshTokenCookie"));
builder.Services.Configure<CryptoOptions>(builder.Configuration.GetSection("Crypto"));
builder.Services.Configure<RuntimeOptions>(builder.Configuration.GetSection("Runtime"));
builder.Services.Configure<MobileConfigOptions>(builder.Configuration.GetSection("MobileConfig"));
builder.Services.Configure<ContentCatalogOptions>(builder.Configuration.GetSection("ContentCatalog"));
builder.Services.AddSingleton<IContentCatalog, ContentCatalog>();
var oracleConnectionString = builder.Configuration.GetConnectionString("OracleDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:OracleDb.");

var logFilePath = string.IsNullOrWhiteSpace(fileLoggingOptions.FilePath)
    ? "logs/digimerchant-.log"
    : fileLoggingOptions.FilePath;
var fullLogPath = Path.IsPathRooted(logFilePath)
    ? logFilePath
    : Path.Combine(builder.Environment.ContentRootPath, logFilePath);
var logDirectory = Path.GetDirectoryName(fullLogPath);
if (!string.IsNullOrWhiteSpace(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

if (!Enum.TryParse<LogEventLevel>(fileLoggingOptions.MinimumLevel, true, out var minimumLevel))
{
    minimumLevel = LogEventLevel.Information;
}

builder.Host.UseSerilog((_, _, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Is(minimumLevel)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: fullLogPath,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: Math.Max(1, fileLoggingOptions.MaxFileSizeMb) * 1024L * 1024L,
            rollOnFileSizeLimit: true);
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseOracle(oracleConnectionString);
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<IIconService, IconService>();
builder.Services.AddScoped<IMobileConfigService, MobileConfigService>();
builder.Services.AddScoped<IAppEnvironmentResolver, AppEnvironmentResolver>();
builder.Services.AddScoped<IUserHistoryService, UserHistoryService>();
builder.Services.AddScoped<ICryptoEnvelopeService, CryptoEnvelopeService>();
builder.Services.AddHostedService<LogCleanupHostedService>();
builder.Services.AddScoped<IPasswordHasher<DmUser>, PasswordHasher<DmUser>>();

builder.Services.AddSingleton<IAuthorizationHandler, FunctionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, FunctionAuthorizationPolicyProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token only",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [jwtSecurityScheme] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseMiddleware<ApiRequestResponseLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AngularDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
