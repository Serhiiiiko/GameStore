using GameStore.Data;
using GameStore.Interfaces;
using GameStore.Repositories;
using GameStore.Services;
using Microsoft.EntityFrameworkCore;

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

// Configure PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ISteamTopUpRepository, SteamTopUpRepository>();

// Register services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISteamTopUpService, SteamTopUpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// Добавить вместе с другими сервисами
builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply migrations at startup in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}

app.Run();