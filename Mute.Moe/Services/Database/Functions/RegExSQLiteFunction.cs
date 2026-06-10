using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace Mute.Moe.Services.Database.Functions;

/// <summary>
/// from https://stackoverflow.com/questions/172735/create-use-user-defined-functions-in-system-data-sqlite
/// taken from http://sqlite.phxsoftware.com/forums/p/348/1457.aspx#1457
/// </summary>
[SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
public class RegExSQLiteFunction
    : SQLiteFunction
{
    // Static cache for regex objects. This function is very likely to be called many times with the same pattern!
    [ThreadStatic] private static Dictionary<string, Regex>? _cache;
    
    /// <inheritdoc />
    public override object Invoke(object[] args)
    {
        // Ensure cache exists
        _cache ??= new();

        // Get or add
        var pattern = Convert.ToString(args[1]) ?? "";
        if (!_cache.TryGetValue(pattern, out var regex))
        {
            // Clear cache if it's too large to add another item to
            if (_cache.Count > 128)
                _cache.Clear();
            
            // Create and store new regex
            regex = new Regex(pattern);
            _cache[pattern] = regex;
        }

        // Do the actual matching
        var match = regex.IsMatch(Convert.ToString(args[0]) ?? "");
        return match;
    }
}