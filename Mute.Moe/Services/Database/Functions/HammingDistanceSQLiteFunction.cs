using System.Data.SQLite;
using System.Numerics.Tensors;

namespace Mute.Moe.Services.Database.Functions;

/// <summary>
/// from https://stackoverflow.com/questions/172735/create-use-user-defined-functions-in-system-data-sqlite
/// taken from http://sqlite.phxsoftware.com/forums/p/348/1457.aspx#1457
/// </summary>
[SQLiteFunction(Name = "HAMMING_DISTANCE", Arguments = 1, FuncType = FunctionType.Scalar)]
public class HammingDistanceSQLiteFunction
    : SQLiteFunction
{
    /// <inheritdoc />
    public override object Invoke(object[] args)
    {
        // Check argument types
        if (args.Length != 2)
            throw new ArgumentException("HAMMING_DISTANCE requires exactly two arguments");
        if (args[0] is not byte[] data1)
            throw new ArgumentException("HAMMING_DISTANCE requires a BLOB argument");
        if (args[1] is not byte[] data2)
            throw new ArgumentException("HAMMING_DISTANCE requires a BLOB argument");
        if (data1.Length != data2.Length)
            throw new ArgumentException("HAMMING_DISTANCE requires BLOB arguments of equal length");

        return (ulong)TensorPrimitives.HammingBitDistance(data1.AsSpan(), data2.AsSpan());
    }
}