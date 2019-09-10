using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ConfigCat.Client.Tests
{
    [TestClass]
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

        [TestMethod]
        public void UseCustomProperties()
        {
            // Arrange            

            var user = new User("id")
            {
                Email = "id@example.com",

                Country = "US"
            };

            // Act

            var actualAttributes = user.AllAttributes;

            // Assert

            string s;
            Assert.IsTrue(actualAttributes.TryGetValue("email", out s));
            Assert.AreEqual("id@example.com", s);

            s = null;
            Assert.IsTrue(actualAttributes.TryGetValue("country", out s));
            Assert.AreEqual("US", s);

            s = null;
            Assert.IsTrue(actualAttributes.TryGetValue("identifier", out s));
            Assert.AreEqual("id", s);

            Assert.AreEqual(3, actualAttributes.Count);
        }

        [TestMethod]
        public void UseWellKnownAttributesAsCustomProperties_ShouldNotAppendAllAttributes()
        {
            // Arrange

            var user = new User("id")
            {
                Email = "id@example.com",

                Country = "US",

                Custom = new Dictionary<string, string>
                {
                    { "myCustomAttribute", ""},
                    { "identifier", "myIdentifier"},
                    { "country", "United States"},
                    { "email", "otherEmail@example.com"}
                }
            };

            // Act

            var actualAttributes = user.AllAttributes;

            // Assert

            Assert.AreEqual(4, actualAttributes.Count);

            string s;
            Assert.IsTrue(actualAttributes.TryGetValue("identifier", out s));
            Assert.AreEqual("id", s);

            s = null;
            Assert.IsTrue(actualAttributes.TryGetValue("country", out s));
            Assert.AreEqual("US", s);
            Assert.AreNotEqual("United States", s);

            s = null;
            Assert.IsTrue(actualAttributes.TryGetValue("email", out s));
            Assert.AreEqual("id@example.com", s);
            Assert.AreNotEqual("otherEmail@example.com", s);
        }
    }
}
