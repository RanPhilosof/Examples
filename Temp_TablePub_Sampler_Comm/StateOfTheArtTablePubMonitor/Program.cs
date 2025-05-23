using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Prober.Consumer.Service;
using RP.Infra;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<ProberCacheMonitoringService>();
builder.Services.AddSingleton<IServicesInfo>(new ServicesInfo());
builder.Services.AddBlazoredLocalStorage();

builder.Host.ConfigureLogging(logging =>
     {
         logging.ClearProviders();
         logging.AddConsole(); // Logs to console
         logging.SetMinimumLevel(LogLevel.Information); // Optional
     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
