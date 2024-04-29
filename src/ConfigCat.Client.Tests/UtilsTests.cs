using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConfigCat.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class UtilsTests
{
    [DataRow(new byte[] { }, "")]
    [DataRow(new byte[] { 0 }, "00")]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }, "0123456789abcdef")]
    [DataRow(new byte[] { 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10 }, "fedcba9876543210")]
    [DataTestMethod]
    public void ArrayUtils_ToHexString_Works(byte[] bytes, string expectedResult)
    {
        Assert.AreEqual(expectedResult, bytes.ToHexString());
    }

    [DataRow(new byte[] { }, "", true)]
    [DataRow(new byte[] { }, "00", false)]
    [DataRow(new byte[] { }, " ", false)]
    [DataRow(new byte[] { }, "0", false)]
    [DataRow(new byte[] { 0 }, "00", true)]
    [DataRow(new byte[] { 0 }, "0000", false)]
    [DataRow(new byte[] { 0 }, "01", false)]
    [DataRow(new byte[] { 0 }, "000", false)]
    [DataRow(new byte[] { 0 }, " 00 ", false)]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }, "0123456789abcdef", true)]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }, "0123456789abcdee", false)]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }, "0123456789abcdeg", false)]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }, "0123456789a_bcde", false)]
    [DataTestMethod]
    public void ArrayUtils_EqualsToHexString_Works(byte[] bytes, string hexString, bool expectedResult)
    {
        Assert.AreEqual(expectedResult, bytes.Equals(hexString.AsSpan()));
    }

    [DataRow("-62135596800001", -1L)]
    [DataRow("-62135596800000", 0L)]
    [DataRow("0", 621355968000000000L)]
    [DataRow("+253402300799999", 3155378975999990000L)]
    [DataRow("+253402300800000", -1L)]
    [DataRow(".0", -1L)]
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

    [DataRow(-62135596800.001, -1L)]
    [DataRow(-62135596800.000, 0L)]
    [DataRow(0, 621355968000000000L)]
    [DataRow(+253402300799.999, 3155378975999990000L)]
    [DataRow(+253402300800.000, -1L)]
    [DataTestMethod]
    public void DateTimeUtils_UnixTimeSecondsConversion_Works(double seconds, long ticks)
    {
        var success = DateTimeUtils.TryConvertFromUnixTimeSeconds(seconds, out var dateTime);
        if (ticks >= 0)
        {
            Assert.IsTrue(success);
            Assert.AreEqual(ticks, dateTime.Ticks);
        }
        else
        {
            Assert.IsFalse(success);
        }
    }

    [DataRow(new string[] { }, 0, false, null, "")]
    [DataRow(new string[] { }, 1, true, null, "")]
    [DataRow(new string[] { "a" }, 0, false, null, "'a'")]
    [DataRow(new string[] { "a" }, 1, true, null, "'a'")]
    [DataRow(new string[] { "a" }, 1, true, "a", "'a'")]
    [DataRow(new string[] { "a", "b", "c" }, 0, false, null, "'a', 'b', 'c'")]
    [DataRow(new string[] { "a", "b", "c" }, 3, false, null, "'a', 'b', 'c'")]
    [DataRow(new string[] { "a", "b", "c" }, 2, false, null, "'a', 'b'")]
    [DataRow(new string[] { "a", "b", "c" }, 2, true, null, "'a', 'b', ...1 item(s) omitted")]
    [DataRow(new string[] { "a", "b", "c" }, 0, true, "a", "'a' -> 'b' -> 'c'")]
    [DataTestMethod]
    public void StringListFormatter_ToString_Works(string[] items, int maxLength, bool addOmittedItemsText, string? format, string expectedResult)
    {
        var actualResult = new StringListFormatter(items, maxLength, addOmittedItemsText ? static (count) => $", ...{count} item(s) omitted" : null)
            .ToString(format, CultureInfo.InvariantCulture);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void ModelHelper_SetOneOf_Works()
    {
        object? field = null;

        Assert.IsFalse(ModelHelper.IsValidOneOf(field));

        ModelHelper.SetOneOf<bool?>(ref field, null);
        Assert.IsNull(field);
        Assert.IsFalse(ModelHelper.IsValidOneOf(field));

        ModelHelper.SetOneOf(ref field, true);
        Assert.AreEqual(true, field);
        Assert.IsTrue(ModelHelper.IsValidOneOf(field));

        ModelHelper.SetOneOf<bool?>(ref field, null);
        Assert.AreEqual(true, field);
        Assert.IsTrue(ModelHelper.IsValidOneOf(field));

        ModelHelper.SetOneOf(ref field, true);
        Assert.IsNotNull(field);
        Assert.AreNotEqual(true, field);
        Assert.AreNotEqual(false, field);
        Assert.IsFalse(ModelHelper.IsValidOneOf(field));

        ModelHelper.SetOneOf<bool?>(ref field, null);
        Assert.IsNotNull(field);
        Assert.AreNotEqual(true, field);
        Assert.AreNotEqual(false, field);
        Assert.IsFalse(ModelHelper.IsValidOneOf(field));
    }

    private static IEnumerable<object?[]> GetEnumValues() => Enum.GetValues(typeof(SettingType))
        .Cast<SettingType>()
        .Concat(new[] { Setting.UnknownType })
        .Select(t => new object?[] { t });

    [DataTestMethod]
    [DynamicData(nameof(GetEnumValues), DynamicDataSourceType.Method)]
    public void ModelHelper_SetEnum_Works(SettingType enumValue)
    {
        SettingType field = default;

        if (Enum.IsDefined(typeof(SettingType), enumValue))
        {
            ModelHelper.SetEnum(ref field, enumValue);
            Assert.AreEqual(enumValue, field);
        }
        else
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => ModelHelper.SetEnum(ref field, enumValue));
        }
    }
}
