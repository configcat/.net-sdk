using System;
using ConfigCat.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApplication.Adapters;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var configCatSdkKey = builder.Configuration["ConfigCatSdkKey"];

// Register ConfigCatClient as a singleton service so you can inject it in your controllers, actions, etc.
builder.Services.AddSingleton<IConfigCatClient>(sp =>
{
    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConfigCatClient>>();

    return ConfigCatClient.Get(configCatSdkKey, options =>
    {
        options.PollingMode = PollingModes.LazyLoad(cacheTimeToLive: TimeSpan.FromSeconds(120));
        options.Logger = new ConfigCatToMSLoggerAdapter(logger);
    });
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
