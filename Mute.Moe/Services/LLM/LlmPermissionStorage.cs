using Mute.Moe.Services.Database;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM;

public interface ILlmPermission
{
    Task SetPermission(ulong id, bool data);

    Task<bool> GetPermission(ulong id);
}

public sealed class DatabaseLlmPermissionStorage(IDatabaseService database)
    : SimpleJsonBlobTable<DatabaseLlmPermissionStorage.PermissionContainer>("LLM_Permission", database), ILlmPermission
{
    public sealed class PermissionContainer
    {
        public bool Value { get; set; }
    }

    public Task SetPermission(ulong id, bool data)
    {
        return Put(id, new PermissionContainer { Value = data });
    }

    public async Task<bool> GetPermission(ulong id)
    {
        return (await Get(id))?.Value ?? false;
    }
}