using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WebStore")
        ?? throw new InvalidOperationException("Brak konfiguracji połączenia WebStore.")));
builder.Services.AddIdentityCore<User>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddWebStoreServices(builder.Configuration["AutoMapper:LicenseKey"]);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();
