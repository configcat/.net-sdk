using System;
using System.Net.Http;
using ConfigCat.Client;
using ConfigCat.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

/* Calling `UseConfigCat()` on the application builder is all you need to register ConfigCat clients
   based on application configuration (see appsettings.json). */

var configCatBuilder = builder.UseConfigCat();

/* You can also configure ConfigCat clients by code. Please note that if you add a client here that
   is already defined in the configuration (e.g. appsettings.json), the settings from the configuration
   and the settings made by code will be merged, with the latter taking precedence. */

//configCatBuilder
//    .AddDefaultClient(options =>
//    {
//        options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(10));
//    })
//    .AddNamedClient("secondary", options =>
//    {
//        options.SdkKey = "PKDVCLf-Hq-h-kCzMp-L7Q/PaDVCFk9EpmD6s-invalid";
//    })
//    .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: true));

/* Uncomment the following lines if you want to configure ConfigCat clients to use IHttpClientFactory
   for fetching config data, instead of using the SDK's built-in HTTP connection management. */

//configCatBuilder.UseHttpClientFactory<IHttpClientFactory>((factory, _, _) => factory.CreateClient());
//builder.Services.AddHttpClient();

/* Uncomment the following line to enable structured logging. */
//builder.Logging.AddJsonConsole(options => options.JsonWriterOptions = new() { Indented = true });

// Add services to the container.
builder.Services.AddControllersWithViews();

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
