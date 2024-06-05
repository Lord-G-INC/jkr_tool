global using System.Runtime.CompilerServices;
global using System.Text;

namespace binary_stream;

public static class Globals {
    class U<T> where T : unmanaged;
    public static bool IsUnmanaged(this Type t)
    {
        try { typeof(U<>).MakeGenericType(t); return true; }
        catch (Exception){ return false; }
    }
    internal static void WriteValueType(BinaryStream stream, ValueType value) {
        var bs = stream.GetType();
        var mth = bs.GetMethods().Where(x => x.Name is "WriteUnmanaged" && x.IsGenericMethod).First()
        .MakeGenericMethod(value.GetType());
        mth.Invoke(stream, [value]);
    }   
}