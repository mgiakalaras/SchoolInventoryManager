using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;

var builder = WebApplication.CreateBuilder(args);

// Ensure local runtime folders exist both on Windows and inside Docker.
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataPath);
Directory.CreateDirectory(Path.Combine(appDataPath, "imports"));

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// For school LAN / Docker deployment we usually serve plain HTTP.
// Enable HTTPS redirection only if explicitly requested in configuration.
if (builder.Configuration.GetValue<bool>("App:UseHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
