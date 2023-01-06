using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class UserTests
{
    [TestMethod]
    public void CreateUser_WithIdAndEmailAndCountry_AllAttributesShouldContainsPassedValues()
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

        Assert.IsTrue(actualAttributes.TryGetValue(nameof(User.Email), out var s));
        Assert.AreEqual("id@example.com", s);

        Assert.IsTrue(actualAttributes.TryGetValue(nameof(User.Country), out s));
        Assert.AreEqual("US", s);

        Assert.IsTrue(actualAttributes.TryGetValue(nameof(User.Identifier), out s));
        Assert.AreEqual("id", s);
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
                { "myCustomAttribute", "myCustomAttributeValue"},
                { nameof(User.Identifier), "myIdentifier"},
                { nameof(User.Country), "United States"},
                { nameof(User.Email), "otherEmail@example.com"}
            }
        };

        // Act

        var actualAttributes = user.AllAttributes;

        // Assert

        Assert.IsTrue(actualAttributes.TryGetValue(nameof(User.Identifier), out var s));
        Assert.AreEqual("id", s);
        Assert.AreNotEqual("myIdentifier", s);

        Assert.IsTrue(actualAttributes.TryGetValue(nameof(User.Country), out s));
        Assert.AreEqual("US", s);
        Assert.AreNotEqual("United States", s);

        Assert.IsTrue(actualAttributes.TryGetValue(nameof(User.Email), out s));
        Assert.AreEqual("id@example.com", s);
        Assert.AreNotEqual("otherEmail@example.com", s);

        Assert.AreEqual(4, actualAttributes.Count);
    }

    [DataTestMethod]
    [DataRow("identifier", "myId")]
    [DataRow("IDENTIFIER", "myId")]
    [DataRow("email", "theBoss@example.com")]
    [DataRow("EMAIL", "theBoss@example.com")]
    [DataRow("eMail", "theBoss@example.com")]
    [DataRow("country", "myHome")]
    [DataRow("COUNTRY", "myHome")]
    public void UseWellKnownAttributesAsCustomPropertiesWithDifferentNames_ShouldAppendAllAttributes(string attributeName, string attributeValue)
    {
        // Arrange

        var user = new User("id")
        {
            Email = "id@example.com",

            Country = "US",

            Custom = new Dictionary<string, string>
            {
                { attributeName, attributeValue}
            }
        };

        // Act

        var actualAttributes = user.AllAttributes;

        // Assert

        Assert.AreEqual(4, actualAttributes.Count);

        Assert.IsTrue(actualAttributes.TryGetValue(attributeName, out var s));
        Assert.AreEqual(attributeValue, s);
    }

    [DataTestMethod()]
    [DataRow(null, User.DefaultIdentifierValue)]
    [DataRow("", User.DefaultIdentifierValue)]
    [DataRow("id", "id")]
    [DataRow("\t", "\t")]
    [DataRow("\u1F600", "\u1F600")]
    public void CreateUser_ShouldSetIdentifier(string identifier, string expectedValue)
    {
        var user = new User(identifier);

        Assert.AreEqual(expectedValue, user.Identifier);
        Assert.AreEqual(expectedValue, user.AllAttributes[nameof(User.Identifier)]);
    }
}
