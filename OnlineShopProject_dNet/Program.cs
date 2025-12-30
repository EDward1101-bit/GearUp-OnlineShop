using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

// Force UTF-8 encoding for console and strings
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


// CONFIGURARE IDENTITY
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Stores.MaxLengthForKeys = 128;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRazorPages();

// Add HttpContext accessor
builder.Services.AddHttpContextAccessor();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddScoped<OnlineShopProject_dNet.Services.CartService>();
builder.Services.AddScoped<OnlineShopProject_dNet.Services.TextProcessingService>();
builder.Services.AddScoped<OnlineShopProject_dNet.Services.ProductAIService>();

var app = builder.Build();

// Force UTF-8 response encoding
app.Use(async (context, next) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// Custom error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred.");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/html";

        await context.Response.WriteAsync(@"
            <html>
                <head>
                    <title>Eroare Internă Server</title>
                    <link href='/lib/bootstrap/dist/css/bootstrap.min.css' rel='stylesheet' />
                </head>
                <body class='bg-light'>
                    <div class='container mt-5'>
                        <div class='row justify-content-center'>
                            <div class='col-md-6'>
                                <div class='card shadow'>
                                    <div class='card-body text-center'>
                                        <i class='bi bi-exclamation-triangle text-danger' style='font-size: 4rem;'></i>
                                        <h1 class='card-title mt-3'>Oops! Ceva nu a mers bine</h1>
                                        <p class='card-text'>A apărut o eroare neașteptată. Vă rugăm să încercați din nou mai târziu.</p>
                                        <a href='/' class='btn btn-primary'>Înapoi la Acasă</a>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </body>
            </html>");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --- ZONA DE SEEDING ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "A aparut o eroare la Seeding.");
    }
}
// --- FINAL ZONA SEEDING ---

// Maparile de rute
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();


app.Run();