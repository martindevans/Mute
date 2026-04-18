using System.Data;
using Dapper;


namespace Mute.Moe.Services.Database;

/// <summary>
/// Main SQL database services
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Get a DB connection
    /// </summary>
    /// <returns></returns>
    IDbConnection GetConnection();
}