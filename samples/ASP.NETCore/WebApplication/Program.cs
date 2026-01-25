using System;
using ConfigCat.Client;
using ConfigCat.HostingIntegration;
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

builder.UseConfigCat()
    // Uncomment the following lines if you want to configure ConfigCat clients by code. Please note that
    // if you add a client here that is already defined in the configuration, the settings from the
    // configuration and the settings made by code will be merged, with the latter taking precedence.
    //.AddDefaultClient(options =>
    //{
    //    options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(10));
    //})
    //.AddNamedClient("secondary", options =>
    //{
    //    options.SdkKey = "PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ";
    //})
    .UseInitStrategy(ConfigCatInitStrategy.WaitForClientReadyAndLogOnFailure);

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
