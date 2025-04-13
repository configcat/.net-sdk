using System;
using ConfigCat.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Uncomment the following line for structured logging.
//builder.Logging.AddJsonConsole(options => options.JsonWriterOptions = new() { Indented = true });

// Add services to the container.
builder.Services.AddControllersWithViews();

var configCatSdkKey = builder.Configuration["ConfigCatSdkKey"];

builder.UseConfigCat()
    // Register ConfigCatClient so you can inject it in your controllers, actions, etc.
    .AddDefaultClient("PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ", options =>
    {
        options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
    })
    .AddKeyedClient("secondary", "PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ", options =>
     {
         options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
