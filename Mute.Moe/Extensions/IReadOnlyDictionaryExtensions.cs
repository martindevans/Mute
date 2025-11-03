using System.Diagnostics.CodeAnalysis;

namespace Mute.Moe.Extensions;

/// <summary>
/// Get parameters from a dictionary of objects, converting type
/// </summary>
public static class IReadOnlyDictionaryExtensions
{
    /// <summary>
    /// Get an object type parameter
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="key"></param>
    /// <param name="result"></param>
    /// <param name="err"></param>
    /// <returns></returns>
    public static bool GetToolParameterObject(this IReadOnlyDictionary<string, object?> parameters, string key, [NotNullWhen(true)] out object? result, [NotNullWhen(false)] out object? err)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null)
        {
            result = default;
            err = new { error = $"Must provide '{key}' parameter" };
            return false;
        }

        result = value;
        err = null;
        return true;
    }

    /// <summary>
    /// Get a float type parameter
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="key"></param>
    /// <param name="result"></param>
    /// <param name="err"></param>
    /// <returns></returns>
    public static bool GetToolParameterFloat(this IReadOnlyDictionary<string, object?> parameters, string key, out float result, [NotNullWhen(false)] out object? err)
    {
        if (!GetToolParameterObject(parameters, key, out var obj, out err))
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToSingle(obj);
            return true;
        }
        catch
        {
            err = new { error = $"Failed to convert parameter '{key}' to number" };
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Get a boolean type parameter
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="key"></param>
    /// <param name="result"></param>
    /// <param name="err"></param>
    /// <returns></returns>
    public static bool GetToolParameterBoolean(this IReadOnlyDictionary<string, object?> parameters, string key, out bool result, [NotNullWhen(false)] out object? err)
    {
        if (!GetToolParameterObject(parameters, key, out var obj, out err))
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToBoolean(obj);
            return true;
        }
        catch
        {
            err = new { error = $"Failed to convert parameter '{key}' to boolean" };
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Get a integer type parameter
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="key"></param>
    /// <param name="result"></param>
    /// <param name="err"></param>
    /// <returns></returns>
    public static bool GetToolParameterInteger(this IReadOnlyDictionary<string, object?> parameters, string key, out int result, [NotNullWhen(false)] out object? err)
    {
        if (!GetToolParameterObject(parameters, key, out var obj, out err))
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToInt32(obj);
            return true;
        }
        catch
        {
            err = new { error = $"Failed to convert parameter '{key}' to integer" };
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Get a string type parameter
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="key"></param>
    /// <param name="result"></param>
    /// <param name="err"></param>
    /// <returns></returns>
    public static bool GetToolParameterString(this IReadOnlyDictionary<string, object?> parameters, string key, [NotNullWhen(true)] out string? result, [NotNullWhen(false)] out object? err)
    {
        if (!GetToolParameterObject(parameters, key, out var obj, out err))
        {
            result = default;
            return false;
        }

        if (obj is string s)
        {
            result = s;
            return true;
        }

        result = obj.ToString() ?? "";
        return true;
    }
}