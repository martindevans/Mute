using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using LlmTornado.Infra;

namespace Mute.Moe.Tools;

/// <summary>
/// Automatically create an LLM tool from a method. Using reflection to extract the parameter info
/// </summary>
public class AutoTool
    : ITool
{
    private readonly Delegate _action;
    private readonly Action<object[]>? _preprocess;
    private readonly Func<object?, object?>? _postprocess;
    private readonly Func<object?, Task<object?>>? _postprocessAsync;

    private readonly IReadOnlyList<ToolParam> _params;
    private readonly IReadOnlyList<(string, Type)> _methodParams;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public bool IsDefaultTool { get; }

    /// <summary>
    /// Create a new autotool, reflects parameters from the delegate
    /// </summary>
    /// <param name="name"></param>
    /// <param name="isDefault"></param>
    /// <param name="action">The actual tool action. XML docs will be extracted from this method, and used as the docs for the tool.</param>
    /// <param name="preprocess">Preprocess arguments before calling action</param>
    /// <param name="postprocess">Postprocess result from calling action</param>
    /// <param name="postprocessAsync">Postprocess result from calling action</param>
    public AutoTool(string name, bool isDefault, Delegate action, Action<object[]>? preprocess = null, Func<object?, object?>? postprocess = null, Func<object?, Task<object?>>? postprocessAsync = null)
    {
        Name = name;
        Description = action.GetMethodInfo().GetDocumentation() ?? "";
        IsDefaultTool = isDefault;

        _action = action;
        _preprocess = preprocess;
        _postprocess = postprocess;
        _postprocessAsync = postprocessAsync;

        var toolParams = new List<ToolParam>();
        _params = toolParams;

        var methodParams = new List<(string, Type)>();
        _methodParams = methodParams;

        foreach (var parameterInfo in _action.Method.GetParameters())
        {
            var docs = parameterInfo.GetDocumentation() ?? "";
            var ttype = ParamType(parameterInfo.ParameterType, docs, true);

            toolParams.Add(new ToolParam(parameterInfo.Name!, ttype));

            methodParams.Add((parameterInfo.Name!, parameterInfo.ParameterType));
        }
    }

    private static IToolParamType ParamType(Type type, string desc, bool required)
    {
        // Handle nullables of the other types
        var innerNullable = Nullable.GetUnderlyingType(type);
        if (innerNullable != null)
        {
            return new ToolParamNullable(
                ParamType(innerNullable, desc, required)
            );
        }

        // Atomic types
        if (type == typeof(float))
            return new ToolParamNumber(desc, required);
        if (type == typeof(int))
            return new ToolParamInt(desc, required);
        if (type == typeof(string))
            return new ToolParamString(desc, required);
        if (type == typeof(bool))
            return new ToolParamBool(desc, required);
        if (type.IsEnum)
            return new ToolParamEnum(desc, type.GetEnumNames().ToList(), required);

        // Array of atomic types
        if (type.IsArray)
        {
            var element = type.GetElementType()!;

            if (element == typeof(float))
                return new ToolParamListAtomic(desc, ToolParamAtomicTypes.Float, required);
            if (element == typeof(int))
                return new ToolParamListAtomic(desc, ToolParamAtomicTypes.Int, required);
            if (element == typeof(string))
                return new ToolParamListAtomic(desc, ToolParamAtomicTypes.String, required);
            if (element == typeof(bool))
                return new ToolParamListAtomic(desc, ToolParamAtomicTypes.Bool, required);
            if (element.IsEnum)
                return new ToolParamListEnum(desc, element.GetEnumNames(), required);
        }

        throw new ArgumentException($"Type '{type.FullName}' cannot be mapped to tool type");
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolParam> GetParameters()
    {
        return _params;
    }

    /// <inheritdoc />
    public async Task<(bool success, object result)> Execute(IReadOnlyDictionary<string, object?> arguments)
    {
        // Get all parameters and convert to the right type
        var parameters = new object[_methodParams.Count];
        var idx = 0;
        foreach (var (name, type) in _methodParams)
        {
            if (type == typeof(float))
            {
                if (!arguments.GetToolParameterFloat(name, out var f, out var e))
                    return (false, e);
                parameters[idx++] = f;
            }
            else if (type == typeof(int))
            {
                if (!arguments.GetToolParameterInteger(name, out var i, out var e))
                    return (false, e);
                parameters[idx++] = i;
            }
            else if (type == typeof(string))
            {
                if (!arguments.GetToolParameterString(name, out var s, out var e))
                    return (false, e);
                parameters[idx++] = s;
            }
            else if (type == typeof(bool))
            {
                if (!arguments.GetToolParameterBoolean(name, out var b, out var e))
                    return (false, e);
                parameters[idx++] = b;
            }
            else if (type.IsArray)
            {
                var element = type.GetElementType()!;

                if (element == typeof(float))
                {
                    if (!arguments.GetToolParameterFloatArray(name, out var fa, out var e))
                        return (false, e);
                    parameters[idx++] = fa;
                }
                else if (element == typeof(float))
                {
                    if (!arguments.GetToolParameterIntArray(name, out var ia, out var e))
                        return (false, e);
                    parameters[idx++] = ia;
                }
                else if (element == typeof(string))
                {
                    if (!arguments.GetToolParameterStringArray(name, out var sa, out var e))
                        return (false, e);
                    parameters[idx++] = sa;
                }
                else if (element == typeof(bool))
                {
                    if (!arguments.GetToolParameterBoolArray(name, out var sa, out var e))
                        return (false, e);
                    parameters[idx++] = sa;
                }
            }
            else
            {
                return (false, new { error = $"Invalid parameter type '{type.FullName}' - this is likely a bug in the AutoTool code, report it to developers" });
            }
        }

        // Preprocess parameters
        _preprocess?.Invoke(parameters);

        // Invoke the actual method
        var result = _action.Method.Invoke(_action.Target, parameters);

        // Postprocess result
        if (_postprocess != null)
            result = _postprocess.Invoke(result);
        if (_postprocessAsync != null)
            result = await _postprocessAsync.Invoke(result);

        // If the result is a task, await and then extract the actual result
        if (result is Task t)
        {
            await t;

            result = t.GetType()
                      .GetProperty("Result")?
                      .GetValue(t);
        }

        // If the result is an enumerable, convert to an array.
        // Apply a hard limit of 1024 items as a sanity check to protect against infinite enumerables.
        if (result is IEnumerable en)
        {
            result = en.Cast<object>().Take(1024).ToArray();
        }

        // Null results are invalid
        if (result == null)
            return (false, new { error = "Tool returned a null result" });

        // Success!
        return (true, result);
    }

    /// <summary>
    /// Convert an object which is an <see cref="IAsyncEnumerable{T}"/> to an <see cref="IEnumerable{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static IEnumerable<T> AsyncEnumerableToEnumerable<T>(object? obj)
    {
        var en = (IAsyncEnumerable<T>)obj!;
        return en.ToBlockingEnumerable();
    }
}