using System;
using System.Globalization;
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

        var actualAttributes = user.GetAllAttributes();

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

            Custom =
            {
                { "myCustomAttribute", "myCustomAttributeValue"},
                { nameof(User.Identifier), "myIdentifier"},
                { nameof(User.Country), "United States"},
                { nameof(User.Email), "otherEmail@example.com"}
            }
        };

        // Act

        var actualAttributes = user.GetAllAttributes();

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

            Custom =
            {
                { attributeName, attributeValue}
            }
        };

        // Act

        var actualAttributes = user.GetAllAttributes();

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
        Assert.AreEqual(expectedValue, user.GetAllAttributes()[nameof(User.Identifier)]);
    }


    [DataTestMethod]
    [DataRow("datetime", "2023-09-19T11:01:35.0000000+00:00", "1695121295")]
    [DataRow("datetime", "2023-09-19T13:01:35.0000000+02:00", "1695121295")]
    [DataRow("datetime", "2023-09-19T11:01:35.0510886+00:00", "1695121295.051")]
    [DataRow("datetime", "2023-09-19T13:01:35.0510886+02:00", "1695121295.051")]
    [DataRow("number", "3", "3")]
    [DataRow("number", "3.14", "3.14")]
    [DataRow("number", "-1.23e-100", "-1.23e-100")]
    [DataRow("stringlist", "a,,b,c", "[\"a\",\"\",\"b\",\"c\"]")]
    public void HelperMethodsShouldWork(string type, string value, string expectedAttributeValue)
    {
        string actualAttributeValue;
        switch (type)
        {
            case "datetime":
                var dateTimeOffset = DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture);
                actualAttributeValue = User.AttributeValueFrom(dateTimeOffset);
                break;
            case "number":
                var number = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                actualAttributeValue = User.AttributeValueFrom(number);
                break;
            case "stringlist":
                var items = value.Split(',');
                actualAttributeValue = User.AttributeValueFrom(items);
                break;
            default:
                throw new InvalidOperationException();
        }

        Assert.AreEqual(expectedAttributeValue, actualAttributeValue);
    }
}
