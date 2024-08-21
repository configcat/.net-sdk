# Sample ASP.NET Core MVC app

This is a simple [ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc) web application to demonstrate how to use the ConfigCat SDK.

1. Install [.NET](https://dotnet.microsoft.com/download)
2. Change dir to `/WebApplication`
   ```bash
   cd WebApplication
   ```
3. Run app
    ```bash 
    dotnet run
    ```
4. Open http://localhost:5000 in browser

## Backdoor controller - webhook example
The purpose of `http://localhost:5000/api/backdoor/configcatchanged` is to be called by ConfigCat Webhhooks. This way the application is notified by ConfigCat when feature flag or setting values updated and the new config is ready for downloading.

Webhooks on ConfigCat Dashboard: https://app.configcat.com/webhook

Webhooks on ConfigCat Docs: https://configcat.com/docs/advanced/notifications-webhooks 