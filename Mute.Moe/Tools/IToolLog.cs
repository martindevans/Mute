using System.Globalization;
using Dapper;
using Dapper.Contrib.Extensions;
using Mute.Moe.Services.Database;
using System.Threading.Tasks;

namespace Mute.Moe.Tools;

/// <summary>
/// Service for monitoring tool usage
/// </summary>
public interface IToolLog
{
    /// <summary>
    /// Log a tool query
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    Task Call(ToolCall data);

    /// <summary>
    /// Query for calls
    /// </summary>
    /// <param name="callContext"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="name"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public IEnumerable<ToolCall> Calls(ulong callContext, DateTime? before = null, DateTime? after = null, string? name = null, Guid? id = null);
    
    /// <summary>
    /// Log a tool response
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    Task Response(ToolResponse data);

    /// <summary>
    /// Query for responses
    /// </summary>
    /// <param name="callContext"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public IEnumerable<ToolResponse> Responses(ulong callContext, DateTime? before = null, DateTime? after = null, Guid? id = null);

    /// <summary>
    /// Data about a tool call
    /// </summary>
    /// <param name="Timestamp"></param>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="Parameters"></param>
    /// <param name="CallContext"></param>
    public record ToolCall(DateTime Timestamp, Guid Id, string Name, string Parameters, ulong CallContext);

    /// <summary>
    /// Data about a tool response
    /// </summary>
    /// <param name="Timestamp"></param>
    /// <param name="Id"></param>
    /// <param name="Value"></param>
    /// <param name="Success"></param>
    /// <param name="CallContext"></param>
    public record ToolResponse(DateTime Timestamp, Guid Id, string? Value, bool Success, ulong CallContext);
}

/// <inheritdoc />
public class DatabaseToolLog
    : IToolLog
{
    private readonly IDatabaseService _database;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="database"></param>
    public DatabaseToolLog(IDatabaseService database)
    {
        _database = database;

        using var db = _database.GetConnection();
        db.Execute("CREATE TABLE IF NOT EXISTS `ToolCallModels` (`UnixTimestamp` TEXT NOT NULL, `CallContext` TEXT NOT NULL, `CallId` TEXT NOT NULL, `Name` TEXT NOT NULL, `Parameters` TEXT NOT NULL)");
        db.Execute("CREATE TABLE IF NOT EXISTS `ToolResponseModels` (`UnixTimestamp` TEXT NOT NULL, `CallContext` TEXT NOT NULL, `CallId` TEXT NOT NULL, `Value` TEXT, `Success` INTEGER NOT NULL)");
    }

    /// <inheritdoc />
    public async Task Call(IToolLog.ToolCall data)
    {
        using var db = _database.GetConnection();

        await db.InsertAsync(new ToolCallModel(
            data.Timestamp.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
            data.CallContext.ToString(CultureInfo.InvariantCulture),
            data.Id.ToString(),
            data.Name,
            data.Parameters
        ));
    }

    /// <inheritdoc />
    public IEnumerable<IToolLog.ToolCall> Calls(ulong callContext, DateTime? before = null, DateTime? after = null, string? name = null, Guid? id = null)
    {
        const string query = """
                             SELECT *
                             FROM `ToolCallModels`
                             WHERE (@Before IS NULL OR `UnixTimestamp` < @Before)
                               AND (@After IS NULL OR `UnixTimestamp` > @After)
                               AND (@Name IS NULL OR `Name` = @Name)
                               AND (@Id IS NULL OR `CallId` = @Id)
                               AND (@CallCtx = `CallContext`)
                             ORDER BY UnixTimestamp DESC 
                             """;

        using var db = _database.GetConnection();

        var results = db.Query<ToolCallModel>(query, new
        {
            Before = before?.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
            After = after?.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
            Name = name,
            Id = id?.ToString(),
            CallCtx = callContext.ToString(CultureInfo.InvariantCulture),
        });
        
        return results
           .Select(r => new IToolLog.ToolCall(
                ulong.Parse(r.UnixTimestamp).FromUnixTimestamp(),
                Guid.Parse(r.CallId),
                r.Name,
                r.Parameters,
                ulong.Parse(r.CallContext)
            )
        );
    }

    /// <inheritdoc />
    public async Task Response(IToolLog.ToolResponse data)
    {
        using var db = _database.GetConnection();

        await db.InsertAsync(new ToolResponseModel(
            data.Timestamp.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
            data.CallContext.ToString(CultureInfo.InvariantCulture),
            data.Id.ToString(),
            data.Value,
            Convert.ToInt32(data.Success)
        ));
    }

    /// <inheritdoc />
    public IEnumerable<IToolLog.ToolResponse> Responses(ulong callContext, DateTime? before = null, DateTime? after = null, Guid? id = null)
    {
        const string query = """
                             SELECT *
                             FROM `ToolResponseModels`
                             WHERE (@Before IS NULL OR `UnixTimestamp` < @Before)
                               AND (@After IS NULL OR `UnixTimestamp` > @After)
                               AND (@Id IS NULL OR `CallId` = @Id)
                               AND (@CallCtx = `CallContext`)
                             ORDER BY UnixTimestamp DESC 
                             """;

        using var db = _database.GetConnection();

        var results = db.Query<ToolResponseModel>(query, new
        { 
            Before = before?.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
            After = after?.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
            Id = id?.ToString(),
            CallCtx = callContext.ToString(CultureInfo.InvariantCulture),
        });

        return results
           .Select(r => new IToolLog.ToolResponse(
                ulong.Parse(r.UnixTimestamp).FromUnixTimestamp(),
                Guid.Parse(r.CallId),
                r.Value,
                Convert.ToBoolean(r.Success),
                ulong.Parse(r.CallContext)
            )
        );
    }

    private record ToolCallModel(string UnixTimestamp, string CallContext, string CallId, string Name, string Parameters);
    private record ToolResponseModel(string UnixTimestamp, string CallContext, string CallId, string? Value, long Success);
}