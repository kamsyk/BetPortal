using BetPortalWebApi.Middleware;
using BetPortalWebApi.Services;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddMemoryCache();

// Configure Session-based Authentication
const string SessionAuthScheme = "SessionAuth";
const string CookieAuthScheme = "CookieAuth";
const string DynamicAuthScheme = "DynamicAuth";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = DynamicAuthScheme;
        options.DefaultChallengeScheme = DynamicAuthScheme;
    })
    .AddPolicyScheme(DynamicAuthScheme, DynamicAuthScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            // Prefer durable auth cookie when present; fallback to legacy SessionId flow.
            if (context.Request.Cookies.ContainsKey("AuthCookie"))
            {
                return CookieAuthScheme;
            }

            return SessionAuthScheme;
        };
    })
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthScheme, null)
    .AddCookie(CookieAuthScheme, options =>
    {
        options.Cookie.Name = "AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        // Handle API requests with status codes instead of redirects
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

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

app.UseCors("DefaultCors");

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
app.UseAuthorization();

app.MapControllers();

app.Run();