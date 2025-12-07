using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Mute.Moe.Extensions;

/// <summary>
/// Get parameters from a dictionary of objects, converting type
/// </summary>
public static class IReadOnlyDictionaryExtensions
{
    /// <param name="parameters"></param>
    extension(IReadOnlyDictionary<string, object?> parameters)
    {
        /// <summary>
        /// Get an object type parameter
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterObject(string key, [NotNullWhen(true)] out object? result, [NotNullWhen(false)] out object? err)
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
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterFloat(string key, out float result, [NotNullWhen(false)] out object? err)
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
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterBoolean(string key, out bool result, [NotNullWhen(false)] out object? err)
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
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterInteger(string key, out int result, [NotNullWhen(false)] out object? err)
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
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterString(string key, [NotNullWhen(true)] out string? result, [NotNullWhen(false)] out object? err)
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

        private bool GetToolParameterArrayObject<TElement>(string key, [NotNullWhen(true)] out TElement[]? result, [NotNullWhen(false)] out object? err, Func<object, TElement> convert)
        {
            if (!GetToolParameterObject(parameters, key, out var obj, out err))
            {
                result = default;
                return false;
            }

            try
            {
                switch (obj)
                {
                    case TElement[] ea:
                        result = ea;
                        return true;

                    case IList<TElement> ei:
                        result = ei.ToArray();
                        return true;

                    case JArray jarray:
                        result = new TElement[jarray.Count];
                        for (var i = 0; i < jarray.Count; i++)
                            result[i] = convert(jarray[i]);
                        return true;
                }

                result = default;
                err = new
                {
                    error = $"Parameter '{key}' of type '{obj.GetType().Name}' cannot be converted to {typeof(TElement).Name}[]. This is likely a bug in the bot code which needs fixing."
                };
                return false;
            }
            catch
            {
                err = new { error = $"Failed to convert an item in array for parameter '{key}' to {typeof(TElement).Name}" };
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Get a string array type parameter
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterStringArray(string key, [NotNullWhen(true)] out string?[]? result, [NotNullWhen(false)] out object? err)
        {
            return parameters.GetToolParameterArrayObject(key, out result, out err, Convert.ToString);
        }

        /// <summary>
        /// Get a float array type parameter
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterFloatArray(string key, [NotNullWhen(true)] out float[]? result, [NotNullWhen(false)] out object? err)
        {
            return parameters.GetToolParameterArrayObject(key, out result, out err, Convert.ToSingle);
        }

        /// <summary>
        /// Get a int array type parameter
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterIntArray(string key, [NotNullWhen(true)] out int[]? result, [NotNullWhen(false)] out object? err)
        {
            return parameters.GetToolParameterArrayObject(key, out result, out err, Convert.ToInt32);
        }

        /// <summary>
        /// Get a int array type parameter
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool GetToolParameterBoolArray(string key, [NotNullWhen(true)] out bool[]? result, [NotNullWhen(false)] out object? err)
        {
            return parameters.GetToolParameterArrayObject(key, out result, out err, Convert.ToBoolean);
        }
    }
}