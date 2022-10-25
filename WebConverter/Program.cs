using Microsoft.AspNetCore.Authentication.Cookies;
using ServiceWorkerCronJobDemo.Services;
using WebConverter.Hubs;
using WebConverter.Utils;
using WebConverter.Utils.Interface;

var builder = WebApplication.CreateBuilder(args);

// Dependency injection
builder.Services.AddScoped<IConverter, PDFConverter>();
builder.Services.AddScoped<IFileStorage, FileStorage>();
builder.Services.AddScoped<IScopedService, ProcessFileService>();


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddCronJob<FilesCronJob>(c =>
{
    c.TimeZoneInfo = TimeZoneInfo.Local;
    c.CronExpression = builder.Configuration["Services:Files:CronExpression"];
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Events = new CookieAuthenticationEvents()
    {
        OnSigningIn = async context =>
        {
            await Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<SignalRHub>("/signalRHub");

app.Run();