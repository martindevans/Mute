#nullable enable
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;
using Newtonsoft.Json.Linq;

namespace Mute.Tests.Extensions;

[TestClass]
public class IReadOnlyDictionaryExtensionsTests
{
    #region GetToolParameterObject

    [TestMethod]
    public void GetToolParameterObject_KeyExists_ReturnsValue()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = "value" };

        var success = dict.GetToolParameterObject("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.AreEqual("value", result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterObject_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterObject("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    [TestMethod]
    public void GetToolParameterObject_NullValue_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = null };

        var success = dict.GetToolParameterObject("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterFloat

    [TestMethod]
    public void GetToolParameterFloat_ValidFloat_ReturnsValue()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = 3.14f };

        var success = dict.GetToolParameterFloat("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.AreEqual(3.14f, result, 0.001f);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterFloat_ConvertibleStringValue_ReturnsValue()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = "2.5" };

        var success = dict.GetToolParameterFloat("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.AreEqual(2.5f, result, 0.001f);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterFloat_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterFloat("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.AreEqual(default, result);
        Assert.IsNotNull(err);
    }

    [TestMethod]
    public void GetToolParameterFloat_InvalidStringValue_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = "not_a_number" };

        var success = dict.GetToolParameterFloat("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.AreEqual(default, result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterBoolean

    [TestMethod]
    public void GetToolParameterBoolean_ValidBoolTrue_ReturnsTrue()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = true };

        var success = dict.GetToolParameterBoolean("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterBoolean_ValidBoolFalse_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = false };

        var success = dict.GetToolParameterBoolean("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsFalse(result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterBoolean_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterBoolean("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.AreEqual(default, result);
        Assert.IsNotNull(err);
    }

    [TestMethod]
    public void GetToolParameterBoolean_InvalidValue_ReturnsFalse()
    {
        // DateTime does not implement IConvertible.ToBoolean, so Convert.ToBoolean throws
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new System.DateTime() };

        var success = dict.GetToolParameterBoolean("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.AreEqual(default, result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterInteger

    [TestMethod]
    public void GetToolParameterInteger_ValidInt_ReturnsValue()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = 42 };

        var success = dict.GetToolParameterInteger("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.AreEqual(42, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterInteger_ConvertibleStringValue_ReturnsValue()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = "99" };

        var success = dict.GetToolParameterInteger("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.AreEqual(99, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterInteger_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterInteger("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.AreEqual(default, result);
        Assert.IsNotNull(err);
    }

    [TestMethod]
    public void GetToolParameterInteger_InvalidStringValue_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = "not_an_int" };

        var success = dict.GetToolParameterInteger("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.AreEqual(default, result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterString

    [TestMethod]
    public void GetToolParameterString_StringValue_ReturnsValue()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = "hello" };

        var success = dict.GetToolParameterString("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.AreEqual("hello", result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterString_NonStringValue_ReturnsToStringResult()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = 123 };

        var success = dict.GetToolParameterString("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.AreEqual("123", result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterString_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterString("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterStringArray

    [TestMethod]
    public void GetToolParameterStringArray_StringArray_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new[] { "a", "b", "c" } };

        var success = dict.GetToolParameterStringArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterStringArray_ListOfStrings_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new List<string?> { "x", "y" } };

        var success = dict.GetToolParameterStringArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { "x", "y" }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterStringArray_JArray_ReturnsArray()
    {
        var jarray = new JArray("foo", "bar");
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = jarray };

        var success = dict.GetToolParameterStringArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("foo", result[0]);
        Assert.AreEqual("bar", result[1]);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterStringArray_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterStringArray("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    [TestMethod]
    public void GetToolParameterStringArray_UnsupportedType_ReturnsFalse()
    {
        // An int value (not array/list/JArray) cannot be converted to string[]
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = 42 };

        var success = dict.GetToolParameterStringArray("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterFloatArray

    [TestMethod]
    public void GetToolParameterFloatArray_FloatArray_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new[] { 1.0f, 2.0f, 3.0f } };

        var success = dict.GetToolParameterFloatArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterFloatArray_ListOfFloats_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new List<float> { 4.0f, 5.0f } };

        var success = dict.GetToolParameterFloatArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 4.0f, 5.0f }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterFloatArray_JArray_ReturnsArray()
    {
        var jarray = new JArray(1.5, 2.5);
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = jarray };

        var success = dict.GetToolParameterFloatArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(1.5f, result[0], 0.001f);
        Assert.AreEqual(2.5f, result[1], 0.001f);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterFloatArray_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterFloatArray("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterIntArray

    [TestMethod]
    public void GetToolParameterIntArray_IntArray_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new[] { 10, 20, 30 } };

        var success = dict.GetToolParameterIntArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterIntArray_ListOfInts_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new List<int> { 7, 8, 9 } };

        var success = dict.GetToolParameterIntArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 7, 8, 9 }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterIntArray_JArray_ReturnsArray()
    {
        var jarray = new JArray(100, 200);
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = jarray };

        var success = dict.GetToolParameterIntArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(100, result[0]);
        Assert.AreEqual(200, result[1]);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterIntArray_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterIntArray("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    #endregion

    #region GetToolParameterBoolArray

    [TestMethod]
    public void GetToolParameterBoolArray_BoolArray_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new[] { true, false, true } };

        var success = dict.GetToolParameterBoolArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { true, false, true }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterBoolArray_ListOfBools_ReturnsArray()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = new List<bool> { false, true } };

        var success = dict.GetToolParameterBoolArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { false, true }, result);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterBoolArray_JArray_ReturnsArray()
    {
        var jarray = new JArray(true, false);
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?> { ["key"] = jarray };

        var success = dict.GetToolParameterBoolArray("key", out var result, out var err);

        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length);
        Assert.IsTrue(result[0]);
        Assert.IsFalse(result[1]);
        Assert.IsNull(err);
    }

    [TestMethod]
    public void GetToolParameterBoolArray_KeyMissing_ReturnsFalse()
    {
        IReadOnlyDictionary<string, object?> dict = new Dictionary<string, object?>();

        var success = dict.GetToolParameterBoolArray("key", out var result, out var err);

        Assert.IsFalse(success);
        Assert.IsNull(result);
        Assert.IsNotNull(err);
    }

    #endregion
}