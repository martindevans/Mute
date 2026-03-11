using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// 
/// </summary>
public static class IDataReaderExtensions
{
    /// <summary>
    /// Read all rows from data reader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader"></param>
    /// <param name="read"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IDataReader reader, Func<IDataReader, ValueTask<T>> read)
    {
        if (reader is DbDataReader dbReader)
        {
            await using (dbReader)
            {
                while (await dbReader.ReadAsync())
                    yield return await read(reader);
            }
        }
        else
        {
            using (reader)
            {
                while (reader.Read())
                    yield return await read(reader);
            }
        }
    }

    /// <summary>
    /// Read all rows from data reader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader"></param>
    /// <param name="read"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this DbDataReader reader, Func<IDataReader, ValueTask<T>> read)
    {
        await using (reader)
        {
            while (await reader.ReadAsync())
                yield return await read(reader);
        }
    }

    /// <summary>
    /// Read all rows from data reader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader"></param>
    /// <param name="read"></param>
    /// <returns></returns>
    public static IEnumerable<T> ToEnumerable<T>(this IDataReader reader, Func<IDataReader, T> read)
    {
        using (reader)
        {
            while (reader.Read())
                yield return read(reader);
        }
    }
}