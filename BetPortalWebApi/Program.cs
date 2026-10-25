using System.Security.Claims;
using BetPortalWebApi.ErrorHandling;
using BetPortalWebApi.Logging;
using BetPortalWebApi.Middleware;
using BetPortalWebApi.Services;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) =>
    SerilogConfiguration.Configure(loggerConfiguration, services.GetRequiredService<IConfiguration>()));

builder.Services.AddProblemDetails(options =>
{
    // Expose a support reference instead of the raw W3C traceparent value.
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.Remove("traceId");
        context.ProblemDetails.Extensions["errorId"] = ErrorReference.Get(context.HttpContext);
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "BetPortal API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddSingleton<IUserService, UserService>();

builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddHostedService<SessionCleanupBackgroundService>();

builder.Services.AddScoped<IGoogleAccountService, GoogleAccountService>();

builder.Services.AddScoped<IFacebookAccountService, FacebookAccountService>();

builder.Services.AddScoped<IMicrosoftAccountService, MicrosoftAccountService>();

builder.Services.AddScoped<IExternalAuthErrorDetailsService, ExternalAuthErrorDetailsService>();

builder.Services.AddSingleton<IEmailService, AzureCommunicationEmailService>();

builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<IDbQueryService, DbQueryService>();

builder.Services.AddScoped<IGridSettingsService, GridSettingsService>();

builder.Services.AddHttpClient();

builder.Services.AddHttpClient("ClaudeChatService", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["ClaudeChatService:BaseUrl"] ?? "http://localhost:8000";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(
        int.TryParse(configuration["ClaudeChatService:TimeoutSeconds"], out var timeoutSeconds)
            ? timeoutSeconds
            : 120);
});

const string SessionAuthScheme = "SessionAuth";

var authentication = builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = SessionAuthScheme;
        options.DefaultChallengeScheme = SessionAuthScheme;
        options.DefaultScheme = SessionAuthScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthScheme, null)
    .AddCookie("GoogleExternal", options =>
    {
        options.Cookie.Name = "GoogleExternal";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddCookie("MicrosoftExternal", options =>
    {
        options.Cookie.Name = "MicrosoftExternal";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddCookie("FacebookExternal", options =>
    {
        options.Cookie.Name = "FacebookExternal";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    });

if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.SignInScheme = "GoogleExternal";
        options.CallbackPath = "/signin-google";
        options.UsePkce = true;
        options.ClaimActions.MapJsonKey("google:email_verified", "email_verified");
        options.Events.OnRemoteFailure = context => ExternalAuthRemoteFailureHandler.HandleAsync(context, "Google");
    });
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Facebook:AppId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Facebook:AppSecret"]))
{
    authentication.AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? string.Empty;
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? string.Empty;
        options.SignInScheme = "FacebookExternal";
        options.CallbackPath = "/signin-facebook";
        options.Fields.Add("first_name");
        options.Fields.Add("last_name");
        options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");
        options.Events.OnRemoteFailure = context => ExternalAuthRemoteFailureHandler.HandleAsync(context, "Facebook");
    });
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Microsoft:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Microsoft:ClientSecret"]))
{
    authentication.AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? string.Empty;
        options.SignInScheme = "MicrosoftExternal";
        options.CallbackPath = "/signin-microsoft";
        options.UsePkce = true;

        // A work/school userPrincipalName is not necessarily a mailbox address.
        options.ClaimActions.Remove(ClaimTypes.Email);
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "mail");
        options.Events.OnRemoteFailure = context => ExternalAuthRemoteFailureHandler.HandleAsync(context, "Microsoft");
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Permission", "AdminAccess");
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

string? dbConnectionString = builder.Configuration.GetConnectionString("BetPortalDbConnectionString");

if (string.IsNullOrEmpty(dbConnectionString))
{
    throw new ArgumentNullException(dbConnectionString);
}

builder.Services.AddDbContext<BpDbContext>((provider, options) =>
{
    options.UseSqlServer(dbConnectionString, mssqlOptions => mssqlOptions.MigrationsAssembly("DbUpdateMsSql"));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Dev fallback - allow common development ports
            policy.WithOrigins("http://localhost:4200", "http://localhost:4210", "http://localhost:3000", "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseCors("DefaultCors");

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseUserLogContext();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
