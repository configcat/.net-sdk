using System;
using ConfigCat.Client;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Creating the ConfigCat client instance using the SDK Key
            var client = new ConfigCatClient("PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ");

            // Setting log level to Info to show detailed feature flag evaluation
            client.LogLevel = LogLevel.Info;

            // Creating a user object to identify the user (optional)
            User user = new User("<SOME USERID>")
            {
                Country = "US",
                Email = "configcat@example.com",
                Custom =
                {
                    { "SubscriptionType", "Pro"},
                    { "Role", "Admin"},
                    { "version", "1.0.0" }
                }
            };

            // Accessing feature flag or setting value
            var value = client.GetValue("isPOCFeatureEnabled", false, user);
            Console.WriteLine($"isPOCFeatureEnabled: {value}");
        }
    }
}
