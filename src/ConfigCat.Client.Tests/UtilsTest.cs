using System;
using ConfigCat.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class UtilsTest
{
    [DataRow(new byte[] { }, "")]
    [DataRow(new byte[] { 0 }, "00")]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }, "0123456789abcdef")]
    [DataRow(new byte[] { 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10 }, "fedcba9876543210")]
    [DataTestMethod]
    public void ArrayUtils_ToHexString_Works(byte[] bytes, string expected)
    {
        Assert.AreEqual(expected, bytes.ToHexString());
    }

    [DataRow("Mon, 01 Jan 0001 00:00:00 GMT", 0L)]
    [DataRow("Fri, 31 Dec 9999 23:59:59 GMT", 3155378975990000000L)]
    [DataRow("Wed, 21 Oct 2015 07:28:00 GMT", 635810092800000000L)]
    [DataRow("Wed, 35 Oct 2015 07:28:00 GMT", -1)]
    [DataRow("Wed, 21 Oct 2015 07:28:00 CET", -1)]
    [DataTestMethod]
    public void DateTimeUtils_HttpHeaderDateConversion_Works(string dateString, long ticks)
    {
        var success = DateTimeUtils.TryParseHttpHeaderDate(dateString.AsSpan(), out var dateTime);
        if (ticks >= 0)
        {
            Assert.IsTrue(success);
            Assert.AreEqual(ticks, dateTime.Ticks);
            Assert.AreEqual(dateString, DateTimeUtils.ToHttpHeaderDate(dateTime));
        }
        else
        {
            Assert.IsFalse(success);
        }
    }
}
