using System.Data.SQLite;

namespace Mute.Moe.Services.Database.Functions;

/// <summary>
/// from https://stackoverflow.com/questions/172735/create-use-user-defined-functions-in-system-data-sqlite
/// taken from http://sqlite.phxsoftware.com/forums/p/348/1457.aspx#1457
/// </summary>
[SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
public class RegExSQLiteFunction
    : SQLiteFunction
{
    /// <inheritdoc />
    public override object Invoke(object[] args)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            Convert.ToString(args[1]) ?? "",
            Convert.ToString(args[0]) ?? ""
        );
    }
}