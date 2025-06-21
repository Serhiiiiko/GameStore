using AspNetCoreRateLimit;
using GameStore.Data;
using GameStore.Interfaces;
using GameStore.Repositories;
using GameStore.Services;
using GameStore.Services.HealthChecks;
using GameStore.Services.PaymentProviders;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ISteamTopUpRepository, SteamTopUpRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISteamTopUpService, SteamTopUpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
builder.Services.AddScoped<TestPaymentProvider>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddMemoryCache();

// Choose storage service based on configuration
var useMinIO = builder.Configuration.GetValue<bool>("Storage:UseMinIO", false);
if (useMinIO)
{
    builder.Services.AddScoped<IFileService, MinioStorageService>();
    builder.Logging.AddConsole().AddFilter("MinIO", LogLevel.Information);
}
else
{
    builder.Services.AddScoped<IFileService, FileService>();
}

// Rate limiting configuration
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Health checks
builder.Services.AddSingleton<DatabaseHealthCheck>();
builder.Services.AddSingleton<MinioHealthCheck>();
builder.Services.AddSingleton<TelegramHealthCheck>();

builder.Services.AddHealthChecks()
    .AddTypeActivatedCheck<DatabaseHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "critical" })
    .AddTypeActivatedCheck<MinioHealthCheck>(
        "minio",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "storage" })
    .AddTypeActivatedCheck<TelegramHealthCheck>(
        "telegram",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "notifications" })
    .AddProcessAllocatedMemoryHealthCheck(
        maximumMegabytesAllocated: 500,
        name: "memory",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "memory" });

// Add health monitoring background service
builder.Services.AddHostedService<HealthMonitoringService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Create uploads directory if using local storage
if (!useMinIO)
{
    var uploadsPath = Path.Combine(app.Environment.WebRootPath, "images", "uploads");
    if (!Directory.Exists(uploadsPath))
    {
        Directory.CreateDirectory(uploadsPath);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add session middleware before authorization
app.UseSession();

app.UseAuthorization();

// Add rate limiting middleware
app.UseIpRateLimiting();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("critical"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, _) =>
    {
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("OK");
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Startup initialization
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var telegramService = scope.ServiceProvider.GetService<ITelegramNotificationService>();

    try
    {
        // Apply migrations
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");

        // Send startup error notification
        if (telegramService != null)
        {
            await telegramService.SendServiceRestartNotificationAsync(
                "GameStore Web Application",
                $"Failed to apply database migrations: {ex.Message}");
        }

        // Don't throw - let the health checks handle it
    }

    // Send startup notification
    if (telegramService != null)
    {
        await telegramService.SendServiceRestartNotificationAsync(
            "GameStore Web Application",
            "Application started successfully");
    }
}

app.Run();