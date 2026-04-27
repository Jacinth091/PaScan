using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaScan.Data;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<PaScan.Repositories.Interfaces.IDeviceRequestRepository, PaScan.Repositories.DeviceRequestRepository>();
builder.Services.AddScoped<PaScan.Repositories.Interfaces.IDeviceRepository, PaScan.Repositories.DeviceRepository>();
builder.Services.AddScoped<PaScan.Repositories.Interfaces.IStudentRepository, PaScan.Repositories.StudentRepository>();
builder.Services.AddScoped<PaScan.Repositories.Interfaces.IQrTokenRepository, PaScan.Repositories.QrTokenRepository>();
builder.Services.AddScoped<PaScan.Repositories.Interfaces.IRfidCardRepository, PaScan.Repositories.RfidCardRepository>();
builder.Services.AddScoped<PaScan.Repositories.Interfaces.IScanLogRepository, PaScan.Repositories.ScanLogRepository>();
builder.Services.AddScoped<PaScan.Repositories.Interfaces.IScannerRepository, PaScan.Repositories.ScannerRepository>();

// Services
builder.Services.AddScoped<PaScan.Services.Interfaces.IAuthService, PaScan.Services.AuthService>();
builder.Services.AddScoped<PaScan.Services.Interfaces.IDeviceService, PaScan.Services.DeviceService>();
builder.Services.AddScoped<PaScan.Services.Interfaces.IAdminService, PaScan.Services.AdminService>();
builder.Services.AddScoped<PaScan.Services.Interfaces.IRfidService, PaScan.Services.RfidService>();
builder.Services.AddScoped<PaScan.Services.Interfaces.IScanService, PaScan.Services.ScanService>();
builder.Services.AddScoped<PaScan.Services.Interfaces.IQrTokenService, PaScan.Services.QrTokenService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/access-denied";
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); // Auto-apply migrations
    DbSeeder.Initialize(context);
}

app.Run();
