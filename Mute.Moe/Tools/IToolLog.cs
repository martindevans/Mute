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
    /// <returns></returns>
    public IEnumerable<ToolCall> Calls(ulong callContext, DateTime? before = null, DateTime? after = null, string? name = null);
    
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
    public IEnumerable<ToolResponse> Responses(ulong callContext, DateTime? before = null, DateTime? after = null, string? id = null);
    
    /// <summary>
    /// Data about a tool call
    /// </summary>
    /// <param name="Timestamp"></param>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="Parameters"></param>
    public record ToolCall(DateTime Timestamp, Guid Id, string Name, string Parameters, ulong CallContext);

    /// <summary>
    /// Data about a tool response
    /// </summary>
    /// <param name="Timestamp"></param>
    /// <param name="Id"></param>
    /// <param name="Value"></param>
    /// <param name="Success"></param>
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
        db.Execute("CREATE TABLE IF NOT EXISTS `ToolCallModels` (`UnixTimestamp` INTEGER NOT NULL, `CallContext` INTEGER NOT NULL, `CallId` TEXT NOT NULL, `Name` TEXT NOT NULL, `Parameters` TEXT NOT NULL)");
        db.Execute("CREATE TABLE IF NOT EXISTS `ToolResponseModels` (`UnixTimestamp` INTEGER NOT NULL, `CallContext` INTEGER NOT NULL, `CallId` TEXT NOT NULL, `Value` TEXT, `Success` INTEGER NOT NULL)");
    }

    /// <inheritdoc />
    public async Task Call(IToolLog.ToolCall data)
    {
        using var db = _database.GetConnection();

        await db.InsertAsync(new ToolCallModel(
            data.Timestamp.UnixTimestamp(),
            data.CallContext,
            data.Id.ToString(),
            data.Name,
            data.Parameters
        ));
    }

    /// <inheritdoc />
    public IEnumerable<IToolLog.ToolCall> Calls(ulong callContext, DateTime? before = null, DateTime? after = null, string? name = null)
    {
        const string query = """
                             SELECT *
                             FROM `ToolCallModels`
                             WHERE (@Before IS NULL OR `UnixTimestamp` < @Before)
                               AND (@After IS NULL OR `UnixTimestamp` > @After)
                               AND (@Name IS NULL OR `Name` = @Name)
                               AND (@CallCtx = `CallContext`)
                             """;

        using var db = _database.GetConnection();

        var results = db.Query<ToolCallModel>(query, new
        {
            Before = before?.UnixTimestamp(),
            After = after?.UnixTimestamp(),
            Name = name,
            CallCtx = callContext,
        });
        
        return results
           .Select(r => new IToolLog.ToolCall(
                r.UnixTimestamp.FromUnixTimestamp(),
                Guid.Parse(r.CallId),
                r.Name,
                r.Parameters,
                r.CallContext
            )
        );
    }

    /// <inheritdoc />
    public async Task Response(IToolLog.ToolResponse data)
    {
        using var db = _database.GetConnection();

        await db.InsertAsync(new ToolResponseModel(
            data.Timestamp.UnixTimestamp(),
            data.CallContext,
            data.Id.ToString(),
            data.Value,
            Convert.ToInt32(data.Success)
        ));
    }

    /// <inheritdoc />
    public IEnumerable<IToolLog.ToolResponse> Responses(ulong callContext, DateTime? before = null, DateTime? after = null, string? id = null)
    {
        const string query = """
                             SELECT *
                             FROM `ToolResponseModels`
                             WHERE (@Before IS NULL OR `UnixTimestamp` < @Before)
                               AND (@After IS NULL OR `UnixTimestamp` > @After)
                               AND (@Id IS NULL OR `Id` = @Id)
                               AND (@CallCtx = `CallContext`)
                             """;

        using var db = _database.GetConnection();

        var results = db.Query<ToolResponseModel>(query, new
        { 
            Before = before?.UnixTimestamp(),
            After = after?.UnixTimestamp(),
            Id = id,
            CallCtx = callContext,
        });

        return results
           .Select(r => new IToolLog.ToolResponse(
                r.UnixTimestamp.FromUnixTimestamp(),
                Guid.Parse(r.CallId),
                r.Value,
                Convert.ToBoolean(r.Success),
                r.CallContext
            )
        );
    }

    private record ToolCallModel(ulong UnixTimestamp, ulong CallContext, string CallId, string Name, string Parameters);
    private record ToolResponseModel(ulong UnixTimestamp, ulong CallContext, string CallId, string? Value, int Success);
}