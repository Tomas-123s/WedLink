using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.AdminAccounts;
using WedLink.DataAccess.Customers;
using WedLink.DataAccess.Templates;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence;
using WedLink.Business.AdminAccounts;
using WedLink.Business.Customers;
using WedLink.Business.Templates;
using WedLink.Business.Orders;
using WedLink.Business.CustomerSubmissions;
using WedLink.DataAccess.Storage;
using WedLink.DataAccess.Invitations;
using WedLink.Business.Invitations;
using System.Threading.RateLimiting;
using WedLink.API.Filters;
using WedLink.Business.Guests;
using WedLink.DataAccess.Guests;
using WedLink.API.Services;
using WedLink.Business.Dashboard;
using WedLink.DataAccess.Dashboard;
using WedLink.Business.Audit;
using WedLink.DataAccess.Audit;

var builder = WebApplication.CreateBuilder(args);

// Render terminates public HTTPS at its load balancer and forwards the request
// to Kestrel over HTTP. Process its scheme header before HTTPS and secure-cookie
// middleware so the app still recognizes the original request as HTTPS.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    if (builder.Environment.IsProduction())
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("WedLinkDb");

if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "Connection string 'WedLinkDb' is missing. Set it with user-secrets for local development.");
}

builder.Services.AddDbContext<WedLinkDbContext>(options =>
    options.UseSqlServer(connectionString ?? "Server=(local);Database=WedLinkTests;Integrated Security=True;"));

builder.Services.AddScoped<IAdminAccountRepository, IdentityAdminAccountRepository>();
builder.Services.AddScoped<AdminAccountService>();
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ITemplateCatalogRepository, EfTemplateCatalogRepository>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<TemplateVersionService>();
builder.Services.AddScoped<FeatureService>();
builder.Services.AddScoped<AddOnService>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ICustomerSubmissionRepository, EfCustomerSubmissionRepository>();
builder.Services.AddSingleton<IPrivateLinkTokenGenerator, PrivateLinkTokenGenerator>();
builder.Services.AddScoped<CustomerSubmissionService>();
builder.Services.AddScoped<IInvitationRepository, EfInvitationRepository>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<IInvitationExpirationRepository, EfInvitationRepository>();
builder.Services.AddScoped<InvitationExpirationService>();
builder.Services.AddScoped<IMediaRetentionRepository, EfMediaRetentionRepository>();
builder.Services.AddScoped<MediaRetentionService>();
builder.Services.AddSingleton(TimeProvider.System);
var invitationLifecycleSettings = new InvitationLifecycleSettings(
    builder.Configuration.GetValue("InvitationLifecycle:DefaultValidityMonths", InvitationLifecycleSettings.DefaultValidityMonthsValue),
    builder.Configuration.GetValue("InvitationLifecycle:ExpirationIntervalMinutes", InvitationLifecycleSettings.DefaultExpirationIntervalMinutes),
    builder.Configuration.GetValue("InvitationLifecycle:MediaCleanupIntervalMinutes", InvitationLifecycleSettings.DefaultMediaCleanupIntervalMinutes));
invitationLifecycleSettings.Validate();
builder.Services.AddSingleton(invitationLifecycleSettings);
builder.Services.AddHostedService<InvitationExpirationWorker>();
builder.Services.AddHostedService<MediaRetentionWorker>();
if (builder.Environment.IsProduction())
{
    builder.Services.AddHostedService<EditorialLuxuryTemplateSeedWorker>();
}
builder.Services.AddScoped<IGuestRepository, EfGuestRepository>();
builder.Services.AddScoped<GuestService>();
builder.Services.AddScoped<IAdminDashboardRepository, EfAdminDashboardRepository>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<ISubmissionFileStorage, DatabaseSubmissionFileStorage>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = 110L * 1024 * 1024);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 110L * 1024 * 1024);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("private-submission", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("private-upload", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("private-preview", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("public-invitation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("guest-rsvp", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("guest-messages", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});

builder.Services
    .AddIdentityCore<AdminUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<WedLinkDbContext>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "WedLink.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "WedLink.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddScoped<AdminAntiforgeryFilter>();
builder.Services.AddScoped<AdminAuditFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<AdminAntiforgeryFilter>();
    options.Filters.AddService<AdminAuditFilter>();
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Production deployment uses MonsterASP SQL Server through the configured
// ConnectionStrings:WedLinkDb value. Apply pending schema changes before
// accepting requests so new tables (including StoredMediaFiles) exist.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<WedLinkDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
