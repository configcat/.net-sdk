using ConfigCat.Client;
using System;
using System.Collections.Generic;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const string apiKey = "PKDVCLf-Hq-h-kCzMp-L7Q/PaDVCFk9EpmD6sLpGLltTA";

            // create Client instance with ConfigCatClientBuilder
            var client = ConfigCatClientBuilder
                .Initialize(apiKey)
                .WithAutoPoll()
                .WithMaxInitWaitTimeSeconds(10)
                .Create();

            // create a user object to identify the caller
            ConfigCat.Client.Evaluate.User user = new ConfigCat.Client.Evaluate.User("2605ED59-D7D0-4D84-9CDF-5E37B971DD03")
            {
                Country = "US",
                Email = "myUser@example.com",
                Custom = new Dictionary<string, string>
                {
                    { "SubscriptionType", "Pro"},
                    { "Role", "Admin"},

                }
            };

            // current project's setting key name is 'keyBool'            
            var myNewFeatureEnabled = client.GetValue("keyBool", false, user);

            // is my new feature enabled?
            if (myNewFeatureEnabled)
            {
                Console.WriteLine(" Here is my new feature...");
                Console.WriteLine(client.GetValue(" keyString", "", user));
            }

            // 'myKeyNotExits' setting doesn't exist in the project configuration and the client returns default value ('N/A');
            var mySettingNotExists = client.GetValue("myKeyNotExits", "N/A", user);

            Console.WriteLine();
            Console.WriteLine("'mySettingNotExists' value from ConfigCat: {0}", mySettingNotExists);
            
            Console.WriteLine("\n\nPress any key(s) to exit...");
            Console.ReadKey();            
        }      
    }
}
