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

    [DataRow("-62135596801", -1L)]
    [DataRow("-62135596800", 0L)]
    [DataRow("0", 621355968000000000L)]
    [DataRow("+253402300799", 3155378975990000000L)]
    [DataRow("+253402300800", -1L)]
    [DataRow("1.0", -1L)]
    [DataRow("1x", -1L)]
    [DataTestMethod]
    public void DateTimeUtils_UnixTimeStampConversion_Works(string dateString, long ticks)
    {
        var success = DateTimeUtils.TryParseUnixTimeStamp(dateString.AsSpan(), out var dateTime);
        if (ticks >= 0)
        {
            Assert.IsTrue(success);
            Assert.AreEqual(ticks, dateTime.Ticks);
            Assert.AreEqual(
                dateString.StartsWith("+", StringComparison.Ordinal) ? dateString.Substring(1) : dateString,
                DateTimeUtils.ToUnixTimeStamp(dateTime));
        }
        else
        {
            Assert.IsFalse(success);
        }
    }
}
