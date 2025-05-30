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

    [DataTestMethod]
    [DataRow(null, false, true, null)]
    [DataRow("abc", false, true, "abc")]
    [DataRow("abc", null, false, "abc")]
    [DataRow("abc", new object?[0], false, "abc")]
    [DataRow("abc{0}{1}{2}", new object?[] { 0.1, null, 23 }, false, "abc0.123")]
    public void LazyString_Value_Works(string? valueOrFormat, object? args, bool expectedIsValueCreated, string? expectedValue)
    {
        var lazyString = args is false ? new LazyString(valueOrFormat) : new LazyString(valueOrFormat!, (object?[]?)args);

        Assert.AreEqual(expectedIsValueCreated, lazyString.IsValueCreated);

        var value = lazyString.Value;
        Assert.AreEqual(expectedValue, value);

        Assert.IsTrue(lazyString.IsValueCreated);

        Assert.AreSame(value, lazyString.Value);

        Assert.AreSame(expectedValue is not null ? value : string.Empty, lazyString.ToString());
    }

    [TestMethod]
    public void SerializationHelper_SerializeUser_Works()
    {
        var user = new User("id")
        {
            Custom =
            {
                ["BooleanValue"] = true,
                ["CharValue"] = 'c',
                ["SByteValue"] = sbyte.MinValue,
                ["ByteValue"] = sbyte.MaxValue,
                ["Int16Value"] = short.MinValue,
                ["UInt16Value"] = ushort.MaxValue,
                ["Int32Value"] = int.MinValue,
                ["UInt32Value"] = int.MaxValue,
                ["Int64Value"] = long.MinValue,
                ["UInt64Value"] = long.MaxValue,
                ["SingleValue"] = 3.14f,
                ["DoubleValue"] = 3.14,
                ["DecimalValue"] = 3.14m,
                ["DateTimeValue"] = DateTime.MaxValue,
                ["DateTimeOffsetValue"] = DateTime.MaxValue,
                ["TimeSpanValue"] = TimeSpan.MaxValue,
                ["StringValue"] = "s",
                ["GuidValue"] = Guid.Empty,
                ["StringArrayValue"] = new string[] { "a", "b", "c" },
                ["DictionaryValue"] = new Dictionary<int, string> { [0] = "a", [1] = "b", [2] = "c" },
                ["NestedCollectionValue"] = new object[] { false, new Dictionary<string, object> { ["a"] = 0, ["b"] = new object[] { true, "c" } } },
            }
        };

        Assert.AreEqual(
            """
            {"Identifier":"id","BooleanValue":true,"CharValue":"c","SByteValue":-128,"ByteValue":127,"Int16Value":-32768,"UInt16Value":65535,"Int32Value":-2147483648,"UInt32Value":2147483647,"Int64Value":-9223372036854775808,"UInt64Value":9223372036854775807,"SingleValue":3.14,"DoubleValue":3.14,"DecimalValue":3.14,"DateTimeValue":"9999-12-31T23:59:59.9999999","DateTimeOffsetValue":"9999-12-31T23:59:59.9999999","TimeSpanValue":"10675199.02:48:05.4775807","StringValue":"s","GuidValue":"00000000-0000-0000-0000-000000000000","StringArrayValue":["a","b","c"],"DictionaryValue":{"0":"a","1":"b","2":"c"},"NestedCollectionValue":[false,{"a":0,"b":[true,"c"]}]}
            """,
            SerializationHelper.SerializeUser(user));
    }

    [TestMethod]
    public void SerializationHelper_SerializeUser_DetectsCircularReference()
    {
        var dictionary = new Dictionary<string, object>();
        dictionary["a"] = new object[] { dictionary };

        var user = new User("id")
        {
            Custom =
            {
                ["ArrayValue"] = new object[] { dictionary },
            }
        };

        var ex = Assert.ThrowsException<InvalidOperationException>(() => SerializationHelper.SerializeUser(user));
        StringAssert.StartsWith(ex.Message, "A circular reference was detected");
    }
}
