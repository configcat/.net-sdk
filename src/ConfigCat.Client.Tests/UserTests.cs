using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    public class UserTests
    {
        [TestMethod]
        public void CreateUser()
        {
            var u0 = new User(null);

            var u1 = new User("12345")
            {
                Email = "email",
                Country = "US",
                Custom =
                {
                    { "key", "value"}
                }
            };

            var u2 = new User("sw")
            {
                Email = null,
                Country = "US",
                Custom =
                {
                    { "key0", "value"},
                    { "key1", "value"},
                    { "key2", "value"},
                }
            };

            var u3 = new User("sw");
            
            u3.Custom.Add("customKey0", "customValue");
            u3.Custom["customKey1"] = "customValue";
        }
    }
}
