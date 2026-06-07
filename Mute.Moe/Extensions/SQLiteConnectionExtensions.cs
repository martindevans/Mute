using System.Data.SQLite;

namespace Mute.Moe.Extensions
{
    /// <summary>
    /// Extensions to <see cref="SQLiteConnection"/>
    /// </summary>
    public static class SQLiteConnectionExtensions
    {
        /// <summary>
        /// Bind a <see cref="SQLiteFunction"/>>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="function"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void BindFunction(this SQLiteConnection connection, SQLiteFunction function)
        {
            var attributes = function.GetType().GetCustomAttributes(typeof(SQLiteFunctionAttribute), true).Cast<SQLiteFunctionAttribute>().ToArray();
            if (attributes.Length == 0)
                throw new InvalidOperationException("SQLiteFunction doesn't have SQLiteFunctionAttribute");
            
            connection.BindFunction(attributes[0], function);
        }
    }
}
