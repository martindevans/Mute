using System.Data;
using Dapper;


namespace Mute.Moe.Services.Database;

/// <summary>
/// Main SQL database services
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// The current open DB connection
    /// </summary>
    IDbConnection Connection { get; }
}

/// <summary>
/// Extensions for <see cref="IDatabaseService"/>
/// </summary>
public static class IDatabaseServiceExtensions
{
    /// <summary>
    /// Immediately execute some non-query SQL and return the count
    /// </summary>
    /// <param name="db"></param>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static int Exec(this IDatabaseService db, string sql)
    {
        return db.Connection.Execute(sql);
    }
}